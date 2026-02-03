using System.Reactive.Linq;
using GraphQL;
using PixelBoard.MainServer.Models;

namespace PixelBoard.MainServer.Services;

public class BoardQuery
{
    static public GqlPixel Pixel(int x, int y)
    {
        return new(x, y);
    }

    static public GqlPixel[][] PixelRange(GqlRange xRange, GqlRange yRange)
    {
        return Enumerable
            .Range(xRange.Start, xRange.End - xRange.Start + 1)
            .Select(x =>
                Enumerable
                    .Range(yRange.Start, yRange.End - yRange.Start + 1)
                    .Select(y => new GqlPixel(x, y))
                    .ToArray()
            )
            .ToArray();
    }

    static public GqlTeam? Team([FromServices] IPlayerService players, [FromServices] IGameService game, int id)
    {
        return new(id, players, game);
    }
}

public class BoardSubscription
{
    static public IObservable<Pixel> PixelChanged([FromServices] IReadBoardService readBoardService)
    {
        return readBoardService.PixelChanges();
    }
}

[Name("ExtendedPixel")]
public class GqlPixel
{
    public int X { get; set; }
    public int Y { get; set; }
    public Color Color([FromServices] IReadBoardService readBoardService)
    {
        return readBoardService.GetColor(X, Y) ?? Models.Color.Black();
    }

    public uint? Lives([FromServices] IGameService game)
    {
        return game.Lives(X, Y);
    }

    public GqlPixel(int x, int y)
    {
        X = x;
        Y = y;
    }
}

[Name("Team")]
public class GqlTeam
{
    public int Id { get; set; }

    private readonly Lazy<Team?> _lazyTeam;
    private readonly Lazy<Dictionary<string, string?>?> _lazyTeamInfo;

    public GqlTeam(int id, [FromServices] IPlayerService players, [FromServices] IGameService game)
    {
        Id = id;
        _lazyTeam = new Lazy<Team?>(() => players.GetTeam(Id));
        _lazyTeamInfo = new Lazy<Dictionary<string, string?>?>(() => game.GetTeamInfo(Id));
    }

    public string? Name()
    {
        return _lazyTeam.Value?.Name;
    }

    public Color? Color()
    {
        return _lazyTeam.Value?.Color;
    }

    public int? PaintBudget()
    {
        return int.TryParse(_lazyTeamInfo.Value?.GetValueOrDefault("PaintBudget"), out int budget) ? budget : null;
    }

    public int? Score()
    {
        return int.TryParse(_lazyTeamInfo.Value?.GetValueOrDefault("Score"), out int score) ? score : null;
    }

    public IEnumerable<GqlPlayer> Players([FromServices] IPlayerService players)
    {
        return players.GetAllPlayers().Where(p => p.Team == this.Id).Select(p => new GqlPlayer(p.Name));
    }
}

[Name("Player")]
public class GqlPlayer
{
    public string Name { get; set; }

    public GqlPlayer(string name)
    {
        this.Name = name;
    }
}

[Name("Range")]
public class GqlRange
{
    public int Start { get; set; }
    public int End { get; set; }

    public GqlRange(int start, int end)
    {
        this.Start = start;
        this.End = end;
    }
}
