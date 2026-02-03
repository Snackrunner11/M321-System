using System.Net.WebSockets;
using System.Text;

namespace PixelBoard.WebSockets;

public static class WebSocketEndpointExtension
{
    private static int _id_counter = 0;

    public static IEndpointRouteBuilder MapWebSocketHandler(
        this IEndpointRouteBuilder endpoints, string path)
    {
        endpoints.Map(path, async (
            HttpContext context,
            WebSocketService service) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            await HandleAsync(context, socket, service);
        });

        return endpoints;
    }

    private static async Task HandleAsync(HttpContext context, WebSocket socket, WebSocketService handler)
    {
        int id = Interlocked.Increment(ref _id_counter);
        while (socket.State == WebSocketState.Open)
        {
            Tuple<ValueWebSocketReceiveResult, MemoryStream> result = await ReceiveMessage(socket);
            ValueWebSocketReceiveResult message = result.Item1;
            MemoryStream ms = result.Item2;

            switch (message.MessageType)
            {
                case WebSocketMessageType.Close:
                    {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None);
                        handler.HandleDisconnect(id);
                        return;
                    }
                case WebSocketMessageType.Text:
                    {
                        using var reader = new StreamReader(ms, Encoding.UTF8);
                        string data = await reader.ReadToEndAsync();
                        await handler.HandleTextMessageFromClient(context, socket, data, id);
                        break;
                    }
                case WebSocketMessageType.Binary:
                    {
                        byte[] data = ms.ToArray();
                        await handler.HandleBinaryMessageFromClient(context, socket, data, id);
                        break;
                    }
            }


        }
    }

    private static async Task<Tuple<ValueWebSocketReceiveResult, MemoryStream>> ReceiveMessage(WebSocket socket)
    {
        var buffer = new byte[4096];
        var ms = new MemoryStream();
        ValueWebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(
                buffer.AsMemory(),
                CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return Tuple.Create(result, ms);
            }

            ms.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        ms.Seek(0, SeekOrigin.Begin);
        return Tuple.Create(result, ms);
    }
}
