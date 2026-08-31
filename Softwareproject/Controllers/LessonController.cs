using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Softwareproject.Data;
using Softwareproject.Models;
using Microsoft.AspNetCore.Authorization;

namespace Softwareproject.Controllers
{
    // Verwaltet alle Statusübergänge, die ein Trainee selbst auslösen kann.
    // Skip/Unskip und Akzeptieren/Ablehnen sind reine Mentor-Aktionen und liegen
    // im LessonPlanController bzw. RequestController.
    //
    // Statusmaschine (Trainee-Seite):
    //   OPEN ──► STARTED ──► FINISHED  (Normalfall: starten → abgeben)
    //   STARTED ──► OPEN               (Stoppen, falls noch nicht fertig)
    //   REJECTED ──► FINISHED          (Überarbeitung erneut abgeben)
    //   OPEN ──► SKIPPED ──► OPEN      (Lektion überspringen / reaktivieren)
    [Authorize]
    public class LessonController : Controller
    {
        // Zugriff auf TraineeLessonProgress – Status, Trainee, Lektion
        private readonly ITraineeLessonProgressRepository _progressRepository;

        public LessonController(ITraineeLessonProgressRepository progressRepository)
        {
            _progressRepository = progressRepository;
        }

        // POST /Lesson/StartLesson – Trainee beginnt eine Lektion
        // Übergang: OPEN → STARTED (nur wenn aktuell OPEN, sonst keine Änderung)
        [HttpPost]
        public IActionResult StartLesson(int progressId)
        {
            var progress = GetOwnProgressOrNull(progressId);
            if (progress != null && progress.Status == Status.OPEN)
            {
                progress.Status = Status.STARTED;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return RedirectToAction("LehrplanUebersicht", "LessonPlan");
        }

        // POST /Lesson/StopLesson – Trainee pausiert eine gestartete Lektion
        // Übergang: STARTED → OPEN
        [HttpPost]
        public IActionResult StopLesson(int progressId)
        {
            var progress = GetOwnProgressOrNull(progressId);
            if (progress != null && progress.Status == Status.STARTED)
            {
                progress.Status = Status.OPEN;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return RedirectToAction("LehrplanUebersicht", "LessonPlan");
        }

        // POST /Lesson/SubmitLesson – Trainee gibt eine Lektion zur Bewertung ab
        // Übergang: STARTED → FINISHED
        // Der Mentor sieht die Lektion danach in seiner Übersicht und kann akzeptieren/ablehnen
        [HttpPost]
        public IActionResult SubmitLesson(int progressId)
        {
            var progress = GetOwnProgressOrNull(progressId);
            if (progress != null && progress.Status == Status.STARTED)
            {
                progress.Status = Status.FINISHED;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return RedirectToAction("LehrplanUebersicht", "LessonPlan");
        }

        // POST /Lesson/ResubmitLesson – Trainee gibt eine abgelehnte Lektion erneut ab
        // Übergang: REJECTED → FINISHED
        // Ermöglicht es den Ablehnungsgrund zu berücksichtigen und die Lektion nochmals einzureichen
        [HttpPost]
        public IActionResult ResubmitLesson(int progressId)
        {
            var progress = GetOwnProgressOrNull(progressId);
            if (progress != null && progress.Status == Status.REJECTED)
            {
                progress.Status = Status.FINISHED;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return RedirectToAction("LehrplanUebersicht", "LessonPlan");
        }

        // POST /Lesson/SkipLesson – Trainee überspringt eine Lektion (z.B. bereits bekanntes Thema)
        // Übergang: OPEN → SKIPPED
        [HttpPost]
        public IActionResult SkipLesson(int progressId)
        {
            var progress = GetOwnProgressOrNull(progressId);
            if (progress != null && progress.Status == Status.OPEN)
            {
                progress.Status = Status.SKIPPED;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return RedirectToAction("LehrplanUebersicht", "LessonPlan");
        }

        // POST /Lesson/UnskipLesson – Trainee reaktiviert eine übersprungene Lektion
        // Übergang: SKIPPED → OPEN
        [HttpPost]
        public IActionResult UnskipLesson(int progressId)
        {
            var progress = GetOwnProgressOrNull(progressId);
            if (progress != null && progress.Status == Status.SKIPPED)
            {
                progress.Status = Status.OPEN;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return RedirectToAction("LehrplanUebersicht", "LessonPlan");
        }

        [HttpPost]
        public IActionResult UnfinishLesson(int progressId)
        {
            var progress = GetOwnProgressOrNull(progressId);
            if (progress != null && progress.Status == Status.FINISHED)
            {
                progress.Status = Status.STARTED;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return RedirectToAction("LehrplanUebersicht", "LessonPlan");
        }

        // Lädt den Fortschrittseintrag nur, wenn er dem eingeloggten Trainee gehört.
        // Verhindert, dass über eine fremde progressId der Status eines anderen Trainees
        // verändert wird ("Trainees können nur ihre eigenen Bearbeitungszustände verändern").
        private TraineeLessonProgress? GetOwnProgressOrNull(int progressId)
        {
            var progress = _progressRepository.GetById(progressId);
            var email = User.FindFirstValue(ClaimTypes.Email);
            return progress != null && progress.Trainee.EmailAddress == email ? progress : null;
        }
    }
}