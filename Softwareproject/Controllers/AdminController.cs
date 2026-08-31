using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Softwareproject.Data;
using Softwareproject.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Softwareproject.Controllers;
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminRepository _adminRepository;
    private readonly IMentorRepository _mentorRepository;
    private readonly ITraineeRepository _traineeRepository;
    private readonly ILessonPlanRepository _lessonPlanRepository;
    private readonly ITraineeLessonProgressRepository _traineeLessonProgressRepository;

    public AdminController(
        IAdminRepository adminRepository,
        IMentorRepository mentorRepository,
        ITraineeRepository traineeRepository,
        ILessonPlanRepository lessonPlanRepository,
        ITraineeLessonProgressRepository traineeLessonProgressRepository)
    {
        _adminRepository = adminRepository;
        _mentorRepository = mentorRepository;
        _traineeRepository = traineeRepository;
        _lessonPlanRepository = lessonPlanRepository;
        _traineeLessonProgressRepository = traineeLessonProgressRepository;
    }

    public IActionResult Admins()
    {
        var admins = _adminRepository.GetAll();
        return View(admins);
    }
    
    public IActionResult Mentors()
    {
        return View(_mentorRepository.GetAll());
    }

    public IActionResult Trainees()
    {
        ViewBag.LessonPlans = _lessonPlanRepository.GetAll();
        ViewBag.Progresses = _traineeLessonProgressRepository.GetAll();
        ViewBag.Mentors = _mentorRepository.GetAll();

        var trainees = _traineeRepository.GetAll();
        return View(_traineeRepository.GetAll());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Admin admin, string password)
    {
        if (!ModelState.IsValid)
        {
            return View(admin);
        }
        var existingAdmin = _adminRepository.GetByEmail(admin.EmailAddress);

        if (existingAdmin != null)
        {
            ModelState.AddModelError(
                nameof(admin.EmailAddress),
                "Ein Admin mit dieser E-Mail-Adresse existiert bereits.");

            return View(admin);
        }

        var hasher = new PasswordHasher<object>();

        admin.PasswordHash = hasher.HashPassword(new object(), password);
        _adminRepository.Create(admin);

        return RedirectToAction(nameof(Admins));
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var admin = _adminRepository.GetById(id);

        if (admin == null)
            return NotFound();

        return View(admin);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Admin admin)
    {
        if (!ModelState.IsValid)
            return View(admin);

        var existingAdmin = _adminRepository.GetById(admin.Id);

        if (existingAdmin == null)
            return NotFound();

        var emailOwner = _adminRepository.GetByEmail(admin.EmailAddress);

        if (emailOwner != null && emailOwner.Id != admin.Id)
        {
            ModelState.AddModelError(
                nameof(admin.EmailAddress),
                "Diese E-Mail-Adresse wird bereits verwendet.");

            return View(admin);
        }

        existingAdmin.Name = admin.Name;
        existingAdmin.EmailAddress = admin.EmailAddress;
        existingAdmin.IsActive = admin.IsActive;

        _adminRepository.Update(existingAdmin);

        return RedirectToAction(nameof(Admins));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var admin = _adminRepository.GetById(id);

        if (admin == null)
        {
            return NotFound();
        }

        var currentEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (!string.IsNullOrEmpty(currentEmail) &&
            admin.EmailAddress.Equals(
                currentEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] =
                "Sie können Ihr eigenes Konto nicht löschen.";

            return RedirectToAction(nameof(Admins));
        }
        var adminCount = _adminRepository.GetAll().Count();

        if (adminCount <= 1)
        {
            TempData["Error"] =
                "Der letzte Admin kann nicht gelöscht werden.";

            return RedirectToAction(nameof(Admins));
        }

        _adminRepository.Delete(id);

        return RedirectToAction(nameof(Admins));
    }

    [HttpGet]
    public IActionResult MentorCreate()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MentorCreate(Mentor mentor, string password)
    {
        if (!ModelState.IsValid)
            return View(mentor);

        var existingMentor = _mentorRepository.GetByEmail(mentor.EmailAddress);

        if (existingMentor != null)
        {
            ModelState.AddModelError(
                nameof(mentor.EmailAddress),
                "Ein Mentor mit dieser E-Mail-Adresse existiert bereits.");

            return View(mentor);
        }

        var hasher = new PasswordHasher<object>();
        mentor.PasswordHash = hasher.HashPassword(new object(), password);

        _mentorRepository.Create(mentor);

        return RedirectToAction(nameof(Mentors));
    }
    
    [HttpGet]
    public IActionResult MentorEdit(int id)
    {
        var mentor = _mentorRepository.GetById(id);

        if (mentor == null)
            return NotFound();

        return View(mentor);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MentorEdit(Mentor mentor)
    {
        if (!ModelState.IsValid)
            return View(mentor);

        var existingMentor = _mentorRepository.GetById(mentor.Id);

        if (existingMentor == null)
            return NotFound();

        var emailOwner = _mentorRepository.GetByEmail(mentor.EmailAddress);

        if (emailOwner != null && emailOwner.Id != mentor.Id)
        {
            ModelState.AddModelError(
                nameof(mentor.EmailAddress),
                "Diese E-Mail-Adresse wird bereits verwendet.");

            return View(mentor);
        }

        existingMentor.Name = mentor.Name;
        existingMentor.EmailAddress = mentor.EmailAddress;
        existingMentor.IsActive = mentor.IsActive;

        _mentorRepository.Update(existingMentor);

        return RedirectToAction(nameof(Mentors));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MentorDelete(int id)
    {
        var mentor = _mentorRepository.GetById(id);

        if (mentor == null)
            return NotFound();

        _mentorRepository.Delete(id);

        return RedirectToAction(nameof(Mentors));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AssignMentor(int traineeId, int mentorId)
    {
        if (mentorId <= 0)
        {
            TempData["Error"] = "Bitte einen Mentor auswählen.";
            return RedirectToAction(nameof(Trainees));
        }

        var trainee = _traineeRepository.GetByIdWithMentors(traineeId);
        if (trainee == null)
            return NotFound();

        var mentor = _mentorRepository.GetById(mentorId);
        if (mentor == null)
            return NotFound();

        if (trainee.MentorList.Any(m => m.Id == mentor.Id))
        {
            TempData["Error"] = "Dieser Mentor ist bereits zugewiesen.";
            return RedirectToAction(nameof(Trainees));
        }

        trainee.MentorList.Add(mentor);

        // Don't create a new Trainee object or replace MentorList.
        _traineeRepository.Update(trainee);

        return RedirectToAction(nameof(Trainees));
    }
    
    [HttpGet]
    public IActionResult TraineeCreate()
    {
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TraineeCreate(Trainee trainee, string password)
    {
        if (!ModelState.IsValid)
            return View(trainee);

        var existingTrainee = _traineeRepository.GetByEmail(trainee.EmailAddress);

        if (existingTrainee != null)
        {
            ModelState.AddModelError(
                nameof(trainee.EmailAddress),
                "Ein Trainee mit dieser E-Mail-Adresse existiert bereits.");

            return View(trainee);
        }

        var hasher = new PasswordHasher<object>();
        trainee.PasswordHash = hasher.HashPassword(new object(), password);

        _traineeRepository.Create(trainee);

        return RedirectToAction(nameof(Trainees));
    }
    
    [HttpGet]
    public IActionResult TraineeEdit(int id)
    {
        var trainee = _traineeRepository.GetById(id);

        if (trainee == null)
            return NotFound();

        ViewBag.LessonPlans = _lessonPlanRepository.GetAll();
        ViewBag.Mentors = _mentorRepository.GetAll();

        return View(trainee);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TraineeEdit(Trainee trainee)
    {
        if (!ModelState.IsValid)
            return View(trainee);

        var existingTrainee = _traineeRepository.GetById(trainee.Id);

        if (existingTrainee == null)
            return NotFound();

        var emailOwner = _traineeRepository.GetByEmail(trainee.EmailAddress);

        if (emailOwner != null && emailOwner.Id != trainee.Id)
        {
            ModelState.AddModelError(
                nameof(trainee.EmailAddress),
                "Diese E-Mail-Adresse wird bereits verwendet.");

            return View(trainee);
        }

        existingTrainee.Name = trainee.Name;
        existingTrainee.EmailAddress = trainee.EmailAddress;
        existingTrainee.IsActive = trainee.IsActive;
        existingTrainee.StartDate = trainee.StartDate;
        existingTrainee.EndDate = trainee.EndDate;

        _traineeRepository.Update(existingTrainee);

        return RedirectToAction(nameof(Trainees));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TraineeDelete(int id)
    {
        var trainee = _traineeRepository.GetByIdWithMentors(id);

        if (trainee == null)
            return NotFound();

        // 1. Remove mentor relations
        trainee.MentorList.Clear();

        // 2. Remove lesson progress
        var progresses = _traineeLessonProgressRepository
            .GetAll()
            .Where(p => p.Trainee.Id == id)
            .ToList();

        foreach (var progress in progresses)
        {
            _traineeLessonProgressRepository.Delete(progress.Id);
        }

        trainee.LessonPlan = null;

        _traineeRepository.Update(trainee);

        // 4. Now delete trainee
        _traineeRepository.Delete(id);

        return RedirectToAction(nameof(Trainees));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AssignLessonPlan(int traineeId, int lessonPlanId)
    {
        if (lessonPlanId <= 0)
        {
            TempData["Error"] = "Bitte einen Lehrplan auswählen.";
            return RedirectToAction(nameof(Trainees));
        }

        var trainee = _traineeRepository.GetByIdWithLessonPlan(traineeId);
        if (trainee == null)
            return NotFound();

        var plan = _lessonPlanRepository.GetById(lessonPlanId);
        if (plan == null)
            return NotFound();

        if (trainee.LessonPlan != null && trainee.LessonPlan.Id == lessonPlanId)
        {
            TempData["Error"] = "Dieser Lehrplan ist bereits zugewiesen.";
            return RedirectToAction(nameof(Trainees));
        }

        trainee.LessonPlan = plan;

        _traineeRepository.Update(trainee);

        int order = 1;

        foreach (var lesson in plan.lessonList)

        {
            var progress = new TraineeLessonProgress
            {
                Trainee = trainee,
                Lesson = lesson,
                Status = Status.OPEN,
                Order = order
            };
            order++;
            _traineeLessonProgressRepository.Create(progress);
        }

        return RedirectToAction(nameof(Trainees));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveLessonPlan(int traineeId)
    {
        var trainee = _traineeRepository.GetByIdWithLessonPlan(traineeId);

        if (trainee == null)
            return NotFound();

        // delete progress
        var progresses = _traineeLessonProgressRepository
            .GetAll()
            .Where(p => p.Trainee.Id == traineeId)
            .ToList();

        foreach (var progress in progresses)
        {
            _traineeLessonProgressRepository.Delete(progress.Id);
        }

        trainee.LessonPlan = null;

        _traineeRepository.Update(trainee);

        return RedirectToAction(nameof(Trainees));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveMentor(int traineeId, int mentorId)
    {
        var trainee = _traineeRepository.GetByIdWithMentors(traineeId);

        if (trainee == null)
            return NotFound();

        var mentor = trainee.MentorList.FirstOrDefault(m => m.Id == mentorId);

        if (mentor == null)
        {
            TempData["Error"] = "Dieser Mentor ist nicht dem Trainee zugewiesen.";
            return RedirectToAction(nameof(Trainees));
        }

        trainee.MentorList.Remove(mentor);

        _traineeRepository.Update(trainee);

        return RedirectToAction(nameof(Trainees));
    }
    
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Passwords()
    {
        List<User> users = new();

        users.AddRange(_adminRepository.GetAll());
        users.AddRange(_mentorRepository.GetAll());
        users.AddRange(_traineeRepository.GetAll());

        users = users
            .OrderByDescending(u => u.ForgotPassword)
            .ThenBy(u => u.Name)
            .ToList();

        return View(users);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public IActionResult SetPassword(int userId, string role, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["Error"] = "Bitte ein Passwort eingeben.";
            return RedirectToAction(nameof(Passwords));
        }

        var hasher = new PasswordHasher<object>();

        switch (role)
        {
            case "Administrator":
                {
                    var admin = _adminRepository.GetById(userId);
                    if (admin == null)
                        return NotFound();

                    admin.PasswordHash = hasher.HashPassword(new object(), newPassword);
                    admin.ForgotPassword = false;

                    _adminRepository.Update(admin);
                    break;
                }

            case "Mentor":
                {
                    var mentor = _mentorRepository.GetById(userId);
                    if (mentor == null)
                        return NotFound();

                    mentor.PasswordHash = hasher.HashPassword(new object(), newPassword);
                    mentor.ForgotPassword = false;

                    _mentorRepository.Update(mentor);
                    break;
                }

            case "Trainee":
                {
                    var trainee = _traineeRepository.GetById(userId);
                    if (trainee == null)
                        return NotFound();

                    trainee.PasswordHash = hasher.HashPassword(new object(), newPassword);
                    trainee.ForgotPassword = false;

                    _traineeRepository.Update(trainee);
                    break;
                }

            default:
                return BadRequest();
        }

        return RedirectToAction(nameof(Passwords));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> LoginAs(int userId, string role)
    {
        string name;
        string email;

        switch (role)
        {
            case "Administrator":
                {
                    var admin = _adminRepository.GetById(userId);
                    if (admin == null)
                        return NotFound();

                    name = admin.Name;
                    email = admin.EmailAddress;
                    role = "Admin";
                    break;
                }

            case "Mentor":
                {
                    var mentor = _mentorRepository.GetById(userId);
                    if (mentor == null)
                        return NotFound();

                    name = mentor.Name;
                    email = mentor.EmailAddress;
                    role = "Mentor";
                    break;
                }

            case "Trainee":
                {
                    var trainee = _traineeRepository.GetById(userId);
                    if (trainee == null)
                        return NotFound();

                    name = trainee.Name;
                    email = trainee.EmailAddress;
                    role = "Trainee";
                    break;
                }

            default:
                return BadRequest();
        }

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, name),
        new Claim(ClaimTypes.Email, email),
        new Claim(ClaimTypes.Role, role)
    };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return role switch
        {
            "Admin" => RedirectToAction("Admins", "Admin"),
            "Mentor" => RedirectToAction("Trainees", "Mentor"),
            _ => RedirectToAction("LehrplanUebersicht", "LessonPlan")
        };
    }
}
