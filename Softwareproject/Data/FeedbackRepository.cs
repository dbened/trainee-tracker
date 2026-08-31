using Softwareproject.Models;
using System.Collections.Generic;
using System.Linq;

namespace Softwareproject.Data
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly Context _context;

        public FeedbackRepository(Context context)
        {
            _context = context;
        }

        public Feedback? GetById(int id)
            => _context.Feedbacks.FirstOrDefault(x => x.Id == id);

        public List<Feedback> GetAll()
            => _context.Feedbacks.ToList();

        public void Create(Feedback feedback)
        {
            _context.Feedbacks.Add(feedback);
            _context.SaveChanges();
        }

        public void Update(Feedback feedback)
        {
            _context.Feedbacks.Update(feedback);
            _context.SaveChanges();
        }
        public List<Feedback> GetByTrainee(Trainee trainee)
        => _context.Feedbacks.Where(x => x.Trainee == trainee).ToList();
        public void Delete(int id)
        {
            var entity = _context.Feedbacks.Find(id);
            if (entity != null)
            {
                _context.Feedbacks.Remove(entity);
                _context.SaveChanges();
            }
        }

        public bool Exists(int id)
            => _context.Feedbacks.Any(x => x.Id == id);
    }
}