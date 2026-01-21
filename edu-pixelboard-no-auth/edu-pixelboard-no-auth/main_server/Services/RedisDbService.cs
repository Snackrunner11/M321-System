using StackExchange.Redis;

namespace PixelBoard.MainServer.Services;

public class RedisDbService : IRedisDbService
{
    private readonly ConnectionMultiplexer _redis;

    public RedisDbService(IConfiguration configuration)
    {
        string redisUrl = configuration.GetValue<string>("Redis") ?? "localhost";
        _redis = ConnectionMultiplexer.Connect(redisUrl);
    }

    public IDatabase GetConnection()
    {
        return _redis.GetDatabase();
    }

    public IServer GetServer()
    {
        return _redis.GetServer(_redis.GetEndPoints()[0]);
    }
}