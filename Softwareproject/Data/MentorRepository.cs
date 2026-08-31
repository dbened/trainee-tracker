using Softwareproject.Models;
using System.Collections.Generic;
using System.Linq;

namespace Softwareproject.Data
{
    public class MentorRepository : IMentorRepository
    {
        private readonly Context _context;

        public MentorRepository(Context context)
        {
            _context = context;
        }

        public Mentor? GetById(int id)
            => _context.Mentors.FirstOrDefault(x => x.Id == id);

        public Mentor? GetByEmail(string email)
            => _context.Mentors.FirstOrDefault(x => x.EmailAddress.ToLower() == email.ToLower());

        public List<Mentor> GetAll()
            => _context.Mentors.ToList();

        public void Create(Mentor mentor)
        {
            _context.Mentors.Add(mentor);
            _context.SaveChanges();
        }

        public void Update(Mentor mentor)
        {
            _context.Mentors.Update(mentor);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = _context.Mentors.Find(id);
            if (entity != null)
            {
                _context.Mentors.Remove(entity);
                _context.SaveChanges();
            }
        }

        public bool Exists(int id)
            => _context.Mentors.Any(x => x.Id == id);
    }
}