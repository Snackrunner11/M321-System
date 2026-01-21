using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using PixelBoard.MainServer.Models;
using PixelBoard.MainServer.Services;

namespace PixelBoard.MainServer.RestApi;

[ApiController]
[Route("api/color")]
public class BoardApiController : ControllerBase
{
    public const uint BoardWidth = 16;
    public const uint BoardHeight = 16;
    public const uint MaxTeams = 16;

    [HttpGet("{x:int}/{y:int}")]
    public async Task<ActionResult<Color?>> GetPixelColor([FromServices] IBoardService board, int x, int y)
    {
        // Discourage using this API
        await Task.Delay(30);

        if (x == 42 && y == 42)
        {
            return StatusCode(418, "I'm a teapot");
        }
        if (x >= BoardWidth || x < 0 || y >= BoardHeight || y < 0)
        {
            return BadRequest("Out of range");
        }

        return board.GetColor(x, y);
    }

    [HttpPost("")]

    // Removed for solving the task without authorization first
    // [Authorize(AuthenticationSchemes = "JwtBearer")]
    [Consumes("application/json")]
    public IActionResult PostJson(
        [FromServices] IGameService game,
        [FromServices] IPlayerService players,
        [FromBody] PostColorPayload payload
        )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        int x = payload.X;
        int y = payload.Y;
        int team = payload.Team;

        if (x >= BoardWidth || x < 0 || y >= BoardHeight || y < 0)
        {
            return BadRequest("Out of range");
        }
        if (team < 0 || team >= MaxTeams)
        {
            return BadRequest($"team must be between 0 and {MaxTeams}");
        }

        // Use the "team" claim as parsed from the OIDC JWT to check if the user
        // is authorized to make a move for the team specified in the request.
        // Removed for solving the task without authorization first
        // var identity = HttpContext.User.Identity as System.Security.Claims.ClaimsIdentity;
        // string? authorizedTeam = identity?.FindFirst("team")?.Value;
        // string? userId = identity?.FindFirst("sub")?.Value;
        // if (authorizedTeam != "*" && authorizedTeam != team.ToString())
        //     return Unauthorized($"Not allowed to make a move for team {team}");
        // if (userId == null)
        //     return BadRequest("missing sub claim");

        // Player? player = players.GetPlayer(userId);
        // if (player is null)
            // return BadRequest("User not registered");
        // if (player.Team != team)
            // return BadRequest("Player registered with another team");

        try
        {
            game.MakeMove(x, y, team);
        }
        catch (InvalidOperationException ex)
        {
            // Operation is not valid with the current state, for example the
            // team is on cooldown to make a move.
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return StatusCode(500, new { error = "Internal Server Error" });
        }

        return Ok("Ok");
    }
}

public class PostColorPayload
{
    [Required]
    public int X { get; set; }
    [Required]
    public int Y { get; set; }
    [Required]
    public int Team { get; set; }
}