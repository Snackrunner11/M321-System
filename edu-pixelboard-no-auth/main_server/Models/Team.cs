namespace PixelBoard.MainServer.Models;

public class Team
{
    public string Name { get; set; }
    public Color Color { get; set; }

    public Team(string name, Color color)
    {
        this.Name = name;
        this.Color = color;
    }
}