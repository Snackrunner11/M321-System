using PixelBoard.MainServer.Models;
using PixelBoard.MainServer.Services;

/// <summary>
/// A representation of the pixel board for use in snapshot testing.
/// </summary>
public class BoardSnapshot
{
    public int[,] Fields { get; set; }
    private Dictionary<Color, int> _colorToTeam { get; set; }

    public BoardSnapshot(IReadBoardService board)
    {
        _colorToTeam = new();
        // TODO: config 16?
        Fields = new int[16, 16];
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                Color? color = board.GetColor(x, y);
                if (color is null)
                    continue;
                if (!_colorToTeam.TryGetValue(color, out int team))
                {
                    team = _colorToTeam.Count + 1;
                    _colorToTeam[color] = team;
                }
                Fields[x, y] = team;
            }
        }
    }

    public string ToAscii()
    {
        var sb = new System.Text.StringBuilder();
        int asciiBase = (int)'a' - 1;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int fieldValue = Fields[x, y];
                if (fieldValue == 0)
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append((char)(fieldValue + asciiBase));
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}