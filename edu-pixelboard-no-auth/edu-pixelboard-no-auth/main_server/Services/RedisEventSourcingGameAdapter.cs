// note: ChatGPT 3.5 wrote almost all of the code in this file.

using System.Text.Json;
using System.Text.Json.Nodes;
using StackExchange.Redis;

namespace PixelBoard.MainServer.Services;

public class RedisEventSourcingGameAdapter : IGameService, IArchiveService
{
    private static readonly string RedisKeyPrefix = "Game-V4";
    private static readonly string RedisKeyKey = "string:CurrentGameV4:";
    private readonly IRedisDbService _redisConnection;
    private readonly IDatabase _redisDatabase;
    private readonly IGameService _originalGameService;
    private readonly ILogger<RedisEventSourcingGameAdapter> _logger;

    public RedisEventSourcingGameAdapter(
           IRedisDbService redisConnection,
           IGameService originalGameService,
           ILogger<RedisEventSourcingGameAdapter> logger)
    {
        _redisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
        _redisDatabase = _redisConnection.GetConnection();
        _originalGameService = originalGameService ?? throw new ArgumentNullException(nameof(originalGameService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Automatically replay events when the adapter is constructed
        ReplayEvents();
    }

    public void MakeMove(int x, int y, int team)
    {
        // Forward the request to the original game service
        _originalGameService.MakeMove(x, y, team);

        // Persist the move event to Redis
        var moveEvent = new MoveEvent { X = x, Y = y, Team = team, Timestamp = DateTime.UtcNow };
        StoreEvent(moveEvent);
    }

    public void Start(IEnumerable<int> teamIds)
    {
        // Forward the request to the original game service
        _originalGameService.Start(teamIds);

        // if it was accepted, rotate to a new game on DB
        GenerateNewGameKey();

        // Persist the start event to Redis
        var startEvent = new StartEvent { TeamIds = teamIds.ToList(), Timestamp = DateTime.UtcNow };
        StoreEvent(startEvent);
    }

    public void Stop()
    {
        // Forward the request to the original game service
        _originalGameService.Stop();

        // Persist the stop event to Redis
        var stopEvent = new StopEvent { Timestamp = DateTime.UtcNow };
        StoreEvent(stopEvent);
    }

    public void Reset()
    {
        // Forward the request to the original game service
        _originalGameService.Reset();

        // Persist the reset event to Redis
        var resetEvent = new ResetEvent { Timestamp = DateTime.UtcNow };
        StoreEvent(resetEvent);
    }

    public void Tick()
    {
        // Forward the request to the original game service
        _originalGameService.Tick();

        // Persist the tick event to Redis
        var tickEvent = new TickEvent { Timestamp = DateTime.UtcNow };
        StoreEvent(tickEvent);
    }

    public Dictionary<string, string?>? GetTeamInfo(int team)
    {
        return _originalGameService.GetTeamInfo(team);
    }

    public GameState GetGameState()
    {
        return _originalGameService.GetGameState();
    }

    private void StoreEvent(GameEvent gameEvent)
    {
        var serializedEvent = gameEvent.Serialize();
        _redisDatabase.ListRightPush(GetCurrentGameKey(), serializedEvent);
    }

    private void ReplayEvents()
    {
        ReplayEvents(GetCurrentGameKey());
    }

    private void ReplayEvents(string key)
    {
        // Retrieve and replay events from Redis
        var events = _redisDatabase.ListRange(key);

        // Check if the last event is a reset event
        if (events.Any() && GameEvent.Deserialize(events.Last()!) is ResetEvent)
        {
            _logger.LogInformation("Skipping replay of the last event because it is a reset event");
            events = events[0..^1];
        }

        _logger.LogInformation("Replaying {0} events", events.Count());
        foreach (var serializedEvent in events)
        {
            _logger.LogDebug("Replaying {0}", serializedEvent);
            try
            {
                var gameEvent = GameEvent.Deserialize(serializedEvent!);
                try
                {
                    ReplayEvent(gameEvent);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning("Replaying {0} failed due to {1}", serializedEvent, ex.Message);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Parsing {0} failed due to {1}", serializedEvent, ex.Message);
            }
        }
    }

    private void ReplayEvent(GameEvent? gameEvent)
    {
        // Replay the event by calling the appropriate method on the original game service
        switch (gameEvent)
        {
            case MoveEvent moveEvent:
                _originalGameService.MakeMove(moveEvent.X, moveEvent.Y, moveEvent.Team);
                break;
            case StartEvent startEvent:
                _originalGameService.Start(startEvent.TeamIds);
                break;
            case StopEvent _:
                _originalGameService.Stop();
                break;
            case ResetEvent _:
                _originalGameService.Reset();
                break;
            case TickEvent _:
                _originalGameService.Tick();
                break;
            default:
                _logger.LogWarning($"Unknown event type: {gameEvent?.GetType().Name}");
                break;
        }
    }

    // IArchiveService implementation

    public void ArchiveGame(string archiveKey)
    {
        var currentGameKey = GetCurrentGameKey();
        var events = _redisDatabase.ListRange(currentGameKey);
        _redisDatabase.ListLeftPush(archiveKey, events);
        _redisDatabase.KeyDelete(currentGameKey);
    }

    public void LoadGame(string archiveKey)
    {
        if (_originalGameService.GetGameState() != GameState.Init)
            throw new InvalidOperationException("Can't load while game is not reset");

        _redisDatabase.StringSet(RedisKeyKey, archiveKey);
        // Replay events from the specified archive key
        ReplayEvents(archiveKey);
    }

    public List<string> GetAllGameKeys()
    {
        var server = _redisConnection.GetServer();
        var keys = server.Keys(pattern: $"{RedisKeyPrefix}*");
        return keys.Select(k => k.ToString()).ToList();
    }

    public void DeleteGame(string archiveKey)
    {
        _redisDatabase.KeyDelete(archiveKey);
    }

    public void InitializeGameFromArchive(string archiveKey)
    {
        // Replay events from the specified archive key to initialize the game
        ReplayEvents(archiveKey);
    }

    // Helper methods

    private string GetCurrentGameKey()
    {
        string? key = _redisDatabase.StringGet(RedisKeyKey);
        return key ?? GenerateNewGameKey();
    }

    private string GenerateNewGameKey()
    {
        string newKey = $"{RedisKeyPrefix}:{DateTime.UtcNow:yyyyMMddHHmmss}";
        _redisDatabase.StringSet(RedisKeyKey, newKey);
        return newKey;
    }

    public uint? Lives(int x, int y)
    {
        return _originalGameService.Lives(x, y);
    }
}

public abstract class GameEvent
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; protected set; } = string.Empty; // Ensure EventType is not null

    protected GameEvent() // Protected parameterless constructor
    {
        Timestamp = DateTime.UtcNow;
        EventType = this.GetType().Name; // Set the concrete class name
    }

    // Custom serialization method for GameEvent
    public virtual string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    // Static deserialize method to cover all known concrete types
    public static GameEvent Deserialize(string serializedEvent)
    {
        var jsonObject = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serializedEvent);
        string eventType = jsonObject!["EventType"].ToString();

#pragma warning disable CS8603 // Possible null reference return.
        return eventType switch
        {
            nameof(MoveEvent) => JsonSerializer.Deserialize<MoveEvent>(serializedEvent),
            nameof(StartEvent) => JsonSerializer.Deserialize<StartEvent>(serializedEvent),
            nameof(StopEvent) => JsonSerializer.Deserialize<StopEvent>(serializedEvent),
            nameof(ResetEvent) => JsonSerializer.Deserialize<ResetEvent>(serializedEvent),
            nameof(TickEvent) => JsonSerializer.Deserialize<TickEvent>(serializedEvent),
            _ => throw new InvalidOperationException("Unknown event type during deserialization.")
        };
#pragma warning restore CS8603 // Possible null reference return.
    }
}

public class MoveEvent : GameEvent
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Team { get; set; }

    public override string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }
}

public class StartEvent : GameEvent
{
    public List<int> TeamIds { get; set; } = new List<int>();

    public override string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }
}

public class StopEvent : GameEvent
{
    public override string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }
}

public class ResetEvent : GameEvent
{

    public override string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }
}

public class TickEvent : GameEvent
{

    public override string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }
}
