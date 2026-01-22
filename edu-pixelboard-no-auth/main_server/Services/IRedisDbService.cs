using StackExchange.Redis;

namespace PixelBoard.MainServer.Services;

public interface IRedisDbService
{
    public IDatabase GetConnection();
    public IServer GetServer();
}