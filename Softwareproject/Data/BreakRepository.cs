using Microsoft.EntityFrameworkCore;
using Softwareproject.Models;
using System.Collections.Generic;
using System.Linq;

namespace Softwareproject.Data
{
    public class BreakRepository : IBreakRepository
    {
        private readonly Context _context;

        public BreakRepository(Context context)
        {
            _context = context;
        }

        // Lädt den Break inkl. zugehörigem Trainee und dessen MentorList.
        // Notwendig, damit der RequestController prüfen kann, ob der eingeloggte
        // Mentor diesem Trainee zugeordnet ist (Zugriffsschutz für Accept/Deny).
        public Break? GetById(int id)
            => _context.Breaks
                .Include(x => x.Trainee)
                    .ThenInclude(t => t.MentorList)
                .FirstOrDefault(x => x.Id == id);

        public List<Break> GetAll()
            => _context.Breaks
                .Include(x => x.Trainee)
                    .ThenInclude(t => t.MentorList)
                .ToList();

        public void Create(Break breakItem)
        {
            _context.Breaks.Add(breakItem);
            _context.SaveChanges();
        }

        public void Update(Break breakItem)
        {
            _context.Breaks.Update(breakItem);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = _context.Breaks.Find(id);
            if (entity != null)
            {
                _context.Breaks.Remove(entity);
                _context.SaveChanges();
            }
        }

        public bool Exists(int id)
            => _context.Breaks.Any(x => x.Id == id);
    }
}