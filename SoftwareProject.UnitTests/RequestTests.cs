using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Softwareproject.Controllers;
using Softwareproject.Models;
using SoftwareProject.UnitTests.FakeRepositories;

namespace SoftwareProject.UnitTests;

// Unit-Tests für den RequestController (Aufgabenbereich "Anträge").
// Statt der echten Datenbank kommen In-Memory-Fake-Repositories zum Einsatz,
// damit die Controller-Logik isoliert und ohne EF/DB getestet werden kann.
public class RequestTest
{
    // Baut einen RequestController mit vorbefüllten Fakes auf und meldet
    // einen Mentor per E-Mail-Claim an (so wie es GetCurrentMentor() erwartet).
    private static RequestController CreateController(
        Mentor mentor,
        FakeBreakRepository breakRepo,
        FakeMentorRepository mentorRepo,
        FakeProgressRepository progressRepo,
        FakeTraineeRepository traineeRepo)
    {
        var controller = new RequestController(breakRepo, mentorRepo, progressRepo, traineeRepo);

        // Login-Cookie nachbilden: der Controller liest die E-Mail aus dem Claim.
        var claims = new[] { new Claim(ClaimTypes.Email, mentor.EmailAddress) };
        var identity = new ClaimsIdentity(claims, "Test");
        var user = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    [Fact]
    public void AcceptBreak_MarksBreakAsAccepted_WhenMentorOwnsTrainee()
    {
        // Arrange: Mentor mit eigenem Trainee, der einen offenen Break-Antrag hat.
        var mentor = new Mentor { Id = 1, EmailAddress = "mentor@test.de" };
        var trainee = new Trainee { Id = 10, EmailAddress = "trainee@test.de" };
        trainee.MentorList.Add(mentor);

        var openBreak = new Break
        {
            Id = 100,
            Accepted = false,
            Description = "Urlaub",
            StartingDate = new DateOnly(2026, 7, 20),
            EndDate = new DateOnly(2026, 7, 24),
            Trainee = trainee
        };

        var mentorRepo = new FakeMentorRepository();
        mentorRepo.Create(mentor);

        var traineeRepo = new FakeTraineeRepository();
        traineeRepo.Add(trainee);

        var breakRepo = new FakeBreakRepository();
        breakRepo.Create(openBreak);

        var progressRepo = new FakeProgressRepository();

        var controller = CreateController(mentor, breakRepo, mentorRepo, progressRepo, traineeRepo);

        // Act
        var result = controller.AcceptBreak(openBreak.Id, traineeId: null);

        // Assert: Break ist jetzt bestätigt und wir landen zurück in der Übersicht.
        Assert.True(breakRepo.GetById(100)!.Accepted);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RequestController.Index), redirect.ActionName);
    }

    [Fact]
    public void DenyLesson_SetsStatusRejectedAndStoresReason_WhenLessonWasFinished()
    {
        // Arrange: Mentor mit eigenem Trainee, der eine abgegebene Lesson (FINISHED) hat.
        var mentor = new Mentor { Id = 1, EmailAddress = "mentor@test.de" };
        var trainee = new Trainee { Id = 10, EmailAddress = "trainee@test.de" };
        trainee.MentorList.Add(mentor);

        var progress = new TraineeLessonProgress
        {
            Id = 200,
            Status = Status.FINISHED,
            Trainee = trainee
        };

        var mentorRepo = new FakeMentorRepository();
        mentorRepo.Create(mentor);

        var traineeRepo = new FakeTraineeRepository();
        traineeRepo.Add(trainee);

        var progressRepo = new FakeProgressRepository();
        progressRepo.Add(progress);

        var breakRepo = new FakeBreakRepository();

        var controller = CreateController(mentor, breakRepo, mentorRepo, progressRepo, traineeRepo);

        // Act
        var result = controller.DenyLesson(progress.Id, refusalReason: "Bitte nacharbeiten", traineeId: null);

        // Assert: Status auf REJECTED, Ablehnungsgrund gespeichert, zurück zur Übersicht.
        var updated = progressRepo.GetById(200)!;
        Assert.Equal(Status.REJECTED, updated.Status);
        Assert.Equal("Bitte nacharbeiten", updated.RefusalReason);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(RequestController.Index), redirect.ActionName);
    }
}
