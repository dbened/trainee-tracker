using Microsoft.EntityFrameworkCore;
using Softwareproject.Models;
using Softwareproject.Data;

namespace Softwareproject.Data
{
    public class LessonPlanRepository : ILessonPlanRepository
    {
        private readonly Context _context;

        public LessonPlanRepository(Context context)
        {
            _context = context;
        }

        public void Create(LessonPlan lessonPlan)
        {
            _context.LessonPlans.Add(lessonPlan);
            _context.SaveChanges();
        }

        public void Update(LessonPlan lessonPlan)
        {
            _context.LessonPlans.Update(lessonPlan);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = _context.LessonPlans.Find(id);
            if (entity != null)
            {
                _context.LessonPlans.Remove(entity);
                _context.SaveChanges();
            }
        }

        public LessonPlan? GetById(int id)
            => _context.LessonPlans
                .Include(x => x.lessonList)
                .FirstOrDefault(x => x.Id == id);

        public List<LessonPlan> GetAll()
            => _context.LessonPlans
                .Include(x => x.lessonList)
                .ToList();

        public bool Exists(int id)
            => _context.LessonPlans.Any(x => x.Id == id);
    }
}