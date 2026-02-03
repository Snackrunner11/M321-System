namespace PixelBoard.MainServer.Services;

/// <summary>
/// Custom exception thrown when accessing the DB yields invalid data.
/// </summary>
public class DbCorruptException : Exception
{
    public DbCorruptException(string? message) : base(message)
    {
    }
}