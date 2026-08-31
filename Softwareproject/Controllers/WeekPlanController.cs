using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Softwareproject.Models;
using Softwareproject.Data;
using Softwareproject.Services;

namespace Softwareproject.Controllers
{
    [Authorize]
    public class WeekPlanController : Controller
    {
        // Repositories
        private readonly IBreakRepository _breakRepository;
        private readonly ITraineeRepository _traineeRepository;
        private readonly IWeekPlanService _weekPlanService;

        public WeekPlanController(
            IBreakRepository breakRepository,
            ITraineeRepository traineeRepository,
            IWeekPlanService weekPlanService)
        {
            _breakRepository = breakRepository;
            _traineeRepository = traineeRepository;
            _weekPlanService = weekPlanService;

        }

        // ==========================================================
        // FEATURE 1: DER WOCHENPLAN
        // Im DCD als "showWeekplanView(): void" bezeichnet
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> Index(int offset = 0)
        {
            var currentTrainee = GetCurrentTrainee();
            // Sicherheitsnetz: Falls der Trainee in der DB gelöscht wurde
            if (currentTrainee == null) return RedirectToAction("Login", "Login");

            var vm = await _weekPlanService.GenerateWeekPlanAsync(currentTrainee, offset);

            ViewBag.ApiError = vm.IsApiError; // Damit die View weiß, ob die API offline ist

            return View(vm);
        }

        [Authorize(Roles = "Mentor,Admin")]
        public async Task<IActionResult> TraineeWeekPlan(int traineeId, int offset = 0)
        {
            var trainee = _traineeRepository.GetByIdWithLessonPlan(traineeId);

            if (trainee == null)
            {
                return NotFound();
            }

            var vm = await _weekPlanService.GenerateWeekPlanAsync(trainee, offset);

            ViewBag.Trainee = trainee;
            ViewBag.ApiError = vm.IsApiError;

            return View(vm);
        }

        // ==========================================================
        // FEATURE 2: PAUSE BEANTRAGEN (Formular anzeigen)
        // Im DCD als "requestBreak(...): void" bezeichnet
        // ==========================================================
        [HttpGet]
        public IActionResult RequestBreak()
        {
            // Sicherheitsnetz: Falls der Trainee in der DB gelöscht wurde
            if (GetCurrentTrainee() == null) return RedirectToAction("Login", "Login");

            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult CreateBreakForTrainee(int traineeId)
        {
            // Sicherheitsnetz: Falls der Trainee in der DB gelöscht wurde
            var trainee = _traineeRepository.GetById(traineeId);
            if (trainee == null) return NotFound();

            ViewBag.Trainee = trainee;

            return View();
        }

        // ==========================================================
        // FEATURE 3: PAUSE SPEICHERN
        // Im DCD als "createBreak(...): void" bezeichnet
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateBreak(DateOnly startDate, DateOnly endDate, string description)
        {
            var currentTrainee = GetCurrentTrainee();
            if (currentTrainee == null) return RedirectToAction("Login", "Login");

            // Pflichtfeld-Checks
            if (startDate == default) ModelState.AddModelError("startDate", "Bitte wähle ein Startdatum.");
            if (endDate == default) ModelState.AddModelError("endDate", "Bitte wähle ein Enddatum.");
            if (string.IsNullOrWhiteSpace(description)) ModelState.AddModelError("description", "Bitte gib eine Begründung ein.");

            // Logik-Checks
            if (startDate != default && endDate != default && startDate > endDate)
            {
                ModelState.AddModelError("endDate", "Das Enddatum darf nicht vor dem Startdatum liegen.");
            }
            if (startDate != default && startDate < DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError("startDate", "Das Startdatum darf nicht in der Vergangenheit liegen.");
            }
            if (endDate != default && endDate > currentTrainee.EndDate)
            {
                ModelState.AddModelError("endDate", "Das Enddatum darf nicht nach dem Ende deiner Ausbildung liegen.");
            }

            // Speicherung nach den Checks
            if (ModelState.IsValid)
            {
                var newBreak = new Break
                {
                    StartingDate = startDate,
                    EndDate = endDate,
                    Description = description,
                    Accepted = false,

                    // Hier übergeben wir das komplette Trainee-Objekt!
                    // Entity Framework Core regelt die Verknüpfung in der Datenbank jetzt vollautomatisch.
                    Trainee = currentTrainee
                };

                _breakRepository.Create(newBreak);
                return RedirectToAction(nameof(Index));
            }

            // Bei Fehlern im Formular zeigen wir es erneut an
            return View("RequestBreak");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult CreateBreakForTrainee(int traineeId, DateOnly startDate, DateOnly endDate, string description)
        {
            var trainee = _traineeRepository.GetById(traineeId);
            if (trainee == null) return NotFound();

            // Pflichtfeld-Checks
            if (startDate == default) ModelState.AddModelError("startDate", "Bitte wähle ein Startdatum.");
            if (endDate == default) ModelState.AddModelError("endDate", "Bitte wähle ein Enddatum.");
            if (string.IsNullOrWhiteSpace(description)) ModelState.AddModelError("description", "Bitte gib eine Begründung ein.");

            // Logik-Checks
            if (startDate != default && endDate != default && startDate > endDate)
            {
                ModelState.AddModelError("endDate", "Das Enddatum darf nicht vor dem Startdatum liegen.");
            }
            if (startDate != default && startDate < DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError("startDate", "Das Startdatum darf nicht in der Vergangenheit liegen.");
            }
            if (endDate != default && endDate > trainee.EndDate)
            {
                ModelState.AddModelError("endDate", "Das Enddatum darf nicht nach dem Ende deiner Ausbildung liegen.");
            }

            // Speicherung nach den Checks
            if (ModelState.IsValid)
            {
                var newBreak = new Break
                {
                    StartingDate = startDate,
                    EndDate = endDate,
                    Description = description,
                    Accepted = true, // Direkt genehmigt, da Mentor/Admin

                    // Hier übergeben wir das komplette Trainee-Objekt!
                    // Entity Framework Core regelt die Verknüpfung in der Datenbank jetzt vollautomatisch.
                    Trainee = trainee
                };

                _breakRepository.Create(newBreak);
                return RedirectToAction(nameof(TraineeWeekPlan), new { traineeId = trainee.Id });
            }

            // Bei Fehlern im Formular zeigen wir es erneut an
            ViewBag.Trainee = trainee;
            return View("CreateBreakForTrainee");
        }

        // Hilfsmethoden

        private Trainee? GetCurrentTrainee()
        {
            // 1. E-Mail aus dem Login-Cookie lesen
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userEmail)) return null;

            // 2. Trainee anhand der E-Mail in der Datenbank suchen und zurückgeben
            return _traineeRepository.GetByEmail(userEmail);
        }
    }
}
// Benedikt Dippner