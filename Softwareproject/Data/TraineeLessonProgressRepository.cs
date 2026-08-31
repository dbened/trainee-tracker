using Microsoft.EntityFrameworkCore;
using Softwareproject.Models;
using System.Collections.Generic;
using System.Linq;

namespace Softwareproject.Data
{
    // Repository für TraineeLessonProgress – kapselt alle DB-Zugriffe auf Fortschrittseinträge.
    // Jeder Eintrag verbindet einen Trainee mit einer Lektion und hält den aktuellen Status.
    public class TraineeLessonProgressRepository : ITraineeLessonProgressRepository
    {
        private readonly Context _context;

        public TraineeLessonProgressRepository(Context context)
        {
            _context = context;
        }

        // Einzelnen Fortschrittseintrag per ID laden.
        // ThenInclude(l => l!.LessonPlan) lädt zusätzlich den LessonPlan der Lektion –
        // notwendig, damit in der Mentor-View plan.Id abgefragt werden kann
        // (z.B. progress.Lesson.LessonPlan?.Id für den "Zurück zum Plan"-Link).
        public TraineeLessonProgress? GetById(int id)
            => _context.TraineeLessonProgresses
                .Include(x => x.Lesson).ThenInclude(l => l!.LessonPlan)
                .Include(x => x.Trainee).ThenInclude(t => t.MentorList)
                .FirstOrDefault(x => x.Id == id);

        // Alle Fortschrittseinträge laden (alle Trainees, alle Lektionen).
        // Der Controller filtert danach selbst nach Trainee oder Plan.
        public List<TraineeLessonProgress> GetAll()
            => _context.TraineeLessonProgresses
                .Include(x => x.Lesson).ThenInclude(l => l!.LessonPlan)
                .Include(x => x.Lesson).ThenInclude(l => l.FeedbackList)
                .Include(x => x.Trainee).ThenInclude(t => t.MentorList)
                .ToList();

        // Neuen Fortschrittseintrag anlegen und sofort speichern
        public void Create(TraineeLessonProgress progress)
        {
            _context.TraineeLessonProgresses.Add(progress);
            _context.SaveChanges();
        }

        // Geänderten Fortschrittseintrag (z.B. neuer Status) in die DB schreiben
        public void Update(TraineeLessonProgress progress)
        {
            _context.TraineeLessonProgresses.Update(progress);
            _context.SaveChanges();
        }

        // Fortschrittseintrag löschen (wird aktuell nicht genutzt, gehört aber zum Interface)
        public void Delete(int id)
        {
            var entity = _context.TraineeLessonProgresses.Find(id);
            if (entity != null)
            {
                _context.TraineeLessonProgresses.Remove(entity);
                _context.SaveChanges();
            }
        }

        public bool Exists(int id)
            => _context.TraineeLessonProgresses.Any(x => x.Id == id);
    }
}
