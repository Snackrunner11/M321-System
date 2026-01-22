namespace main_server_tests;

// This file contains test to check if the capturing rules work as expected. We
// use snapshot testing using [Verify](https://github.com/VerifyTests/Verify).
//
// Put simply, each test (Fact) makes some moves and snapshots the board for an
// assertion.
// The moves for each test are hardcoded once. The assertion is always
// `Verify(BoardSnapshot())` which prints the entire board into an ASCII view
// and gives the string to Verify.Xunit to assert it looks correct.
// How the board should look is stored in one *.verified.txt file per test. When
// creating a test, make sure these files show the final board as it must be. If
// a test fails, a diff between received and verified shows what went wrong.

public partial class PadukTest
{
    [Fact]
    public Task SimpleCapture1()
    {
        var team1 = Teams[0];
        var team2 = Teams[1];

        _game.MakeMove(1, 1, team1);

        _game.MakeMove(0, 1, team2);
        _game.MakeMove(1, 0, team2);
        _game.MakeMove(1, 2, team2);
        _game.MakeMove(2, 1, team2);

        return Verify(BoardSnapshot());
    }

    [Fact]
    public Task SimpleCapture3()
    {
        var team1 = Teams[0];
        var team2 = Teams[1];

        _game.MakeMove(1, 1, team1);
        _game.MakeMove(2, 1, team1);
        _game.MakeMove(3, 1, team1);

        _game.MakeMove(0, 1, team2);
        _game.MakeMove(4, 1, team2);
        _game.MakeMove(1, 0, team2);
        _game.MakeMove(2, 0, team2);
        _game.MakeMove(3, 0, team2);
        _game.MakeMove(1, 2, team2);
        _game.MakeMove(2, 2, team2);
        _game.MakeMove(3, 2, team2);

        return Verify(BoardSnapshot());
    }

    [Fact]
    public Task SimpleCaptureCorner()
    {
        var team1 = Teams[0];
        var team2 = Teams[1];

        _game.MakeMove(0, 0, team1);

        _game.MakeMove(0, 1, team2);
        _game.MakeMove(1, 0, team2);

        return Verify(BoardSnapshot());
    }

    [Fact]
    public Task SimpleCaptureBorder()
    {
        var team1 = Teams[0];
        var team2 = Teams[1];

        _game.MakeMove(0, 10, team1);

        _game.MakeMove(0, 11, team2);
        _game.MakeMove(1, 10, team2);
        _game.MakeMove(0, 9, team2);

        return Verify(BoardSnapshot());
    }

    [Fact]
    public Task SimpleKill1()
    {
        var team1 = Teams[0];
        var team2 = Teams[1];
        var team3 = Teams[2];

        _game.MakeMove(1, 1, team1);

        _game.MakeMove(0, 1, team2);
        _game.MakeMove(1, 0, team2);
        _game.MakeMove(1, 2, team2);

        _game.MakeMove(2, 1, team3);

        return Verify(BoardSnapshot());
    }

    [Fact]
    public Task SimpleKill3()
    {
        var team1 = Teams[0];
        var team2 = Teams[1];
        var team3 = Teams[2];

        _game.MakeMove(1, 1, team1);
        _game.MakeMove(2, 1, team1);
        _game.MakeMove(3, 1, team1);

        _game.MakeMove(0, 1, team2);
        _game.MakeMove(4, 1, team2);
        _game.MakeMove(1, 0, team2);
        _game.MakeMove(2, 0, team2);
        _game.MakeMove(3, 0, team2);
        _game.MakeMove(1, 2, team2);
        _game.MakeMove(2, 2, team2);

        _game.MakeMove(3, 2, team3);

        return Verify(BoardSnapshot());
    }

    [Fact]
    public Task SelfKill1()
    {
        var team1 = Teams[0];
        var team2 = Teams[1];
        var team3 = Teams[2];


        _game.MakeMove(0, 1, team2);
        _game.MakeMove(1, 0, team2);
        _game.MakeMove(1, 2, team2);

        _game.MakeMove(2, 1, team3);

        // play into a surrounded field
        _game.MakeMove(1, 1, team1);

        return Verify(BoardSnapshot());
    }

    [Fact]
    public Task SelfCapture3()
    {
        var team1 = Teams[0];
        var team2 = Teams[1];

        _game.MakeMove(1, 1, team1);
        _game.MakeMove(3, 1, team1);

        _game.MakeMove(0, 1, team2);
        _game.MakeMove(4, 1, team2);
        _game.MakeMove(1, 0, team2);
        _game.MakeMove(2, 0, team2);
        _game.MakeMove(3, 0, team2);
        _game.MakeMove(1, 2, team2);
        _game.MakeMove(2, 2, team2);
        _game.MakeMove(3, 2, team2);

        // remove last life of group
        _game.MakeMove(2, 1, team1);

        return Verify(BoardSnapshot());
    }

    // TODO: more complex tests
    // - kill multiple groups at once (same and different teams, all should die even if kill gives them new lifes)
    // - prevented self-kill by killing and by capturing
    // - double eye to survive enclosure
}
