using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using PixelBoard.MainServer.Models;
using PixelBoard.MainServer.Services;

namespace PixelBoard.MainServer.RestApi;

[ApiController]
[Route("api/player")]
public class PlayerApiController : ControllerBase
{
    [HttpGet("")]
    public IEnumerable<Player> GetPlayers([FromServices] IPlayerService players)
    {
        return players.GetAllPlayers();
    }

    [HttpGet("{id}")]
    public ActionResult<Player> GetPlayer([FromServices] IPlayerService players, string id)
    {
        return players.GetPlayer(id) switch
        {
            Player p => Ok(p),
            null => NotFound(),
        };
    }


    [Authorize(AuthenticationSchemes = "JwtBearer")]
    [HttpPost("register")]
    public ActionResult<int> PostRegistration([FromServices] IPlayerService players, [FromBody] RegistrationPayload payload)
    {
        // Use the "team" claim as parsed from the OIDC JWT to check if the user
        // is authorized to make a move for the team specified in the request.
        var identity = HttpContext.User.Identity as System.Security.Claims.ClaimsIdentity;
        string? id = identity?.FindFirst("sub")?.Value;
        string? name = identity?.FindFirst("name")?.Value;
        string? team = identity?.FindFirst("team")?.Value;
        int teamNumber;
        bool teamIsNumber = int.TryParse(team, out teamNumber);

        if (id == null || id == "")
            return BadRequest("missing sub claim");
        if (team == null || team == "")
            return Unauthorized($"No team authorization found in token claims");
        if (!teamIsNumber)
            return BadRequest($"Team number could not be parsed from {team}");

        try
        {
            players.Register(id, payload.Name, teamNumber);
        }
        catch (BadApiRequestException ex)
        {
            return BadRequest(ex.Message);
        }

        return Ok($"Registered user {id}");
    }
}

public class RegistrationPayload
{
    [Required]
    public required string Name { get; set; }
}