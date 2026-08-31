using Softwareproject.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Softwareproject.Data
{
    public class TraineeRepository : ITraineeRepository
    {
        private readonly Context _context;

        public TraineeRepository(Context context)
        {
            _context = context;
        }
        public Trainee? GetById(int id)
        {
            return _context.Trainees
                .Include(t => t.MentorList)
                .Include(t => t.LessonPlan)
                .FirstOrDefault(t => t.Id == id);
        }
        public List<Trainee> GetByMentorId(int mentorId)
        {
            return _context.Trainees
                .Where(t => t.MentorList.Any(m => m.Id == mentorId))
                .ToList();
        }

        public Trainee? GetByIdWithLessonPlan(int id)
        {
            return _context.Trainees
                .Include(t => t.LessonPlan)
                .FirstOrDefault(t => t.Id == id);
        }
        public Trainee? GetByIdWithMentors(int id)
         => _context.Trainees
            .Include(t => t.MentorList)
            .FirstOrDefault(t => t.Id == id);
        
        public Trainee? GetByEmailWithMentors(string email)
        {
            return _context.Trainees
                .Include(t => t.MentorList)
                .FirstOrDefault(x => x.EmailAddress.ToLower() == email.ToLower());
        }
        

        public Trainee? GetByEmail(string email)
        {
            return _context.Trainees
                .FirstOrDefault(x => x.EmailAddress.ToLower() == email.ToLower());
        }

        public List<Trainee> GetAll()
       => _context.Trainees
           .Include(t => t.LessonPlan)
           .Include(t => t.MentorList)
           .ToList();
        public void Create(Trainee trainee)
        {
            _context.Trainees.Add(trainee);
            _context.SaveChanges();
        }

        public void Update(Trainee trainee)
        {
            _context.Trainees.Update(trainee);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = _context.Trainees.Find(id);
            if (entity != null)
            {
                _context.Trainees.Remove(entity);
                _context.SaveChanges();
            }
        }

        public bool Exists(int id)
            => _context.Trainees.Any(x => x.Id == id);

        internal object GetById(object value)
        {
            throw new NotImplementedException();
        }
    }
}