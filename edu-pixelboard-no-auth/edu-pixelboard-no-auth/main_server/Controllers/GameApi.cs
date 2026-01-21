using Microsoft.AspNetCore.Mvc;
using PixelBoard.MainServer.Services;

namespace PixelBoard.MainServer.RestApi;

[ApiController]
[Route("api/game")]
public class GameApiController : ControllerBase
{
    [HttpGet("")]
    public string Api()
    {
        return "Game API is live.";
    }

    [HttpGet("team/{id:int}")]
    public ActionResult<Dictionary<string, string?>> GetTeamInfo([FromServices] IGameService game, int id)
    {
        // TODO: Protect this info from other teams outside of grpc? (Maybe just use an implicit ID)
        return game.GetTeamInfo(id) switch
        {
            Dictionary<string, string> info => Ok(info),
            null => NotFound(),
        };
    }
}
