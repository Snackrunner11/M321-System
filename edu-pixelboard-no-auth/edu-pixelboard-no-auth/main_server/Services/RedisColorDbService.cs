using System.Text.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using StackExchange.Redis;
using PixelBoard.MainServer.Models;

namespace PixelBoard.MainServer.Services;

public class RedisColorDbService : IBoardService
{
    private readonly IRedisDbService _redis;
    private readonly Subject<Pixel> _pixelChanges = new Subject<Pixel>();

    public RedisColorDbService(IRedisDbService redis)
    {
        _redis = redis;
    }

    public Color? GetColor(int x, int y)
    {
        IDatabase db = _redis.GetConnection();

        string? s = db.StringGet(this.Key(x, y));
        if (s == null)
        {
            return null;
        }

        // simplistic coloring if a single number was stored (for testing)
        byte red;
        bool isNumber = byte.TryParse(s, out red);
        if (isNumber)
        {
            return new Color(red, 0, 0);
        }

        // otherwise, expect a serialized `Color`
        return JsonSerializer.Deserialize<Color>(s);
    }

    public void SetColor(int x, int y, Color color)
    {
        IDatabase db = _redis.GetConnection();
        db.StringSet(this.Key(x, y), JsonSerializer.Serialize(color));
        _pixelChanges.OnNext(new Pixel(x, y, color));
    }

    public void DeleteColor(int x, int y)
    {
        IDatabase db = _redis.GetConnection();
        db.KeyDelete(this.Key(x, y));
        _pixelChanges.OnNext(new Pixel(x, y, Color.Black()));
    }

    public IObservable<Pixel> PixelChanges() => _pixelChanges.AsObservable();

    private string Key(int x, int y)
    {
        return $"color:({x}|{y})";
    }
}