using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelBoard.MainServer.Models;
using PixelBoard.MainServer.Services;

namespace PixelBoard.MainServer.RestApi;

[ApiController]
[Route("api/team")]
public class TeamApiController : ControllerBase
{
    private const string TeamIdsDbKey = "set:teamIds";

    [HttpGet("")]
    public IEnumerable<Team> GetTeams([FromServices] IPlayerService db)
    {
        return db.GetAllTeams();
    }

    [HttpGet("{id:int}")]
    public ActionResult<Team> GetTeam([FromServices] IPlayerService db, int id)
    {
        return db.GetTeam(id) switch
        {
            Team team => Ok(team),
            null => NotFound(),
        };
    }

    [Authorize(AuthenticationSchemes = "JwtBearer")]
    [HttpPut("name")]
    public ActionResult<Team> PutName([FromServices] IPlayerService db, [FromBody] string TeamName)
    {
        if (TeamName.Length < 3)
        {
            return BadRequest("Team name must be at least 3 characters.");
        }
        if (TeamName.Length > 64)
        {
            return BadRequest("Team name must be at most 64 characters.");
        }
        // Use the "team" claim as parsed from the OIDC JWT
        var identity = HttpContext.User.Identity as System.Security.Claims.ClaimsIdentity;
        string? team = identity?.FindFirst("team")?.Value;

        if (int.TryParse(team, out int teamId))
        {
            try
            {
                db.SetTeamName(teamId, TeamName);
            }
            catch (BadApiRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok();
        }
        else
        {
            return BadRequest("Invalid team id");
        }
    }

    [Authorize(AuthenticationSchemes = "JwtBearer")]
    [HttpPost("register")]
    public ActionResult<Team> PostRegister([FromServices] IPlayerService db, [FromBody] string TeamName)
    {
        if (TeamName.Length < 3)
        {
            return BadRequest("Team name must be at least 3 characters.");
        }
        if (TeamName.Length > 64)
        {
            return BadRequest("Team name must be at most 64 characters.");
        }
        // Use the "team" claim as parsed from the OIDC JWT
        var identity = HttpContext.User.Identity as System.Security.Claims.ClaimsIdentity;
        string? team = identity?.FindFirst("team")?.Value;

        if (int.TryParse(team, out int teamId))
        {
            try
            {
                if (!db.RegisterTeam(teamId, TeamName))
                {
                    return BadRequest("Team already registered");
                }

            }
            catch (BadApiRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok();
        }
        else
        {
            return BadRequest("Invalid team id");
        }
    }
}
