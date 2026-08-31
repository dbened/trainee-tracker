using Microsoft.AspNetCore.Mvc;
using Softwareproject.Data;
using Softwareproject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Softwareproject.Controllers;

public class LoginController : Controller
{
    private readonly IAdminRepository _adminRepo;
    private readonly IMentorRepository _mentorRepo;
    private readonly ITraineeRepository _traineeRepo;

    public LoginController(
        IAdminRepository adminRepo,
        IMentorRepository mentorRepo,
        ITraineeRepository traineeRepo)
    {
        _adminRepo = adminRepo;
        _mentorRepo = mentorRepo;
        _traineeRepo = traineeRepo;
    }
    
    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }
    
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var admin = _adminRepo.GetByEmail(model.Email);
        var mentor = _mentorRepo.GetByEmail(model.Email);
        var trainee = _traineeRepo.GetByEmail(model.Email);
        string name;
        string email;
        string role;

        var hasher = new PasswordHasher<object>();
        var user = new object();

        if (admin != null &&
            admin.IsActive &&
            hasher.VerifyHashedPassword(user, admin.PasswordHash, model.Password!)
            == PasswordVerificationResult.Success)
        {
            name = admin.Name;
            email = admin.EmailAddress;
            role = "Admin";
        }

        else if (mentor != null &&
            mentor.IsActive &&
            hasher.VerifyHashedPassword(user, mentor.PasswordHash, model.Password!)
            == PasswordVerificationResult.Success)
        {
            name = mentor.Name;
            email = mentor.EmailAddress;
            role = "Mentor";
        }
        else if (trainee != null &&
         trainee.IsActive &&
         hasher.VerifyHashedPassword(user, trainee.PasswordHash, model.Password!)
         == PasswordVerificationResult.Success)
        {
            name = trainee.Name;
            email = trainee.EmailAddress;
            role = "Trainee";
        }
        else
        {
            model.ErrorMessage = "Invalid email or password";
            return View(model);
        }


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
            "Admin" => RedirectToAction("Passwords", "Admin"),
            "Mentor" => RedirectToAction("Trainees", "Mentor"),
            _ => RedirectToAction("LehrplanUebersicht", "LessonPlan")
        };
    }
    
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login");
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(Login));

        var admin = _adminRepo.GetByEmail(email);

        if (admin != null)
        {
            admin.ForgotPassword = true;
            _adminRepo.Update(admin);

            TempData["Message"] =
                "Falls ein Konto existiert, wurde ein Administrator informiert.";

            return RedirectToAction(nameof(Login));
        }

        var mentor = _mentorRepo.GetByEmail(email);

        if (mentor != null)
        {
            mentor.ForgotPassword = true;
            _mentorRepo.Update(mentor);

            TempData["Message"] =
                "Falls ein Konto existiert, wurde ein Administrator informiert.";

            return RedirectToAction(nameof(Login));
        }

        var trainee = _traineeRepo.GetByEmail(email);

        if (trainee != null)
        {
            trainee.ForgotPassword = true;
            _traineeRepo.Update(trainee);

            TempData["Message"] =
                "Falls ein Konto existiert, wurde ein Administrator informiert.";
        }

        return RedirectToAction(nameof(Login));
    }
}
