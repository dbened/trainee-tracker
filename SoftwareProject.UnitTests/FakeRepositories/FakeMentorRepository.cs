using Softwareproject.Data;
using Softwareproject.Models;

namespace SoftwareProject.UnitTests.FakeRepositories;

public class FakeMentorRepository : IMentorRepository
{
    private readonly List<Mentor> _mentors = new();

    public List<Mentor> GetAll() => _mentors;

    public Mentor? GetById(int id)
        => _mentors.FirstOrDefault(m => m.Id == id);

    public Mentor? GetByEmail(string email)
        => _mentors.FirstOrDefault(m => m.EmailAddress == email);

    public void Create(Mentor mentor)
    {
        _mentors.Add(mentor);
    }

    public void Update(Mentor mentor)
    {
    }

    public void Delete(int id)
    {
        var mentor = GetById(id);
        if (mentor != null)
            _mentors.Remove(mentor);
    }

    public bool Exists(int id)
        => _mentors.Any(m => m.Id == id);
}
