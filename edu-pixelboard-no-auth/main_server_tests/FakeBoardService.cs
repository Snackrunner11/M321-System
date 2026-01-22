namespace main_server_tests;

using PixelBoard.MainServer.Models;
using PixelBoard.MainServer.Services;

public sealed class FakeBoardService : IBoardService
{
    private readonly Dictionary<(int, int), Color> board = new Dictionary<(int, int), Color>();

    public Color? GetColor(int x, int y)
    {
        return board.TryGetValue((x, y), out var color) ? color : null;
    }

    public void SetColor(int x, int y, Color color)
    {
        board[(x, y)] = color;
    }

    public void DeleteColor(int x, int y)
    {
        board.Remove((x, y));
    }

    public IObservable<Pixel> PixelChanges()
    {
        throw new NotImplementedException();
    }
}