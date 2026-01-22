using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PixelBoard.MainServer.Models;
using PixelBoard.MainServer.Services;

namespace PixelBoard.TeacherClient;

// [Authorize(Policy = "Admin")]
public class AdminModel : PageModel
{
    public List<TeamModel> Teams { get; set; }
    public List<string> Games { get; }

    private readonly ILogger<AdminModel> _logger;

    public AdminModel(ILogger<AdminModel> logger, IPlayerService players, IArchiveService archive)
    {
        _logger = logger;
        var allPlayers = players.GetAllPlayers();
        Teams = players.GetAllTeamIds()
            .Select((team) => new TeamModel(team, players.GetTeam(team), allPlayers))
            .ToList();

        Games = archive.GetAllGameKeys();
    }

    public ActionResult OnGet()
    {
        return Page();
    }

    public void OnPostStart([FromServices] IGameService game, [FromServices] IPlayerService players)
    {
        try
        {
            game.Start(players.GetAllTeamIds());
            _logger.LogWarning("Game started");
        }
        catch (Exception exception)
        {
            _logger.LogError("{}", exception);
        }
    }

    public void OnPostStop([FromServices] IGameService game)
    {
        try
        {

            game.Stop();
            _logger.LogWarning("Game stopped");
        }
        catch (Exception exception)
        {
            _logger.LogError("{}", exception);
        }
    }

    public void OnPostReset([FromServices] IGameService game)
    {
        try
        {
            game.Reset();
            _logger.LogWarning("Game reset");
        }
        catch (Exception exception)
        {
            _logger.LogError("{}", exception);
        }
    }

    public void OnPostLoadGame([FromServices] IArchiveService archive, string game)
    {
        _logger.LogWarning("Rotate to {0}", game);
        try
        {
            archive.LoadGame(game);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load game. {ex.Message})");
            ModelState.AddModelError(
                    string.Empty,
                    $"Failed to load game. {ex.Message}"
                );
        }
    }

    public class TeamModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Player> Players { get; set; }

        public TeamModel(int teamId, Team? team, IEnumerable<Player> allPlayers)
        {
            Id = teamId;
            Name = team?.Name ?? "Team not found";
            Players = allPlayers.Where((p) => p.Team == Id).ToList();
        }
    }
}
