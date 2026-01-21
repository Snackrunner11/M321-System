using System.Net.WebSockets;

namespace PixelBoard.MainServer.Services;

public interface IWebSocketService
{
    public Task HandleTextMessageFromClient(HttpContext context, WebSocket socket, string message, int id);

    public Task HandleBinaryMessageFromClient(HttpContext context, WebSocket socket, byte[] message, int id);

    public void HandleDisconnect(int id);
}
