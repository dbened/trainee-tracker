using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Softwareproject.Data;
using Softwareproject.Models;
using Softwareproject.Services;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;


namespace Softwareproject.Controllers
{
    // Controller responsible for displaying trainee statistics
    [Authorize]
    public class StatisticsController : Controller
    {
        private readonly IStatisticRepository _statisticRepository;
        private readonly ITraineeRepository _traineeRepository;
        private readonly IMentorRepository _mentorRepository;
        private readonly ITraineeLessonProgressRepository _progressRepository;
        private readonly IStatisticsRefreshService _statisticsRefreshService;

        public StatisticsController(
            IStatisticRepository statisticRepository,
            ITraineeRepository traineeRepository,
            IMentorRepository mentorRepository,
            ITraineeLessonProgressRepository progressRepository,
            IStatisticsRefreshService statisticsRefreshService)
        {
            _statisticRepository = statisticRepository;
            _traineeRepository = traineeRepository;
            _mentorRepository = mentorRepository;
            _progressRepository = progressRepository;
            _statisticsRefreshService = statisticsRefreshService;
        }


        // Displays the statistics of the currently logged-in trainee.
        // Only users with the Trainee role may access this action.
        [HttpGet]
        [Authorize(Roles = "Trainee")]
        public IActionResult TraineeStatistics()
        {
            // Read the email claim from the authentication cookie.
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            // Retrieve the trainee associated with the email address.
            var trainee = _traineeRepository.GetByEmail(email);

            if (trainee == null)
                return NotFound();
            // Retrieve the trainee's most recent statistics. Store the trainee object so the view can display additional information.
            var statistic = _statisticRepository.GetLatestForTrainee(trainee.Id);
            ViewBag.Trainee = trainee;
            return View("Statistics", statistic);
        }


        // Displays the statistics of a trainee assigned to the logged-in mentor.
        // Mentors may only access statistics for their own trainees.
        [HttpGet]
        [Authorize(Roles = "Mentor")]
        public IActionResult MentorStatistics(int traineeId)
        {
            // Read the logged-in mentor's email from the authentication claims.
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var mentor = _mentorRepository.GetByEmail(email);

            if (mentor == null)
                return NotFound();
            // Load the trainee together with their assigned mentors.
            var trainee = _traineeRepository.GetByIdWithMentors(traineeId);

            if (trainee == null)
                return NotFound();
            // Check whether this mentor is assigned to the trainee.
            // If not, access is denied.
            if (trainee.MentorList == null ||
                !trainee.MentorList.Any(m => m.Id == mentor.Id))
            {
                return Forbid();
            }
            // Retrieve the trainee's latest statistics. Pass the trainee object to the view.
            var statistic = _statisticRepository.GetLatestForTrainee(traineeId);
            ViewBag.Trainee = trainee;

            return View("Statistics", statistic);
        }

        // Manually recalculates and updates the statistics for a trainee.
        // Only mentors and administrators may perform this action.
        [HttpPost]
        [Authorize(Roles = "Mentor,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refresh(int traineeId)
        {
            // Call the refresh service, which recalculates and stores the newest statistics for the selected trainee.
            await _statisticsRefreshService.RefreshAsync(traineeId);
            // Redirect back to the statistics page so the updated values are displayed.
            return RedirectToAction(nameof(MentorStatistics), new { traineeId });
        }
    }
}
