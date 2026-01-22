using PixelBoard.MainServer.Models;

namespace PixelBoard.MainServer.Services;

public class PlainBoardGameService : IGameService
{
    private readonly ILogger<PlainBoardGameService> _logger;
    private readonly IBoardService _displayedBoard;

    public PlainBoardGameService(ILogger<PlainBoardGameService> logger,
                            IBoardService boardService)
    {
        _displayedBoard = boardService;
        _logger = logger;
        _logger.LogWarning("Started Plain Board instead of a real game");
    }

    public void Start(IEnumerable<int> teamIds)
    {
        Color color1 = new Color(150, 150, 150);
        Color color2 = new Color(100, 100, 100);
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                Color c = (x + y) % 2 == 0 ? color1 : color2;
                _displayedBoard.SetColor(x, y, c);
            }
        }
    }

    public void Stop()
    {
        _logger.IgnoredMethod("Stop");
    }

    public void Reset()
    {
        _logger.IgnoredMethod("Reset");
    }

    public void Tick()
    {
        _logger.IgnoredMethod("Tick");
    }

    public void MakeMove(int x, int y, int team)
    {
        SetField(x, y, team);
    }

    private void SetField(int x, int y, int team)
    {
        _displayedBoard.SetColor(x, y, Color.Palette(team));
    }

    public Dictionary<string, string?>? GetTeamInfo(int team)
    {
        // _logger.NotImplementedMethod("GetTeamInfo");
        return new();
    }

    public GameState GetGameState()
    {
        // _logger.NotImplementedMethod("GetGameState");
        return new();
    }

    public uint? Lives(int x, int y)
    {
        return 0;
    }
}

public static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Called {method} while the plain board game is running. The call will be ignored.")]
    public static partial void IgnoredMethod(this ILogger logger, string method);

    [LoggerMessage(Level = LogLevel.Error, Message = "Called {method} while the plain board game is running. This is invalid and will not return proper data.")]
    public static partial void NotImplementedMethod(this ILogger logger, string method);

}