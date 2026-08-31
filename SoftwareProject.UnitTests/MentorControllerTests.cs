using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Softwareproject.Controllers;
using Softwareproject.Models;
using SoftwareProject.UnitTests.FakeRepositories;

namespace SoftwareProject.UnitTests;

public class MentorControllerTests
{
    private static MentorController CreateController(Mentor mentor, FakeMentorRepository mentorRepository, FakeTraineeRepository traineeRepository)
    {
        var controller = new MentorController(mentorRepository, traineeRepository);

        var claims = new[] { new Claim(ClaimTypes.Email, mentor.EmailAddress) };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };

        return controller;
    }

    [Fact]
    public void Trainees_ReturnsOnlyTraineesAssignedToCurrentMentor()
    {
        var mentor = new Mentor
        {
            Id = 1,
            Name = "Mentor One",
            EmailAddress = "mentor@test.de"
        };

        var otherMentor = new Mentor
        {
            Id = 2,
            Name = "Mentor Two",
            EmailAddress = "mentor2@test.de"
        };

        var assignedTrainee = new Trainee
        {
            Id = 10,
            Name = "Assigned Trainee",
            EmailAddress = "trainee@test.de"
        };
        assignedTrainee.MentorList.Add(mentor);

        var otherTrainee = new Trainee
        {
            Id = 11,
            Name = "Other Trainee",
            EmailAddress = "other@test.de"
        };
        otherTrainee.MentorList.Add(otherMentor);

        var mentorRepository = new FakeMentorRepository();
        mentorRepository.Create(mentor);
        mentorRepository.Create(otherMentor);

        var traineeRepository = new FakeTraineeRepository();
        traineeRepository.Add(assignedTrainee);
        traineeRepository.Add(otherTrainee);

        var controller = CreateController(mentor, mentorRepository, traineeRepository);

        var result = controller.Trainees();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<Trainee>>(viewResult.Model);

        Assert.Single(model);
        Assert.Equal(assignedTrainee.Id, model[0].Id);
    }
}