using Softwareproject.Data;
using Softwareproject.Models;

namespace SoftwareProject.UnitTests.FakeRepositories;

public class FakeProgressRepository : ITraineeLessonProgressRepository
{
    private readonly List<TraineeLessonProgress> _progresses = new();

    // Convenience alias für Test-Setup, ruft intern Create() auf.
    public void Add(TraineeLessonProgress progress) => Create(progress);

    public TraineeLessonProgress? GetById(int id)
        => _progresses.FirstOrDefault(p => p.Id == id);

    public List<TraineeLessonProgress> GetAll()
    {
        return _progresses;
    }

    public void Create(TraineeLessonProgress progress)
    {
        _progresses.Add(progress);
    }

    public void Update(TraineeLessonProgress progress)
    {
        // Objects are stored by reference, so nothing is needed.
    }

    public void Delete(int id)
    {
        var progress = GetById(id);
        if (progress != null)
            _progresses.Remove(progress);
    }

    public bool Exists(int id)
        => _progresses.Any(p => p.Id == id);
}
