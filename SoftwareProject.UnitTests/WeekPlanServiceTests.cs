using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Softwareproject.Models;
using Softwareproject.Services;
using SoftwareProject.UnitTests.FakeHttp;
using SoftwareProject.UnitTests.FakeRepositories;

namespace SoftwareProject.UnitTests;

// Unit-Tests für den WeekPlanService
// Statt der echten Datenbank kommen In-Memory-Fake-Repositories zum Einsatz,
// damit die Service-Logik isoliert und ohne EF/DB getestet werden kann.
public class WeekPlanServiceTests
{
    // Test 1 und 2 nur möglich wenn Arbeitsstunden via API einer Firma ermittelt werden.
    

    // Test 1: API-Ausfall simulieren
    /* [Fact]
    public async Task GenerateWeekPlanAsync_WhenApiIsDown_ShouldProjectLessonCorrectly()
    {
        // Arrange: HttpClient mit Fake404Handler um API-Ausfall zu simulieren
        var fakeHandler = new Fake404Handler();
        var httpClient = new HttpClient(fakeHandler);
        
        var fakeBreakRepo = new FakeBreakRepository(); 
        var fakeProgressRepo = new FakeProgressRepository();
        
        var service = new WeekPlanService(fakeBreakRepo, fakeProgressRepo, httpClient);
        
        var trainee = new Trainee { 
            Id = 1, 
            EmailAddress = "trainee@test.de",
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)) 
        };


        fakeProgressRepo.Create(new TraineeLessonProgress {
            Id = 1,
            Trainee = trainee,
            Status = Status.OPEN,
            Lesson = new Lesson { Id = 1, Title = "Lesson 1", URL = "test-url"} 
        });

        // Act
        var result = await service.GenerateWeekPlanAsync(trainee, 0);

        // Assert: Prüfen, ob das ViewModel korrekt erstellt, API-Fehler erkannt wurde und trotzdem Lektionen geplant wurden
        Assert.NotNull(result);
        Assert.True(result.IsApiError); 
        Assert.NotEmpty(result.Days);

        // Testdatum der erste Tag der Projektion (entweder heute oder der nächste Montag falls Wochenende)
        DateTime testDate;
        if (DateTime.Today.DayOfWeek == DayOfWeek.Saturday)
        {
            testDate = DateTime.Today.AddDays(2);
        } 
        else if (DateTime.Today.DayOfWeek == DayOfWeek.Sunday) {
            testDate = DateTime.Today.AddDays(1); 
        } 
        else 
        {
            testDate = DateTime.Today;
        }
        
        var currentDate = result.Days.FirstOrDefault(d => d.Date.Date == testDate.Date);
        Assert.NotNull(currentDate);
        Assert.NotEmpty(currentDate.Lessons);
    } */

    // Test 2: API läuft und Lektion sollte korrekt projiziert werden
    /*[Fact]
    public async Task GenerateWeekPlanAsync_WhenApiIsUp_ShouldProjectLessonCorrectly()
    {
        // Arrange: HttpClient mit FakeHttpMessageHandler um erfolgreiche API-Antworten zu simulieren
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler);
        
        var fakeBreakRepo = new FakeBreakRepository();
        var fakeProgressRepo = new FakeProgressRepository();
        
        var service = new WeekPlanService(fakeBreakRepo, fakeProgressRepo, httpClient);
        
        var trainee = new Trainee { 
            Id = 1, 
            EmailAddress = "trainee@test.de",
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)) 
        };

        fakeProgressRepo.Create(new TraineeLessonProgress { 
            Id = 1, 
            Trainee = trainee, 
            Status = Status.OPEN,
            Lesson = new Lesson { Id = 1, Title = "Lesson 1", URL = "test-url" }
        });

        // Act
        var result = await service.GenerateWeekPlanAsync(trainee, 0);

        // Assert: Prüfen, ob IsApiError false ist, da die API ja läuft, und ob die Lesson richt projiziert wird
        Assert.False(result.IsApiError);

        // Testdatum der erste Tag der Projektion (entweder heute oder der nächste Montag falls Wochenende)
        DateTime testDate;
        if (DateTime.Today.DayOfWeek == DayOfWeek.Saturday)
        {
            testDate = DateTime.Today.AddDays(2);
        } 
        else if (DateTime.Today.DayOfWeek == DayOfWeek.Sunday) {
            testDate = DateTime.Today.AddDays(1); 
        } 
        else 
        {
            testDate = DateTime.Today;
        }

        var currentDate = result.Days.FirstOrDefault(d => d.Date.Date == testDate.Date);
        Assert.NotNull(currentDate);
        Assert.NotEmpty(currentDate.Lessons);
    } */

    // Test 3: Pausen-Logik prüfen
    [Fact]
    public async Task GenerateWeekPlanAsync_WhenBreakExists_ShouldSetIsDayOffToTrue()
    {
        // Arrange: HttpClient mit FakeHttpMessageHandler um erfolgreiche API-Antworten zu simulieren
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler);

        var fakeBreakRepo = new FakeBreakRepository();
        var fakeProgressRepo = new FakeProgressRepository();
            
        var trainee = new Trainee { 
            Id = 1, 
            EmailAddress = "trainee@test.de",
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)) 
        };

        // Testdatum der erste Tag der Projektion (entweder heute oder der nächste Montag falls Wochenende)
        DateTime testDate;
        if (DateTime.Today.DayOfWeek == DayOfWeek.Saturday)
        {
            testDate = DateTime.Today.AddDays(2);
        } 
        else if (DateTime.Today.DayOfWeek == DayOfWeek.Sunday) {
            testDate = DateTime.Today.AddDays(1); 
        } 
        else 
        {
            testDate = DateTime.Today;
        }

        var todayBreak = new Break
        {
            StartingDate = DateOnly.FromDateTime(testDate),
            EndDate = DateOnly.FromDateTime(testDate),
            Accepted = true,
            Trainee = trainee
        };

        fakeBreakRepo.Create(todayBreak);
        
        var service = new WeekPlanService(fakeBreakRepo, fakeProgressRepo, httpClient);
        
        // Act
        var result = await service.GenerateWeekPlanAsync(trainee, 0);
        
        // Assert: Prüfen, ob der nächste zu projizierende Tag als Pause markiert ist
        var currentDate = result.Days.FirstOrDefault(d => d.Date.Date == testDate.Date);

        Assert.NotNull(currentDate);
        Assert.True(currentDate.IsDayOff);
    }
}
// Benedikt Dippner