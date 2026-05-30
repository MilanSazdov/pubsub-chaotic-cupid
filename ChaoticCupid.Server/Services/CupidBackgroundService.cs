using ChaoticCupid.Server.Contracts;

namespace ChaoticCupid.Server.Services;

public sealed class CupidBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly ICupid _cupid;
    private readonly ILogger<CupidBackgroundService> _log;

    public CupidBackgroundService(ICupid cupid, ILogger<CupidBackgroundService> log)
    {
        _cupid = cupid;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("[CUPID] timer started, interval {Interval}", Interval);
        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await _cupid.DeliverLettersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "[CUPID] DeliverLettersAsync failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
