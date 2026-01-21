using PixelBoard.MainServer.Services;
using PixelBoard.MainServer.Models;
using PixelBoard.MainServer.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace main_server_tests;

/// <summary>
/// Test implementors of `IGameService` for its basic functionality.
/// </summary>
public class GameInterfaceTest
{
    private readonly IBoardService _board;
    private readonly IGameService _game;
    public GameInterfaceTest()
    {
        _board = new FakeBoardService();
        _game = new PadukGameService(
            new NullLogger<PadukGameService>(),
             _board,
             Options.Create(new PadukOptions()),
             Options.Create(new BoardOptions())
             );
    }

    private string BoardSnapshot() => new BoardSnapshot(_board).ToAscii();

    [Theory]
    [InlineData(new int[] { 1 })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 })]
    [InlineData(new int[] { 1, 77, 105 })]
    public void TestHappyPath(int[] teams)
    {
        int team = teams[0];
        Color teamColor = Color.Palette(team);

        _game.Start(teams);
        _game.Tick();
        _game.MakeMove(1, 1, team);
        _game.Tick();
        _game.MakeMove(2, 2, team);
        _game.Tick();
        _game.MakeMove(1, 1, team);
        _game.Tick();
        _game.Stop();

        Assert.Equal(teamColor, _board.GetColor(1, 1));
        Assert.Equal(teamColor, _board.GetColor(2, 2));
        Assert.NotEqual(teamColor, _board.GetColor(0, 0));
        Assert.NotEqual(teamColor, _board.GetColor(0, 1));
        Assert.NotEqual(teamColor, _board.GetColor(1, 0));
    }

    [Fact]
    public void EarlyMoveThrowsException()
    {
        Assert.Throws<InvalidOperationException>(
            () => _game.MakeMove(1, 1, 1)
        );
    }

    [Fact]
    public void LateMoveThrowsException()
    {
        _game.Start([1]);
        _game.Stop();
        Assert.Throws<InvalidOperationException>(
            () => _game.MakeMove(1, 1, 1)
        );
    }

    [Fact]
    public void WrongTeamThrowsException()
    {
        int team = 1;
        _game.Start([team]);
        Assert.Throws<InvalidOperationException>(
            () => _game.MakeMove(1, 1, team + 1)
        );
    }

    [Fact]
    public Task BoardStartsEmpty()
    {
        _game.Start([1, 2, 3]);
        return Verify(BoardSnapshot());
    }
}