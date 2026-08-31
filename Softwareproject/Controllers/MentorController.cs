using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Softwareproject.Data;
using Softwareproject.Models;
using System.Security.Claims;

namespace Softwareproject.Controllers
{
    // Bevor irgendjemand diesen Controller benutzen darf, prüft [Authorize], ob der aktuell angemeldete
    // Benutzer die Rolle "Mentor" hat.
    [Authorize(Roles = "Mentor")]
    public class MentorController : Controller
    {
        private readonly IMentorRepository _mentorRepository;
        private readonly ITraineeRepository _traineeRepository;

        public MentorController(
            IMentorRepository mentorRepository,
            ITraineeRepository traineeRepository)
        {
            _mentorRepository = mentorRepository;
            _traineeRepository = traineeRepository;
        }

    
        [HttpGet]
        public IActionResult Trainees()
        {
            var mentor = GetCurrentMentor();

            if (mentor == null)
                return Unauthorized();

            var trainees = _traineeRepository.GetByMentorId(mentor.Id);

            return View(trainees);
        }
    
        // Hilfsmethode: liest den aktuell angemeldeten Mentor aus dem Login-Cookie.
        private Mentor? GetCurrentMentor()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return _mentorRepository.GetByEmail(email);
        }
    }
}

