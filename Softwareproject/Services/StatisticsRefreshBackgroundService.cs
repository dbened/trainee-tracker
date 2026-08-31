using Softwareproject.Services;

namespace Softwareproject.Services;

public class StatisticsRefreshBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    
    public StatisticsRefreshBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run immediately at startup
        await RefreshStatistics(stoppingToken);

        // Then every 2 hours
        using var timer = new PeriodicTimer(TimeSpan.FromHours(2));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshStatistics(stoppingToken);
        }
    }
    
    private async Task RefreshStatistics(CancellationToken token)
    {
        using var scope = _serviceProvider.CreateScope();

        var refreshService =
            scope.ServiceProvider.GetRequiredService<IStatisticsRefreshService>();

        await refreshService.RefreshAllAsync();
    }
}
