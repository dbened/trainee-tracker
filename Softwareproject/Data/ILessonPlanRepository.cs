using Softwareproject.Models;
using System.Collections.Generic;

namespace Softwareproject.Data
{
    public interface ILessonPlanRepository
    {
        LessonPlan? GetById(int id);
        List<LessonPlan> GetAll();

        void Create(LessonPlan lessonPlan);
        void Update(LessonPlan lessonPlan);
        void Delete(int id);

        bool Exists(int id);
    }
}