using Softwareproject.Data;
using Softwareproject.Models;

namespace SoftwareProject.UnitTests.FakeRepositories;

public class FakeStatisticRepository : IStatisticRepository
{
    private readonly List<Statistic> _statistics = new();

    public void Create(Statistic statistic)
    {
        _statistics.Add(statistic);
    }

    public void Update(Statistic statistic)
    {
        // Nothing to do because the object is already in the list.
    }

    public void Delete(int id)
    {
        var statistic = _statistics.FirstOrDefault(s => s.Id == id);
        if (statistic != null)
            _statistics.Remove(statistic);
    }

    public Statistic? GetLatestForTrainee(int traineeId)
    {
        return _statistics
            .Where(s => s.Trainee != null && s.Trainee.Id == traineeId)
            .OrderByDescending(s => s.Date)
            .FirstOrDefault();
    }

    public List<Statistic> GetHistoryForTrainee(int traineeId)
    {
        return _statistics
            .Where(s => s.Trainee != null && s.Trainee.Id == traineeId)
            .OrderBy(s => s.Date)
            .ToList();
    }

    public List<Statistic> GetAll()
    {
        return _statistics;
    }
}
