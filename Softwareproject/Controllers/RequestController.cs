using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Softwareproject.Data;
using Softwareproject.Models;


namespace Softwareproject.Controllers
{
    // Nur Mentoren dürfen Requests annehmen oder ablehnen.
    [Authorize(Roles = "Mentor")]
    public class RequestController : Controller
    {
        private readonly IBreakRepository _breakRepository;
        private readonly IMentorRepository _mentorRepository;
        private readonly ITraineeLessonProgressRepository _traineeLessonProgressRepository;
        private readonly ITraineeRepository _traineeRepository;

        public RequestController(IBreakRepository breakRepository, IMentorRepository mentorRepository, ITraineeLessonProgressRepository traineeLessonProgressRepository, ITraineeRepository traineeRepository)
        {
            _breakRepository = breakRepository;
            _mentorRepository = mentorRepository;
            _traineeLessonProgressRepository = traineeLessonProgressRepository;
            _traineeRepository = traineeRepository;
        }


        // ÜBERSICHT: OFFENE ANTRÄGE
        // Zeigt dem eingeloggten Mentor alle offenen Anträge seiner eigenen Trainees:
        //   - noch nicht bestätigte Breakrequests (Accepted == false)
        //   - abgegebene Lessons (Status == FINISHED), die auf seine Bewertung warten
        [HttpGet]
        public IActionResult Index()
        {
            var mentor = GetCurrentMentor();
            if (mentor == null) return RedirectToAction("Login", "Login");

            var openBreaks = _breakRepository.GetAll()
                .Where(b => !b.Accepted
                            && (b.Trainee?.MentorList.Any(m => m.Id == mentor.Id) ?? false))
                .ToList();

            // Abgegebene Lessons der eigenen Trainees – das ist der Lesson-Request
            // (Trainee bittet den Mentor, die Lesson von FINISHED auf ACCEPTED zu setzen).
            var openLessons = _traineeLessonProgressRepository.GetAll()
                .Where(p => p.Status == Status.FINISHED
                            && (p.Trainee?.MentorList.Any(m => m.Id == mentor.Id) ?? false))
                .ToList();

            var viewModel = new RequestInboxViewModel
            {
                OpenBreaks = openBreaks,
                OpenLessons = openLessons
            };

            return View(viewModel);
        }


        // ÜBERSICHT: OFFENE ANTRÄGE EINES EINZELNEN TRAINEES
        // Wird aus der Trainee-Detailansicht des Mentors (Navbar "Anträge") aufgerufen
        // und zeigt ausschließlich die offenen Anträge des dort ausgewählten Trainees.
        [HttpGet]
        public IActionResult TraineeRequests(int traineeId)
        {
            var mentor = GetCurrentMentor();
            if (mentor == null) return RedirectToAction("Login", "Login");

            var trainee = _traineeRepository.GetByIdWithMentors(traineeId);
            if (trainee == null) return NotFound();

            // Mentor darf nur die Anträge seiner eigenen Trainees sehen.
            var belongsToMentor = trainee.MentorList?.Any(m => m.Id == mentor.Id) ?? false;
            if (!belongsToMentor) return Forbid();

            var openBreaks = _breakRepository.GetAll()
                .Where(b => !b.Accepted && b.Trainee != null && b.Trainee.Id == traineeId)
                .ToList();

            var openLessons = _traineeLessonProgressRepository.GetAll()
                .Where(p => p.Status == Status.FINISHED && p.Trainee != null && p.Trainee.Id == traineeId)
                .ToList();

            var viewModel = new RequestInboxViewModel
            {
                OpenBreaks = openBreaks,
                OpenLessons = openLessons
            };

            ViewBag.Trainee = trainee;

            return View(viewModel);
        }


        // BREAKREQUEST ANNEHMEN
        // Trainees senden Breakrequests an ihre Mentoren; der Mentor
        // bestätigt hier den Zeitraum (Accepted = true).
        [HttpPost]
        public IActionResult AcceptBreak(int id, int? traineeId)
        {
            var breakItem = GetOwnBreakOrNull(id, out var error);
            if (error != null) return error;

            breakItem!.Accepted = true;
            _breakRepository.Update(breakItem);

            return RedirectToInbox(traineeId);
        }


        // BREAKREQUEST ABLEHNEN
        // Bei Ablehnung wird der Request entfernt (kein eigener Status
        // am Break-Modell hinterlegt).
        [HttpPost]
        public IActionResult DenyBreak(int id, int? traineeId)
        {
            var breakItem = GetOwnBreakOrNull(id, out var error);
            if (error != null) return error;

            _breakRepository.Delete(breakItem!.Id);

            return RedirectToInbox(traineeId);
        }

        // LESSON-REQUEST ANNEHMEN
        // Der Trainee hat die Lesson abgegeben (FINISHED); der Mentor akzeptiert sie hier.
        // Zustandsübergang: FINISHED → ACCEPTED
        [HttpPost]
        public IActionResult AcceptLesson(int progressId, int? traineeId)
        {
            var progress = GetOwnLessonProgressOrNull(progressId, out var error);
            if (error != null) return error;

            // Nur abgegebene Lessons können akzeptiert werden.
            if (progress!.Status == Status.FINISHED)
            {
                progress.Status = Status.ACCEPTED;
                progress.StatusChangedAt = DateTime.Now;
                _traineeLessonProgressRepository.Update(progress);
            }

            return RedirectToInbox(traineeId);
        }

        // LESSON-REQUEST ABLEHNEN
        // Bei Ablehnung wird ein Ablehnungsgrund hinterlegt, den der Trainee sieht;
        // er kann die Lesson überarbeiten und erneut abgeben (REJECTED → FINISHED).
        // Zustandsübergang: FINISHED → REJECTED
        [HttpPost]
        public IActionResult DenyLesson(int progressId, string refusalReason, int? traineeId)
        {
            var progress = GetOwnLessonProgressOrNull(progressId, out var error);
            if (error != null) return error;

            if (progress!.Status == Status.FINISHED)
            {
                progress.Status = Status.REJECTED;
                progress.RefusalReason = refusalReason;
                progress.StatusChangedAt = DateTime.Now;
                _traineeLessonProgressRepository.Update(progress);
            }

            return RedirectToInbox(traineeId);
        }


        // Springt nach Annehmen/Ablehnen dahin zurück, wo der Mentor herkam:
        // aus der trainee-spezifischen Ansicht wieder dorthin, sonst zur allgemeinen Übersicht.
        private IActionResult RedirectToInbox(int? traineeId)
        {
            if (traineeId.HasValue)
                return RedirectToAction(nameof(TraineeRequests), new { traineeId = traineeId.Value });

            return RedirectToAction(nameof(Index));
        }


        // HILFSMETHODE: BREAK LADEN UND ZUGRIFF PRÜFEN
        // Stellt sicher, dass der Break existiert und der eingeloggte
        // Mentor dem zugehörigen Trainee zugeordnet ist. Andernfalls
        // wird über den out-Parameter ein passendes Fehlerergebnis gesetzt.
        private Break? GetOwnBreakOrNull(int id, out IActionResult? error)
        {
            error = null;

            var mentor = GetCurrentMentor();
            if (mentor == null)
            {
                error = Unauthorized();
                return null;
            }

            var breakItem = _breakRepository.GetById(id);
            if (breakItem == null)
            {
                error = NotFound();
                return null;
            }

            // Mentor darf nur Requests seiner eigenen Trainees bearbeiten.
            var belongsToMentor = breakItem.Trainee?.MentorList
                .Any(m => m.Id == mentor.Id) ?? false;
            if (!belongsToMentor)
            {
                error = Forbid();
                return null;
            }

            return breakItem;
        }

        // HILFSMETHODE: LESSON-FORTSCHRITT LADEN UND ZUGRIFF PRÜFEN
        // Stellt sicher, dass der Fortschrittseintrag existiert und der eingeloggte
        // Mentor dem zugehörigen Trainee zugeordnet ist. Andernfalls wird über den
        // out-Parameter ein passendes Fehlerergebnis gesetzt.
        private TraineeLessonProgress? GetOwnLessonProgressOrNull(int id, out IActionResult? error)
        {
            error = null;

            var mentor = GetCurrentMentor();
            if (mentor == null)
            {
                error = Unauthorized();
                return null;
            }

            var progress = _traineeLessonProgressRepository.GetById(id);
            if (progress == null)
            {
                error = NotFound();
                return null;
            }

            // Mentor darf nur Lessons seiner eigenen Trainees bearbeiten.
            var belongsToMentor = progress.Trainee?.MentorList
                .Any(m => m.Id == mentor.Id) ?? false;
            if (!belongsToMentor)
            {
                error = Forbid();
                return null;
            }

            return progress;
        }

        // Den eingeloggten Mentor anhand der E-Mail aus dem Login-Cookie laden.
        private Mentor? GetCurrentMentor()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return null;
            return _mentorRepository.GetByEmail(email);
        }


    }
}