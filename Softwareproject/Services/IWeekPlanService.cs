using Softwareproject.Models;

namespace Softwareproject.Services;

public interface IWeekPlanService
{
    Task<WeekPlanViewModel> GenerateWeekPlanAsync(Trainee trainee, int offset);
}
// Benedikt Dippner