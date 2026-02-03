using PixelBoard.MainServer.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace PixelBoard.MainServer.Services;

public class PlayerService : IPlayerService
{
    private const string NumTeamDbKey = "int:numTeams";
    private const string TeamIdsDbKey = "set:teamIds";
    private const string PlayerIdsDbKey = "set:playerIds";
    private readonly IRedisDbService _redis;

    public PlayerService(IRedisDbService redis)
    {
        _redis = redis;
    }

    public IEnumerable<Player> GetAllPlayers()
    {
        IDatabase db = _redis.GetConnection();
        return db.SetScan(PlayerIdsDbKey)
            .Select((id) => GetPlayer((string)id!, db))
            .Where(player => player is not null)
            .ToList()!;
    }

    public IEnumerable<int> GetAllTeamIds()
    {
        return _redis.GetConnection()
            .SetScan(TeamIdsDbKey)
            .Select((id) => (int)id)
            .ToList();
    }

    public IEnumerable<Team> GetAllTeams()
    {
        IDatabase db = _redis.GetConnection();
        return _redis.GetConnection()
            .SetScan(TeamIdsDbKey)
            .Select((id) => GetTeam((int)id))
            .Where((team) => team is not null)
            .ToList()!;
    }

    public Player? GetPlayer(string id)
    {
        IDatabase db = _redis.GetConnection();
        return GetPlayer(id, db);
    }

    public Player? GetPlayer(string id, IDatabase db)
    {
        string? s = db.StringGet(this.PlayerKey(id));
        if (s == null)
            return null;

        return JsonSerializer.Deserialize<Player>(s);
    }

    public Team? GetTeam(int id)
    {
        IDatabase db = _redis.GetConnection();
        return GetTeam(id, db);
    }

    public Team? GetTeam(int id, IDatabase db)
    {

        string? s = db.StringGet(this.TeamKey(id));
        if (s == null)
            return null;

        return JsonSerializer.Deserialize<Team>(s);
    }

    public void RegisterPlayer(string id, string name, int teamId)
    {
        IDatabase db = _redis.GetConnection();
        if (!db.SetAdd(PlayerIdsDbKey, id))
            throw new BadApiRequestException("player already registered");

        Player player = new(name, teamId);
        db.StringSet(this.PlayerKey(id), JsonSerializer.Serialize(player));
    }

    public bool RegisterTeam(int teamId, string teamName)
    {
        IDatabase db = _redis.GetConnection();
        if (db.SetAdd(TeamIdsDbKey, teamId))
        {
            Team team = new(teamName, Color.Palette(teamId));
            db.StringSet(this.TeamKey(teamId), JsonSerializer.Serialize(team));
            return true;
        }
        return false;
    }

    private string PlayerKey(string playerId)
    {
        return $"player:{playerId}:";
    }

    private string TeamKey(int teamId)
    {
        return $"team:{teamId}:";
    }

    public void SetTeamName(int id, string name)
    {
        IDatabase db = _redis.GetConnection();
        Team? team = GetTeam(id, db);
        if (team is null)
            throw new BadApiRequestException("team not registered");
        team.Name = name;
        db.StringSet(this.TeamKey(id), JsonSerializer.Serialize(team));
    }
}