using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using PixelBoard.MainServer.Services;
using PixelBoard.MainServer.Models;


namespace main_server.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IReadBoardService _db;

    public IndexModel(ILogger<IndexModel> logger, IReadBoardService db)
    {
        _logger = logger;
        _db = db;
    }

    public Color?[,] Pixels = new Color[16, 16];

    public void OnGet()
    {
        for (int x = 0; x < Pixels.GetLength(0); x++)
        {
            for (int y = 0; y < Pixels.GetLength(1); y++)
            {
                Pixels[x, y] = this._db.GetColor(x, y);
            }
        }

    }
}
