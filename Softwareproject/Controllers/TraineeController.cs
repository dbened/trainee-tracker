using Microsoft.AspNetCore.Mvc;
using Softwareproject.Data;
using Softwareproject.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Softwareproject.Controllers
{
    // Diese Controller-Klasse bündelt die Trainee-Startseite und die eigenen Übersichten.
    // Sie verwendet die vorhandenen Repositories und zeigt nur Daten des angemeldeten Trainees an.
    [Authorize]
    public class TraineeController : Controller
    {
        private readonly ITraineeRepository _traineeRepository;
        private readonly ITraineeLessonProgressRepository _traineeLessonProgressRepository;

        public TraineeController(
            ITraineeRepository traineeRepository,
            ITraineeLessonProgressRepository traineeLessonProgressRepository)
        {
            _traineeRepository = traineeRepository;
            _traineeLessonProgressRepository = traineeLessonProgressRepository;
        }
        
        // Hilfsmethode, die den aktuell angemeldeten Trainee über die E-Mail ermittelt.
        // Zusätzlich werden die Mentoren des Trainees geladen.
        private Trainee? GetCurrentTraineeWithMentors()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return _traineeRepository.GetByEmailWithMentors(email);
        }
    
        // Diese Action zeigt die Mentoren des aktuell angemeldeten Trainees an.
        [HttpGet]
        public IActionResult Mentoren()
        {
            var trainee = GetCurrentTraineeWithMentors();

            if (trainee == null)
            {
                return NotFound();
            }

            return View(trainee.MentorList.ToList());
        }

    }
}