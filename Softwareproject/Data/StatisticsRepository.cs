using Microsoft.EntityFrameworkCore;
using Softwareproject.Models;

namespace Softwareproject.Data
{
    public class StatisticRepository : IStatisticRepository
    {
        private readonly Context _context;

        public StatisticRepository(Context context)
        {
            _context = context;
        }

        public List<Statistic> GetAll()
            => _context.Statistics
                .Include(s => s.Trainee)
                .ToList();

        public Statistic? GetLatestForTrainee(int traineeId)
            => _context.Statistics
                .Include(s => s.Trainee)
                .Where(s => s.Trainee != null && s.Trainee.Id == traineeId)
                .OrderByDescending(s => s.Date)
                .FirstOrDefault();

        public List<Statistic> GetHistoryForTrainee(int traineeId)
            => _context.Statistics
                .Include(s => s.Trainee)
                .Where(s => s.Trainee != null && s.Trainee.Id == traineeId)
                .OrderBy(s => s.Date)
                .ToList();

        public void Create(Statistic statistic)
        {
            _context.Statistics.Add(statistic);
            _context.SaveChanges();
        }

        public void Update(Statistic statistic)
        {
            _context.Statistics.Update(statistic);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var statistic = _context.Statistics.Find(id);

            if (statistic == null)
                return;

            _context.Statistics.Remove(statistic);
            _context.SaveChanges();
        }
    }
}