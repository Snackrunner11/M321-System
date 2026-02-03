using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PixelBoard.MainServer.Models;
using PixelBoard.MainServer.Services;
using System.Security.Claims;

namespace PixelBoard.TeacherClient;

[Authorize]
public class PlayerModel : PageModel
{
    public Player? Me { get; set;}
    public Team? Team { get; set;}
    public string? KcName { get; set;}

    private readonly ILogger<PlayerModel> _logger;
    private readonly IPlayerService _players;

    public PlayerModel(ILogger<PlayerModel> logger, IPlayerService players)
    {
        this._logger = logger;
        this._players = players;
    }

    public ActionResult OnGet()
    {
        string? id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (id is not null)
        {
            this.Me = _players.GetPlayer(id);
        }
        if (this.Me is not null)
        {
            this.Team = _players.GetTeam(this.Me.Team);
        }
        this.KcName = User.FindFirst("preferred_username")?.Value;
        

        return Page();
    }
}
