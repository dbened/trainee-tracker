using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Softwareproject.Controllers;
using Softwareproject.Data;
using Softwareproject.Models;
using SoftwareProject.UnitTests.FakeRepositories;

namespace SoftwareProject.UnitTests;

public class MentorTraineeOrderTests
{

    // Diese Methode erstellt einen LessonPlanController mit den angegebenen Fake-Repositories und einem Dummy-HttpContext, 
    // um später die MoveLessonUp-Methode zu testen. diese befindet sich in der LessonPlanController-Klasse und ist für die 
    // Verwaltung der Lektionen eines Trainees zuständig.
    private static LessonPlanController CreateLessonPlanController(
        FakeTraineeRepository traineeRepository,
        FakeProgressRepository progressRepository)
    {
        var controller = new LessonPlanController(
            traineeRepository,
            new FakeLessonPlanRepository(),
            progressRepository);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    
    // Beschreibung: Dieser Test überprüft, ob die MoveLessonUp-Methode korrekt funktioniert, 
    // wenn zwei Lektionen denselben Status haben.
    [Fact]
    public void MoveLessonUp_SwapsStoredOrderForEqualStatuses()
    {
        var trainee = new Trainee
        {
            Id = 1,
            EmailAddress = "trainee@test.de"
        };

        var firstLesson = new Lesson
        {
            Id = 10,
            Title = "Erste Lektion",
            URL = "https://example.com/1"
        };

        var secondLesson = new Lesson
        {
            Id = 20,
            Title = "Zweite Lektion",
            URL = "https://example.com/2"
        };

        var firstProgress = new TraineeLessonProgress
        {
            Id = 100,
            Trainee = trainee,
            Lesson = firstLesson,
            Status = Status.OPEN,
            Order = 1
        };

        var secondProgress = new TraineeLessonProgress
        {
            Id = 200,
            Trainee = trainee,
            Lesson = secondLesson,
            Status = Status.OPEN,
            Order = 2
        };

        var traineeRepository = new FakeTraineeRepository();
        traineeRepository.Add(trainee);

        var progressRepository = new FakeProgressRepository();
        progressRepository.Add(firstProgress);
        progressRepository.Add(secondProgress);

        var controller = CreateLessonPlanController(traineeRepository, progressRepository);

        var result = controller.MoveLessonUp(secondProgress.Id);

        Assert.Equal(2, firstProgress.Order);
        Assert.Equal(1, secondProgress.Order);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(LessonPlanController.TraineeLessonPlan), redirect.ActionName);
        var redirectRouteValues = redirect.RouteValues;
        Assert.NotNull(redirectRouteValues);
        Assert.Equal(trainee.Id, Assert.IsType<int>(redirectRouteValues["traineeId"]!));
    }

}