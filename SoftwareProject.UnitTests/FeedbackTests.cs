using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Softwareproject.Controllers;
using Softwareproject.Models;
using Softwareproject.Data;
using SoftwareProject.UnitTests.FakeRepositories;

namespace SoftwareProject.UnitTests;

public class FeedbackTest
{
    private static FeedbackController CreateController(
        Trainee trainee,
        FakeFeedbackRepository feedbackRepo,
        FakeTraineeRepository traineeRepo,
        FakeProgressRepository progressRepo,
        FakeLessonPlanRepository lessonPlanRepo)
    {
        var controller = new FeedbackController(feedbackRepo, traineeRepo, progressRepo, lessonPlanRepo);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, trainee.EmailAddress)
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, "Test"))
            }
        };

        return controller;
    }


    [Fact]
    public void CreateFeedback_ChangesProgressToRated_WhenFeedbackIsCreated()
    {
        var lesson = new Lesson
        {
            Id = 1,
            Title = "Test Lesson",
            URL = "https://test.de"
        };

        var trainee = new Trainee
        {
            Id = 10,
            EmailAddress = "trainee@test.de",
            LessonPlan = new LessonPlan()
        };

        trainee.LessonPlan.lessonList.Add(lesson);


        var progress = new TraineeLessonProgress
        {
            Id = 100,
            Lesson = lesson,
            Trainee = trainee,
            Status = Status.ACCEPTED
        };


        var feedbackRepo = new FakeFeedbackRepository();
        var traineeRepo = new FakeTraineeRepository();
        var progressRepo = new FakeProgressRepository();
        var lessonPlanRepo = new FakeLessonPlanRepository();


        traineeRepo.Add(trainee);
        progressRepo.Add(progress);


        var controller = CreateController(trainee, feedbackRepo, traineeRepo, progressRepo, lessonPlanRepo);


        var feedback = new Feedback
        {
            Difficulty = 4,
            Comment = "Test"
        };


        var result = controller.CreateFeedback(feedback, progress.Id);


        Assert.Single(feedbackRepo.GetAll());

        var saved = feedbackRepo.GetAll().First();

        Assert.Equal("Test", saved.Comment);
        Assert.Equal(trainee.Id, saved.Trainee.Id);

        Assert.Equal(Status.RATED, progressRepo.GetById(progress.Id)!.Status);


        Assert.IsType<RedirectToActionResult>(result);
    }


    [Fact]
    public void DeleteFeedback_RemovesFeedbackAndSetsProgressBackToAccepted()
    {
        var trainee = new Trainee
        {
            Id = 1,
            EmailAddress = "trainee@test.de"
        };

        var feedback = new Feedback
        {
            Id = 50,
            Trainee = trainee,
            Comment = "Feedback"
        };


        var progress = new TraineeLessonProgress
        {
            Id = 100,
            Status = Status.RATED
        };


        var feedbackRepo = new FakeFeedbackRepository();
        var traineeRepo = new FakeTraineeRepository();
        var progressRepo = new FakeProgressRepository();
        var lessonPlanRepo = new FakeLessonPlanRepository();


        feedbackRepo.Create(feedback);
        traineeRepo.Add(trainee);
        progressRepo.Add(progress);


        var controller = CreateController(trainee, feedbackRepo, traineeRepo, progressRepo, lessonPlanRepo);

        var result = controller.DeleteFeedback(feedback.Id, progress.Id);


        Assert.Empty(feedbackRepo.GetAll());

        Assert.Equal(Status.ACCEPTED, progressRepo.GetById(progress.Id)!.Status);

        Assert.IsType<RedirectToActionResult>(result);
    }
}