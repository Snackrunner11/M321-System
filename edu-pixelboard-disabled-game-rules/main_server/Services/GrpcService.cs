using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using PixelBoard.MainServer.Services;
using PixelBoard.Grpc;

namespace PixelBoard.MainServer.Grpc;

public class GrcpService : PadukGrpc.PadukGrpcBase
{
    private readonly PadukGameService _game;
    public GrcpService(PadukGameService game)
    {
        _game = game;
    }

    public override Task<TeamInfoReply> GetTeamInfo(TeamInfoRequest request, ServerCallContext context)
    {
        Dictionary<string, string?>? info = _game.GetTeamInfo(request.Team);

        if (info is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Team {request.Team} not found."));

        string? score = info["Score"];
        string? budget = info["PaintBudget"];

        if (score is null || budget is null)
            throw new DbCorruptException("Team Info has missing fields");

        TeamInfoReply reply = new TeamInfoReply
        {
            Score = uint.Parse(score),
            PaintBudget = uint.Parse(budget),
        };
        return Task.FromResult(reply);
    }
}