using Microsoft.AspNetCore.Mvc;

namespace PixelBoard.MainServer.RestApi;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    [HttpGet("")]
    public string Api()
    {
        return "Rest API for PixelBoard running";
    }
}
