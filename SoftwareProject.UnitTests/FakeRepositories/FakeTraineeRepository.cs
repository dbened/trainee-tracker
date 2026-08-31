using Softwareproject.Data;
using Softwareproject.Models;

namespace SoftwareProject.UnitTests.FakeRepositories;

public class FakeTraineeRepository : ITraineeRepository
{
    private readonly List<Trainee> _trainees = new();

    // Convenience alias für Test-Setup, ruft intern Create() auf.
    public void Add(Trainee trainee) => Create(trainee);

    public List<Trainee> GetAll()
    {
        return _trainees;
    }

    public Trainee? GetById(int id)
        => _trainees.FirstOrDefault(t => t.Id == id);

    public List<Trainee>? GetByMentorId(int mentorId)
        => _trainees.Where(t => t.MentorList.Any(m => m.Id == mentorId)).ToList();

    public Trainee? GetByIdWithLessonPlan(int id)
    {
        return _trainees.FirstOrDefault(t => t.Id == id);
    }

    public Trainee? GetByIdWithMentors(int id)
        => _trainees.FirstOrDefault(t => t.Id == id);

    public Trainee? GetByEmailWithMentors(string email)
        => _trainees.FirstOrDefault(t => t.EmailAddress == email);

    public Trainee? GetByEmail(string email)
        => _trainees.FirstOrDefault(t => t.EmailAddress == email);

    public void Create(Trainee trainee)
    {
        _trainees.Add(trainee);
    }

    public void Update(Trainee trainee)
    {
        // Objects are stored by reference, so nothing is needed.
    }

    public void Delete(int id)
    {
        var trainee = GetById(id);
        if (trainee != null)
            _trainees.Remove(trainee);
    }

    public bool Exists(int id)
        => _trainees.Any(t => t.Id == id);
}
