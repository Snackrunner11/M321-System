namespace PixelBoard.MainServer.Models;

public class Player
{
    public string Name { get; set; }
    public int Team { get; set; }

    public Player(string name, int team)
    {
        this.Name = name;
        this.Team = team;
    }
}