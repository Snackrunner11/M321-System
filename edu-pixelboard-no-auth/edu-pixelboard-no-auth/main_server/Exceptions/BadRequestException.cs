namespace PixelBoard.MainServer.Services;

/// <summary>
/// Custom exception thrown in services to be displayed to the API user.
/// </summary>
public class BadApiRequestException : Exception
{
    public BadApiRequestException(string? message) : base(message)
    {
    }
}