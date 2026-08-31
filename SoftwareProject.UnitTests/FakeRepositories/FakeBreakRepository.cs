using Softwareproject.Data;
using Softwareproject.Models;

namespace SoftwareProject.UnitTests.FakeRepositories;

public class FakeBreakRepository : IBreakRepository
{
    private readonly List<Break> _breaks = new();

    public List<Break> GetAll() => _breaks;

    public Break? GetById(int id)
        => _breaks.FirstOrDefault(b => b.Id == id);

    public void Create(Break breakItem)
    {
        _breaks.Add(breakItem);
    }

    public void Update(Break breakItem)
    {
        // Objects are stored by reference, so nothing is needed.
    }

    public void Delete(int id)
    {
        var breakItem = GetById(id);
        if (breakItem != null)
            _breaks.Remove(breakItem);
    }

    public bool Exists(int id)
        => _breaks.Any(b => b.Id == id);
}
