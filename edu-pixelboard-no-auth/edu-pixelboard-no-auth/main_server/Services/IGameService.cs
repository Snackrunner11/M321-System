namespace PixelBoard.MainServer.Services;

/// <summary>
/// Game logic for a game that can be played on the pixel board.
/// </summary>
public interface IGameService
{
    /// <summary>
    ///  A user activates a pixel in the name of their team.
    /// </summary>
    /// <param name="x">pixel coordinate</param>
    /// <param name="y">pixel coordinate</param>
    /// <param name="team">the team number for which the move is, must be authorized before making this call</param>
    void MakeMove(int x, int y, int team);

    /// <summary>
    /// Initialize the game with teams identified by integer IDs.
    /// </summary>
    /// <param name="teamIds">One identifier per team.</param>
    void Start(IEnumerable<int> teamIds);

    /// <summary>
    /// Finish the game and accept no more moves.
    /// </summary>
    void Stop();

    /// <summary>
    /// Delete the current game, after it was stopped, and reset the state to start a new game.
    /// </summary>
    void Reset();

    /// <summary>
    /// Run frequent evaluations for the game. In the default setting, this is
    /// called once a second.
    /// </summary>
    void Tick();

    Dictionary<string, string?>? GetTeamInfo(int team);

    GameState GetGameState();

    public uint? Lives(int x, int y);
}

public enum GameState
{
    /// <summary>
    /// The game is still in initialization and not ready to accept moves.
    /// </summary>
    Init,
    /// The game is on.
    Active,
    /// The game is over and no more moves are accepted.
    Done
}