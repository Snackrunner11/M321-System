namespace PixelBoard.MainServer.Configuration;

public class BoardOptions
{
    public int BoardWidth { get; set; }
    public int BoardHeight { get; set; }

    public BoardOptions()
    {
        BoardWidth = 16;
        BoardHeight = 16;
    }
}