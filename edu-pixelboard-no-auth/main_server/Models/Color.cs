namespace PixelBoard.MainServer.Models;

public class Color
{
    public byte Red { get; set; }
    public byte Green { get; set; }
    public byte Blue { get; set; }

    public Color(byte Red, byte Green, byte Blue)
    {
        this.Red = Red;
        this.Green = Green;
        this.Blue = Blue;
    }

    private static readonly Color[] Colors = {
        new Color(255,244,0),
        new Color(0,11,255),
        new Color(105,36,148),
        new Color(79,148,36),
        new Color(255,173,167),
        new Color(167,249,255),
    };

    [GraphQL.Ignore]
    public static Color Palette(int index)
    {
        index = Math.Abs(index);
        Color baseColor = Colors[index % Colors.Length];
        int shade = index / Colors.Length;
        if (shade == 0)
        {
            return baseColor;
        }
        int factor = shade + 1;
        return new Color(
            (byte)(baseColor.Red / factor),
            (byte)(baseColor.Green / factor),
            (byte)(baseColor.Blue / factor)
        );
    }

    internal static Color Black()
    {
        return new Color(0, 0, 0);
    }
}