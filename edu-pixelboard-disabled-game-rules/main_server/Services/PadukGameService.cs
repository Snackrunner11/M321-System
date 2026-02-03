using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using PixelBoard.MainServer.Models;
using PixelBoard.MainServer.Configuration;
using PixelBoard.MainServer.Utils;

namespace PixelBoard.MainServer.Services;

public class PadukGameService : IGameService
{
    private readonly PadukOptions _options;

    private readonly ILogger<PadukGameService> _logger;
    private readonly IBoardService _displayedBoard;

    private ConcurrentDictionary<int, TeamInfo> _teams;

    private int?[,] _board;

    private HashSet<(int, int)> _blockedFields;

    private GameState _gameState = GameState.Init;

    private int _tickCounter;

    public PadukGameService(ILogger<PadukGameService> logger,
                            IBoardService boardService,
                            IOptions<PadukOptions> options,
                            IOptions<BoardOptions> boardOptions)
    {
        _options = options.Value;
        _displayedBoard = boardService;
        _board = new int?[boardOptions.Value.BoardWidth, boardOptions.Value.BoardHeight];
        _teams = new();
        _blockedFields = new();
        _logger = logger;
        _logger.LogInformation("Started Paduk game");
    }

    public void Start(IEnumerable<int> teamIds)
    {
        if (_gameState != GameState.Init)
            throw new InvalidOperationException("The game has already started.");
        _teams = new ConcurrentDictionary<int, TeamInfo>(
            teamIds.ToDictionary(teamId => teamId, teamId => new TeamInfo(_options.StartBudget))
        );
        SetInitialColors();
        _gameState = GameState.Active;
    }

    public void Stop()
    {
        if (_gameState != GameState.Active)
            throw new InvalidOperationException("The game is not running.");
        Tick();
        _gameState = GameState.Done;
    }

    public void Reset()
    {
        if (_gameState != GameState.Done)
            throw new InvalidOperationException("The game has not finished.");
        _gameState = GameState.Init;
        DeleteAllColors();
        _teams = [];
    }
    public void Tick()
    {
        if (_gameState != GameState.Active)
            throw new InvalidOperationException("The game is not running.");
        _tickCounter++;

        for (int x = 0; x < _board.GetLength(0); x++)
        {
            for (int y = 0; y < _board.GetLength(1); y++)
            {
                int? team = _board[x, y];
                if (team is not null)
                {
                    lock (_teams)
                    {
                        _teams[team.Value].Score += 1;
                    }
                }
            }
        }
        if (_tickCounter % _options.BudgetIncreaseDelay == 0)
        {
            _logger.BudgetIncrease(_tickCounter);
            lock (_teams)
            {
                foreach (TeamInfo info in _teams.Values)
                {
                    info.IncrementPaintBudget(_options.BudgetIncreaseSize, _options.MaxBudget);
                }
            }
        }
    }

    public void MakeMove(int x, int y, int team)
    {
        if (_gameState is not GameState.Active)
            throw new InvalidOperationException("The game is not ready to accept moves.");

        lock (_teams)
        {
            TeamInfo? info;
            _teams.TryGetValue(team, out info);
            if (info is null)
                throw new InvalidOperationException($"Team {team} is not registered.");
            if (!info.DecrementPaintBudget(1))
                throw new InvalidOperationException($"Team {team} has no paint to make a move.");

            bool blocked = !_blockedFields.Add((x, y));
            if (blocked)
            {
                info.IncrementPaintBudget(1, _options.MaxBudget);
                throw new InvalidOperationException($"Field ({x}|{y}) is blocked.");
            }
        }

        SetField(x, y, team);

        // Check if any components have died due to the new move.
        ComponentScanner neighborScanner = new(_board);
        HashSet<uint> deadComponents = new();
        foreach ((int neighborX, int neighborY) in ComponentScanner.Neighbors(x, y))
        {
            uint? lives = neighborScanner.CountLives(neighborX, neighborY);
            if (lives == 0)
            {
                uint? component = neighborScanner.GetComponent(neighborX, neighborY);
                if (component is not null)
                {
                    deadComponents.Add(component.Value);
                }
            }
        }
        foreach (uint component in deadComponents)
        {
            RemoveComponent(neighborScanner, component);
        }

        // Check if the newly placed stone is alive.
        // (need a new scanner because the board has changed)
        ComponentScanner scanner = new(_board);
        if (0 == scanner.CountLives(x, y))
        {
            uint? component = scanner.GetComponent(x, y);
            if (component is not null)
            {
                RemoveComponent(scanner, component.Value);
            }
        }
    }

    public uint? Lives(int x, int y)
    {
        ComponentScanner scanner = new(_board);
        return scanner.CountLives(x, y);
    }

    private void RemoveComponent(ComponentScanner scanner, uint component)
    {
        HashSet<int> adjacentTeams = scanner.GetAdjacentTeams(component);
        if (adjacentTeams.Count == 1)
        {
            int capturer = adjacentTeams.First();
            foreach ((int x, int y) in scanner.GetComponentFields(component))
            {
                SetField(x, y, capturer);
            }
        }
        else
        {
            foreach ((int x, int y) in scanner.GetComponentFields(component))
            {
                DeleteField(x, y);
            }
        }
    }

    private void SetField(int x, int y, int team)
    {
        _board[x, y] = team;
        _displayedBoard.SetColor(x, y, Color.Palette(team));
    }

    private void DeleteField(int x, int y)
    {
        _board[x, y] = null;
        _displayedBoard.DeleteColor(x, y);
        _blockedFields.Remove((x,y));
    }


    private void SetInitialColors()
    {
        if (_options.StartWithCheckerboard)
        {

            Color color1 = new Color(150, 150, 150);
            Color color2 = new Color(100, 100, 100);
            for (int x = 0; x < _board.GetLength(0); x++)
            {
                for (int y = 0; y < _board.GetLength(1); y++)
                {
                    Color c = (x + y) % 2 == 0 ? color1 : color2;
                    _board[x, y] = null;
                    _displayedBoard.SetColor(x, y, c);
                }
            }
        }
    }

    private void DeleteAllColors()
    {
        for (int x = 0; x < _board.GetLength(0); x++)
        {
            for (int y = 0; y < _board.GetLength(1); y++)
            {
                DeleteField(x, y);
            }
        }
    }

    public Dictionary<string, string?>? GetTeamInfo(int team)
    {
        lock (_teams)
        {
            if (_teams.TryGetValue(team, out var info))
            {
                return info.ToDictionary();
            }
        }
        return null;
    }

    public GameState GetGameState()
    {
        return this._gameState;
    }

    /// <summary>
    /// Private class to define in-memory data layout per team.
    /// Don't use in other classes.
    /// </summary>
    private class TeamInfo
    {
        private int _paintBudget;
        public int Score { get; set; }
        private readonly object _lock = new object();

        public int PaintBudget
        {
            get
            {
                lock (_lock)
                {
                    return _paintBudget;
                }
            }
            set
            {
                lock (_lock)
                {
                    _paintBudget = value;
                }
            }
        }

        public TeamInfo(int startBudget)
        {
            Score = 0;
            _paintBudget = startBudget;
        }

        public Dictionary<string, string?> ToDictionary()
        {
            return new Dictionary<string, string?>() {
                {"Score",Score.ToString()},
                {"PaintBudget",PaintBudget.ToString()},
            };
        }

        public bool DecrementPaintBudget(int amount)
        {
            lock (_lock)
            {
                if (_paintBudget >= amount)
                {
                    _paintBudget -= amount;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal void IncrementPaintBudget(int amount, int maxBudget)
        {
            lock (_lock)
            {
                _paintBudget = Math.Min(_paintBudget + amount, maxBudget);
            }
        }
    }
}

public static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Budget increase at tick {tick}")]
    public static partial void BudgetIncrease(this ILogger logger, int tick);

    [LoggerMessage(Level = LogLevel.Error, Message = "Tick crashed")]
    public static partial void TickCrashed(this ILogger logger);
}