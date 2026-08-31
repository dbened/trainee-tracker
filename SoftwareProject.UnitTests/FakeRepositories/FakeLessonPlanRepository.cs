using Softwareproject.Data;
using Softwareproject.Models;

namespace SoftwareProject.UnitTests.FakeRepositories;

public class FakeLessonPlanRepository : ILessonPlanRepository
{
    private readonly List<LessonPlan> _lessonPlans = new();

    public List<LessonPlan> GetAll()
    {
        return _lessonPlans;
    }

    public LessonPlan? GetById(int id)
    {
        return _lessonPlans.FirstOrDefault(lp => lp.Id == id);
    }

    public void Create(LessonPlan lessonPlan)
    {
        _lessonPlans.Add(lessonPlan);
    }

    public void Update(LessonPlan lessonPlan)
    {
        // Objects are stored by reference, so nothing is needed.
    }

    public void Delete(int id)
    {
        var lessonPlan = GetById(id);

        if (lessonPlan != null)
            _lessonPlans.Remove(lessonPlan);
    }

    public bool Exists(int id)
    {
        return _lessonPlans.Any(lp => lp.Id == id);
    }
}
