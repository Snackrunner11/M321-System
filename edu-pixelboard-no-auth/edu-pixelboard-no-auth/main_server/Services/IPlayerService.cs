using PixelBoard.MainServer.Models;

namespace PixelBoard.MainServer.Services;

/// <summary>
/// Provides access to registered players and teams.
/// </summary>
public interface IPlayerService
{
    IEnumerable<Player> GetAllPlayers();
    Player? GetPlayer(string id);

    IEnumerable<int> GetAllTeamIds();
    IEnumerable<Team> GetAllTeams();
    Team? GetTeam(int id);
    void SetTeamName(int id, string name);

    void Register(string id, string name, int team);
}