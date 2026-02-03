using PixelBoard.MainServer.Models;

namespace PixelBoard.MainServer.Services;

/// <summary>
/// Provides access to a source which assigns colors to each pixel in the
/// pixel board.
/// </summary>
public interface IBoardService : IReadBoardService, IWriteBoardService
{
}

public interface IReadBoardService
{
    Color? GetColor(int x, int y);
    IObservable<Pixel> PixelChanges();
}

public interface IWriteBoardService
{
    void SetColor(int x, int y, Color color);

    void DeleteColor(int x, int y);
}
