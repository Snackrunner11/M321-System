
using Microsoft.Extensions.Options;
using PixelBoard.MainServer.Configuration;

namespace PixelBoard.MainServer.Services;

public class TickService : BackgroundService
{

    private PeriodicTimer? _timer;
    private readonly ILogger<TickService> _logger;
    private readonly IGameService _game;
    PadukOptions _options;

    public TickService(ILogger<TickService> logger,
                            IOptions<PadukOptions> options,
                            IGameService game
                            )
    {
        _logger = logger;
        _options = options.Value;
        _game = game;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _timer = new(TimeSpan.FromMilliseconds(_options.TickDelayMs));
        while (await _timer.WaitForNextTickAsync(cancellationToken))
        {
            TickCallback();
        }
    }


    private void TickCallback()
    {
        if (_game.GetGameState() == GameState.Active)
        {

            try
            {
                _game.Tick();
            }
            catch
            {
                _logger.TickCrashed();
            }
        }
    }

}

