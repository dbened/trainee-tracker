using Softwareproject.Data;
using Softwareproject.Models;

namespace SoftwareProject.UnitTests.FakeRepositories;

public class FakeFeedbackRepository : IFeedbackRepository
{
    private readonly List<Feedback> _feedbacks = new();

    public List<Feedback> GetAll() => _feedbacks;

    public Feedback? GetById(int id)
        => _feedbacks.FirstOrDefault(f => f.Id == id);

    public void Create(Feedback feedback)
    {
        _feedbacks.Add(feedback);
    }

    public void Update(Feedback feedback)
    {
        // Objects are stored by reference, so nothing is needed.
    }

    public void Delete(int id)
    {
        var feedback = GetById(id);
        if (feedback != null)
            _feedbacks.Remove(feedback);
    }

    public bool Exists(int id)
        => _feedbacks.Any(f => f.Id == id);
}
