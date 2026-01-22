using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PixelBoard.MainServer.Configuration;
using PixelBoard.MainServer.Services;
using System.Collections;

namespace main_server_tests;

/// <summary>
/// Test rules and scoring specific to the game Paduk.
/// </summary>
public partial class PadukTest
{
    public static readonly int[] Teams = { 1, 2, 3, 4, 5 };

    private readonly IBoardService _board;
    private readonly IGameService _game;

    public PadukTest()
    {
        PadukOptions options = new PadukOptions();
        BoardOptions boardOptions = new BoardOptions();
        options.TickDelayMs = int.MaxValue;
        _board = new FakeBoardService();
        _game = new PadukGameService(new NullLogger<PadukGameService>(), _board, Options.Create(options), Options.Create(boardOptions));
        _game.Start(Teams);
    }

    private string BoardSnapshot() => new BoardSnapshot(_board).ToAscii();

    [Theory]
    [ClassData(typeof(PadukTestDataEnumerator))]
    public void TestScore(PadukMove[] moves, int expectedScoreTeam1, int expectedScoreTeam2)
    {
        foreach (PadukMove move in moves)
        {
            _game.MakeMove(move.X, move.Y, move.Team);
            _game.Tick();
        }
        _game.Stop();

        Assert.Equal(expectedScoreTeam1.ToString(), _game.GetTeamInfo(Teams[0])?["Score"]);
        Assert.Equal(expectedScoreTeam2.ToString(), _game.GetTeamInfo(Teams[1])?["Score"]);
    }

    [Fact]
    public void TestFieldIsBlockedUntilTick()
    {
        int x = 0;
        int y = 0;

        _game.MakeMove(x, y, Teams[0]);

        // the field is blocked until a tick executes
        Assert.Throws<InvalidOperationException>(
            () => _game.MakeMove(x, y, Teams[0])
        );
        Assert.Throws<InvalidOperationException>(
            () => _game.MakeMove(x, y, Teams[1])
        );

        // assert the move works after a tick
        _game.Tick();
        _game.MakeMove(x, y, Teams[1]);
    }
}

public class PadukMove
{
    public int X;
    public int Y;
    public int Team;

    public PadukMove(int x, int y, int team)
    {
        X = x;
        Y = y;
        Team = team;
    }
}

public class PadukTestDataEnumerator : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        int team1 = PadukTest.Teams[0];
        int team2 = PadukTest.Teams[1];

        yield return new object[] {
            new PadukMove[] {
                new PadukMove(10, 10, team1),
            },
            2,
            0,
        };

        yield return new object[] {
            new PadukMove[] {
                new PadukMove(10, 10, team1),
                new PadukMove(10, 10, team2),
                new PadukMove(10, 10, team1),
            },
            3,
            1,
        };

        yield return new object[] {
            new PadukMove[] {},
            0,
            0,
        };

        yield return new object[] {
            new PadukMove[] {
                new PadukMove(10, 11, team1),
                new PadukMove(10, 10, team2),
                new PadukMove(10, 11, team2),
                new PadukMove(10, 10, team2),
                new PadukMove(10, 10, team2),
            },
            2,
            9,
        };

        yield return new object[] {
            new PadukMove[] {
                new PadukMove(0, 0, team1),
                new PadukMove(0, 1, team2),
                new PadukMove(1, 0, team2),
            },
            2,
            7,
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
