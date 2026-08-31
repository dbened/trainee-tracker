using Softwareproject.Data;
using Softwareproject.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Softwareproject.Services
{
    
    public class StatisticsRefreshService : IStatisticsRefreshService
    {
        private readonly IStatisticRepository _statisticRepository;
        private readonly ITraineeRepository _traineeRepository;
        private readonly IMentorRepository _mentorRepository;
        private readonly ITraineeLessonProgressRepository _progressRepository;

        // Service responsible for calculating and updating trainee statistics.
        // It retrieves attendance data, evaluates lesson progress,
        // calculates performance metrics, and stores the results.
        public StatisticsRefreshService(
            IStatisticRepository statisticRepository,
            ITraineeRepository traineeRepository,
            IMentorRepository mentorRepository,
            ITraineeLessonProgressRepository progressRepository,
            HttpClient? client = null)
        {
            _statisticRepository = statisticRepository;
            _traineeRepository = traineeRepository;
            _mentorRepository = mentorRepository;
            _progressRepository = progressRepository;
        }

        
        // Recalculates all statistics for a single trainee and
        // stores the updated values in the database.
        public async Task RefreshAsync(int traineeId)
        {
            var trainee = _traineeRepository.GetByIdWithLessonPlan(traineeId);

            if (trainee == null)
                return;

            // Die Portfolio-Version verwendet bewusst lokale Demo-Arbeitszeiten.
            // Dadurch benötigt die Anwendung keinen externen Firmenserver und keine
            // im Quellcode hinterlegten Zugangsdaten
            double presentDays = CountWorkingDays(trainee.StartDate.ToDateTime(TimeOnly.MinValue), DateTime.Today);
            bool workingHoursAvailable = true;


            // Get previous statistic (if one exists)
            var statistic = _statisticRepository.GetLatestForTrainee(trainee.Id);

            if (statistic == null)
            {
                statistic = new Statistic
                {
                    Trainee = trainee,
                    Date = DateTime.Today,

                    CompletedDays = 0,
                    RemainingDays = 0,
                    PresentDays = 0,
                    BufferDays = 0,
                    Speed = 0,
                    ForecastBufferDays = 0,

                    WorkingHoursAvailable = false,
                    LastUpdated = DateTime.Now
                };

                _statisticRepository.Create(statistic);
            }


            if (trainee.LessonPlan == null)
            {
                statistic.Date = DateTime.Today;
                statistic.CompletedDays = 0;
                statistic.RemainingDays = 0;
                statistic.PresentDays = presentDays;
                statistic.BufferDays = 0;
                statistic.Speed = 0;
                statistic.ForecastBufferDays = 0;
                statistic.LastUpdated = DateTime.Now;
                statistic.WorkingHoursAvailable = workingHoursAvailable;

                _statisticRepository.Update(statistic);
                return;
            }

            var progresses = _progressRepository.GetAll()
    .Where(p => p.Trainee != null && p.Trainee.Id == trainee.Id);

            double completedDays = 0.0;

            foreach (var progress in progresses)
            {
                if (progress.Lesson == null)
                    continue;

                double effort = progress.Lesson.EstimatedDays ?? 0;

                switch (progress.Status)
                {
                    case Status.OPEN:
                    case Status.STARTED:
                        break;

                    case Status.FINISHED:
                        completedDays += effort * 0.7;
                        break;

                    case Status.ACCEPTED:
                    case Status.RATED:
                        completedDays += effort;
                        break;

                    case Status.REJECTED:
                        completedDays += effort * 0.8;
                        break;

                    case Status.SKIPPED:
                        break;
                }
            }


            double totalDays = 0.0;

            foreach (var progress in progresses)
            {
                if (progress.Lesson == null)
                    continue;

                // Skipped lessons do not count towards the total.
                if (progress.Status == Status.SKIPPED)
                    continue;

                // Inactive lessons that are still open do not count.
                if (progress.Status == Status.OPEN &&
                    progress.Lesson.IsDeprecated)
                    continue;

                totalDays += progress.Lesson.EstimatedDays ?? 0;
            }


            double remainingDays = totalDays - completedDays;

            if (remainingDays < 0)
                remainingDays = 0;
            double bufferDays = 0;
            double speed = 0;
            double forecastBufferDays = 0;



            bufferDays = completedDays - presentDays;


            speed = presentDays == 0
                ? 0
                : completedDays / presentDays * 100;


            if (completedDays > 0 && presentDays > 0)
            {
                double currentSpeed = completedDays / presentDays;


                double expectedAttendance =
                    remainingDays / currentSpeed;


                forecastBufferDays =
                    remainingDays - expectedAttendance;
            }


            statistic.Date = DateTime.Today;


            // Values that can always be calculated
            statistic.CompletedDays = completedDays;
            statistic.RemainingDays = remainingDays;




            statistic.PresentDays = presentDays;
            statistic.BufferDays = bufferDays;
            statistic.Speed = speed;
            statistic.ForecastBufferDays = forecastBufferDays;


            statistic.LastUpdated = DateTime.Now;
            statistic.WorkingHoursAvailable = workingHoursAvailable;


            _statisticRepository.Update(statistic);
        }

        private static int CountWorkingDays(DateTime start, DateTime end)
        {
            if (start.Date > end.Date)
                return 0;

            var count = 0;
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                    count++;
            }

            return count;
        }

        public async Task RefreshAllAsync()
        {
            var trainees = _traineeRepository.GetAll();


            foreach (var trainee in trainees)
            {
                await RefreshAsync(trainee.Id);
            }
        }

    }
}
