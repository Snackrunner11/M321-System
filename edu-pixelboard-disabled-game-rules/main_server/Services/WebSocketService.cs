namespace PixelBoard.WebSockets;

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Options;
using PixelBoard.MainServer.Models;
using PixelBoard.MainServer.Services;
using PixelBoard.MainServer.Configuration;


public class WebSocketService : IWebSocketService
{
    private readonly ILogger _logger;
    private readonly IReadBoardService _board;
    private ConcurrentDictionary<int, WebSocket> _clients;
    private readonly BoardOptions _boardOptions;
    private IDisposable? _boardSubscription;


    public WebSocketService(IConfiguration configuration, ILogger<WebSocketService> logger, IReadBoardService board, IOptions<BoardOptions> boardOptions)
    {
        _logger = logger;
        _board = board;
        _clients = new ConcurrentDictionary<int, WebSocket>();
        _boardOptions = boardOptions.Value;

        _boardSubscription = board.PixelChanges().Subscribe(async pixel =>
        {
            try
            {
                await PushPixel(pixel.X, pixel.Y, pixel.Color);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to push pixel");
            }
        });
    }

    public async Task HandleTextMessageFromClient(HttpContext context, WebSocket socket, string message, int id)
    {
        if (message == "subscribe")
        {
            // register for updates
            _clients.AddOrUpdate(id, socket, (key, oldSocket) =>
            {
                oldSocket.Abort();
                return socket;
            });
            _logger.LogInformation("There are now {} clients", _clients.Count());


            // send initial state
            await SendTextMessage(socket, FullBoardMessage());
        }
        else
        {
            _logger.LogError("Unexpected WS message: {}", message);

        }
    }

    public async Task HandleBinaryMessageFromClient(HttpContext context, WebSocket socket, byte[] message, int id)
    {
        _logger.LogWarning("Incoming Binary Message, which is not handled");
    }

    public void HandleDisconnect(int id)
    {
        _clients.TryRemove(id, out var nop);
    }

    public async Task PushPixel(int x, int y, Color c)
    {
        var msg = $"p\n{x} {y} {c.Red} {c.Green} {c.Blue}";
        var tasks = _clients.Select(async connection =>
        {
            try
            {
                await SendTextMessage(connection.Value, msg);
            }
            catch
            {
                _logger.LogWarning("Sending to a client failed. Will ignore and continue.");
            }
        });

        await Task.WhenAll(tasks);
        _logger.LogInformation("Pushed Pixel change to {} connected clients", tasks.Count());
    }

    private async Task SendTextMessage(WebSocket socket, string message)
    {
        var binary = Encoding.UTF8.GetBytes(message);
        await socket.SendAsync(
            binary, WebSocketMessageType.Text, true, CancellationToken.None
        );
    }

    private string FullBoardMessage()
    {
        var sb = new StringBuilder("p\n");
        for (int x = 0; x < _boardOptions.BoardWidth; x++)
        {
            for (int y = 0; y < _boardOptions.BoardHeight; y++)
            {
                Color? c = _board.GetColor(x, y);
                if (c is not null)
                {
                    sb.AppendLine($"{x} {y} {c.Red} {c.Green} {c.Blue}");
                }
            }
        }
        return sb.ToString();
    }
}
