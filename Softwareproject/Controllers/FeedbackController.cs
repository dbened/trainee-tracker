using Microsoft.AspNetCore.Mvc;
using Softwareproject.Models;
using Softwareproject.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualBasic;

namespace Softwareproject.Controllers
{
    //Implementier Backend logik für Sämtliche FeedbackViews sowie das Speichern von diesen
    [Authorize]
    public class FeedbackController : Controller
    {
        private readonly ITraineeLessonProgressRepository _traineeLessonProgressRepository;
        private readonly ITraineeRepository _traineeRepository;
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly ILessonPlanRepository _lessonPlanRepository;
        public FeedbackController(IFeedbackRepository feedbackRepository, ITraineeRepository traineeRepository, ITraineeLessonProgressRepository traineeLessonProgressRepository, ILessonPlanRepository lessonPlanRepository)
        {
            _feedbackRepository = feedbackRepository;
            _traineeRepository = traineeRepository;
            _traineeLessonProgressRepository = traineeLessonProgressRepository;
            _lessonPlanRepository = lessonPlanRepository;

        }
        
        [HttpGet]
        public IActionResult CreateFeedback()   //Umleitung auf das Feedback Formular
        {
            ViewBag.IsMentorFeedback = false;
            return View("FeedbackGeben", new Feedback());
        }
        
        [HttpPost]
        public IActionResult CreateFeedback(Feedback feedback, int progressId) //Initiale Speicherung von Feedback (auch leere Felder Möglich)
        {

            if (!ModelState.IsValid)
                return View("FeedbackGeben", feedback);
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Unauthorized();
            var progress = _traineeLessonProgressRepository.GetById(progressId);
            if (progress == null)
                return NotFound();
            var trainee = _traineeRepository.GetByEmail(email);
            if (trainee == null)
                return Unauthorized();
            feedback.Trainee = trainee;
            if (!trainee.LessonPlan!.lessonList.Any(x => x.Id == progress.Lesson.Id))
                return Forbid();
            feedback.LessonId = progress.Lesson.Id;
            if (feedback.PriorKnowledge == null)
                feedback.PriorKnowledge = "leer";
            if (feedback.Effort == null)
                feedback.Effort = "leer";
            if (feedback.Comment == null)
                feedback.Comment = "leer";
            feedback.SubmitDate = DateOnly.FromDateTime(DateTime.Now);
            _feedbackRepository.Create(feedback);

            //Ändern des Status von Accepted auf Rated und hinzufügen von feedback zur FeedbackList
            if (progress != null && progress.Status == Status.ACCEPTED)
            {
                progress.Status = Status.RATED;
                progress.StatusChangedAt = DateTime.Now;
                _traineeLessonProgressRepository.Update(progress);
            }
            return RedirectToAction("LehrplanUebersicht", "LessonPlan");
        }

        
        [HttpGet]
        public IActionResult EditFeedback(int id)   //Umleitung auf das Feedback Formular
        {
            var feedback = _feedbackRepository.GetById(id);
            if (feedback == null)
                return NotFound();

            var trainee = _traineeRepository.GetByEmail(User.FindFirstValue(ClaimTypes.Email)!);
            if (trainee == null)
                return Unauthorized();
            if (feedback.Trainee!.Id != trainee.Id)
                return Unauthorized();
            return View("FeedbackGeben", feedback);      //Formular mit bisherigem Feedback übergeben
        }
        
        [HttpPost]
        public IActionResult EditFeedback(Feedback feedback, int progressId)    //Speichern von Feedbackänderungen und Umleitung zurück zur Lehrplanübersicht
        {
            if (!ModelState.IsValid)
                return View("FeedbackGeben", feedback);

            var existingFeedback = _feedbackRepository.GetById(feedback.Id);
            if (existingFeedback == null)
                return NotFound();

            var trainee = _traineeRepository.GetByEmail(User.FindFirstValue(ClaimTypes.Email)!);
            if (trainee == null)
                return Unauthorized();
            if (existingFeedback.Trainee!.Id != trainee.Id)
                return Unauthorized();
            var progress = _traineeLessonProgressRepository.GetById(progressId);
            if (!trainee.LessonPlan!.lessonList.Any(x => x.Id == progress!.Lesson.Id))
                return Forbid();
            if (feedback.PriorKnowledge == null)
                feedback.PriorKnowledge = "leer";
            if (feedback.Effort == null)
                feedback.Effort = "leer";
            if (feedback.Comment == null)
                feedback.Comment = "leer";
            existingFeedback.Difficulty = feedback.Difficulty;
            existingFeedback.PriorKnowledge = feedback.PriorKnowledge;
            existingFeedback.Effort = feedback.Effort;
            existingFeedback.Comment = feedback.Comment;
            existingFeedback.SubmitDate = DateOnly.FromDateTime(DateTime.Now);
            _feedbackRepository.Update(existingFeedback);
            return RedirectToAction("LehrplanUebersicht", "LessonPlan");
        }

        
        [HttpPost]
        public IActionResult DeleteFeedback(int id, int progressId)     //Löschen von Feedback aus dem System
        {
            var feedback = _feedbackRepository.GetById(id);
            if (feedback == null)
                return NotFound();

            var trainee = _traineeRepository.GetByEmail(User.FindFirstValue(ClaimTypes.Email)!);
            if (feedback.Trainee != trainee)
                return Unauthorized();

            _feedbackRepository.Delete(feedback.Id);
            var progress = _traineeLessonProgressRepository.GetById(progressId);
            if (progress != null && progress.Status == Status.RATED)
            {
                progress.Status = Status.ACCEPTED;
                progress.StatusChangedAt = DateTime.Now;
                _traineeLessonProgressRepository.Update(progress);
            }
            return RedirectToAction("LehrplanUebersicht", "LessonPlan");
        }
        
        [Authorize]     //check auf Login
        [HttpGet]
        public IActionResult FeedbackUebersicht(int lessonId, int lessonPlanId)     //Laden von Feedback zur entsprechenden Lektion und Rückgabe mit FeedbackViewModel
        {
            var lesson = _lessonPlanRepository.GetAll().SelectMany(p => p.lessonList).FirstOrDefault(l => l.Id == lessonId);
            if (lesson == null)
                return NotFound();
            var feedbackList = _feedbackRepository.GetAll().Where(x => x.LessonId == lessonId).Select(f => new FeedbackViewModel
            {
                Difficulty = f.Difficulty,
                PriorKnowledge = f.PriorKnowledge!,
                Effort = f.Effort!,
                Comment = f.Comment!,
                Trainee = f.Trainee!,
                Lesson = lesson
            }).ToList();
            return View("FeedbackUebersicht", feedbackList);
        }
        
        public IActionResult neuesFeedbackUebersicht()      //Laden von Feedback der letzten 2 Monate und Rückgabe mit FeedbackViewModel
        {
            var fromDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2));
            var feedbacks = _feedbackRepository
            .GetAll()
            .Where(f => f.SubmitDate >= fromDate)
            .Select(f => new FeedbackViewModel
            {
                SubmitDate = f.SubmitDate,
                Difficulty = f.Difficulty,
                PriorKnowledge = f.PriorKnowledge!,
                Effort = f.Effort!,
                Comment = f.Comment!,
                Trainee = f.Trainee!,
                Lesson = _lessonPlanRepository
                .GetAll()
                .SelectMany(lp => lp.lessonList)
                .FirstOrDefault(l => l.Id == f.LessonId)!
            })
            .ToList();
            return View(feedbacks);
        }




        [HttpGet]
        public IActionResult CreateMentorFeedback(int progressId)   //Umleitung auf das Feedback Formular
        {
            var progress = _traineeLessonProgressRepository.GetById(progressId);
            ViewBag.IsMentorFeedback = true;
            ViewBag.progressId = progressId;
            ViewBag.traineeId = progress.Trainee.Id;

            return View("FeedbackGeben", new Feedback());
        }
        
        [HttpPost]
        public IActionResult CreateMentorFeedback(Feedback feedback, int progressId) //Initiale Speicherung von Feedback (auch leere Felder Möglich)
        {
            if (!ModelState.IsValid)
                return View("FeedbackGeben", feedback);
                

            var progress = _traineeLessonProgressRepository.GetById(progressId);
            if (progress == null)
                return NotFound();

            var trainee = progress.Trainee;;
            if (trainee == null)
                return Unauthorized();
            feedback.Trainee = trainee;
            if (!trainee.LessonPlan!.lessonList.Any(x => x.Id == progress.Lesson.Id))
                return Forbid();
            feedback.LessonId = progress.Lesson.Id;
            if (feedback.PriorKnowledge == null)
                feedback.PriorKnowledge = "leer";
            if (feedback.Effort == null)
                feedback.Effort = "leer";
            if (feedback.Comment == null)
                feedback.Comment = "leer";
            feedback.SubmitDate = DateOnly.FromDateTime(DateTime.Now);
            _feedbackRepository.Create(feedback);

            //Ändern des Status von Accepted auf Rated und hinzufügen von feedback zur FeedbackList
            if (progress != null && progress.Status == Status.ACCEPTED)
            {
                progress.Status = Status.RATED;
                progress.StatusChangedAt = DateTime.Now;
                _traineeLessonProgressRepository.Update(progress);
            }
            return RedirectToAction("TraineeLessonPlan", "LessonPlan", new { traineeId = trainee.Id });
        }

        
        [HttpGet]
        public IActionResult EditMentorFeedback(int id, int progressId)   //Umleitung auf das Feedback Formular
        {
            var feedback = _feedbackRepository.GetById(id);
            if (feedback == null)
                return NotFound();
            var progress = _traineeLessonProgressRepository.GetById(progressId);
            if(progress == null)
                return NotFound();
            if (feedback.Trainee!.Id != progress.Trainee.Id)
                return Unauthorized();
            ViewBag.IsMentorFeedback = true;
            ViewBag.progressId = progressId;
            ViewBag.traineeId = progress.Trainee.Id;
            return View("FeedbackGeben", feedback);      //Formular mit bisherigem Feedback übergeben
        }
        
        [HttpPost]
        public IActionResult EditMentorFeedback(Feedback feedback, int progressId)    //Speichern von Feedbackänderungen und Umleitung zurück zur Lehrplanübersicht
        {
            if (!ModelState.IsValid)
                return View("FeedbackGeben", feedback);

            var existingFeedback = _feedbackRepository.GetById(feedback.Id);
            if (existingFeedback == null)
                return NotFound();

            var progress = _traineeLessonProgressRepository.GetById(progressId);
            if(progress == null)
                return NotFound();
            var trainee = progress.Trainee;
            if(trainee == null)
                return Unauthorized();
            if(existingFeedback.Trainee.Id != trainee.Id)
                return Unauthorized();
            if (!trainee.LessonPlan!.lessonList.Any(x => x.Id == progress!.Lesson.Id))
                return Forbid();
            if (feedback.PriorKnowledge == null)
                feedback.PriorKnowledge = "leer";
            if (feedback.Effort == null)
                feedback.Effort = "leer";
            if (feedback.Comment == null)
                feedback.Comment = "leer";
            existingFeedback.Difficulty = feedback.Difficulty;
            existingFeedback.PriorKnowledge = feedback.PriorKnowledge;
            existingFeedback.Effort = feedback.Effort;
            existingFeedback.Comment = feedback.Comment;
            existingFeedback.SubmitDate = DateOnly.FromDateTime(DateTime.Now);
            _feedbackRepository.Update(existingFeedback);
            return RedirectToAction("TraineeLessonPlan", "LessonPlan", new { traineeId = trainee.Id });
        }

        
        [HttpPost]
        public IActionResult DeleteMentorFeedback(int id, int progressId)     //Löschen von Feedback aus dem System
        {
            var feedback = _feedbackRepository.GetById(id);
            if (feedback == null)
                return NotFound();

            _feedbackRepository.Delete(feedback.Id);
            var progress = _traineeLessonProgressRepository.GetById(progressId);
            if (progress != null && progress.Status == Status.RATED)
            {
                progress.Status = Status.ACCEPTED;
                progress.StatusChangedAt = DateTime.Now;
                _traineeLessonProgressRepository.Update(progress);
            }
            return RedirectToAction("TraineeLessonPlan", "LessonPlan", new { traineeId = progress.Trainee.Id });
        }

    }
}