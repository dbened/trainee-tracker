namespace Softwareproject.Services;

public interface IStatisticsRefreshService
{
    Task RefreshAsync(int traineeId);
    Task RefreshAllAsync();
}
