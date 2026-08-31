using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Softwareproject.Models;
using Xunit;

namespace Softwareproject.E2ETests;

public class MentorSkipLessonE2ETests : IClassFixture<BrowserFixture>
{
    private const string MentorEmail = "mentor@test.com";
    private const string MentorPassword = "123";

    private readonly BrowserFixture _fixture;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public MentorSkipLessonE2ETests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _driver = fixture.Driver;
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
    }


    [Fact]
    public void Mentor_CanSkipLesson_FromOpenAndCannotSkipFinishedLesson()
    {
        var seed = CreateSeedData();

        LoginAsMentor();

        _driver.Navigate().GoToUrl(
            $"{_fixture.BaseUrl}/Mentor/Trainees"
        );


        var traineeCard = _wait.Until(d =>
            d.FindElement(By.XPath(
                $"//article[contains(@class,'trainee-card')]//div[contains(@class,'name') and normalize-space()='{seed.TraineeName}']/ancestor::article"
            ))
        );


        traineeCard.FindElement(By.XPath(
            ".//a[contains(normalize-space(),'Trainee öffnen')]"
        )).Click();


        _wait.Until(d =>
            d.Url.Contains(
                "/LessonPlan/TraineeLessonPlan",
                StringComparison.OrdinalIgnoreCase)
        );


        // Check initial state
        Assert.Contains(
            "Überspringen",
            GetLessonCardText(seed.OpenLessonTitle)
        );


        Assert.DoesNotContain(
            "Überspringen",
            GetLessonCardText(seed.FinishedLessonTitle)
        );


        // Click skip button
        ClickLessonButton(
            seed.OpenLessonTitle,
            "Überspringen"
        );


        // Wait until UI changed
        _wait.Until(d =>
        {
            try
            {
                return GetLessonCardText(seed.OpenLessonTitle)
                    .Contains("Übersprungen");
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        });


        // Verify final state
        Assert.Contains(
            "Übersprungen",
            GetLessonCardText(seed.OpenLessonTitle)
        );


        Assert.DoesNotContain(
            "Überspringen",
            GetLessonCardText(seed.FinishedLessonTitle)
        );
    }



    private string GetLessonCardText(string title)
    {
        return FindLessonCard(title).Text;
    }



    private void ClickLessonButton(string lessonTitle, string buttonText)
    {
        _wait.Until(d =>
        {
            try
            {
                var card = FindLessonCard(lessonTitle);

                var button = card.FindElement(
                    By.XPath(
                        $".//button[contains(normalize-space(),'{buttonText}')]"
                    )
                );

                button.Click();

                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        });
    }



    private IWebElement FindLessonCard(string title)
    {
        return _wait.Until(d =>
            d.FindElement(By.XPath(
                $"//div[contains(@class,'lesson-card')][.//*[normalize-space()='{title}']]"
            ))
        );
    }



    private SeedData CreateSeedData()
    {
        var mentor = _fixture.DbContext.Mentors
            .First(x => x.EmailAddress == MentorEmail);


        var unique = Guid.NewGuid()
            .ToString("N")[..8];


        var id = Random.Shared.Next(
            1_000_000,
            1_500_000_000
        );


        var lessonPlan = new LessonPlan
        {
            Id = id,
            Description = $"E2E Skip Plan {unique}"
        };


        var trainee = new Trainee
        {
            Id = id + 1,
            Name = $"E2E Trainee {unique}",
            EmailAddress = $"e2e-skip-{unique}@test.com",
            PasswordHash = string.Empty,
            IsActive = true,
            StartDate = DateOnly.FromDateTime(
                DateTime.Today.AddDays(-7)
            ),
            LessonPlan = lessonPlan
        };


        trainee.MentorList.Add(mentor);


        var openLesson = new Lesson
        {
            Id = id + 2,
            Title = $"E2E Open Lesson {unique}",
            URL = "https://example.com",
            LessonPlan = lessonPlan,
            SortOrder = 1
        };


        var finishedLesson = new Lesson
        {
            Id = id + 3,
            Title = $"E2E Finished Lesson {unique}",
            URL = "https://example.com",
            LessonPlan = lessonPlan,
            SortOrder = 2
        };


        lessonPlan.lessonList.Add(openLesson);
        lessonPlan.lessonList.Add(finishedLesson);


        _fixture.DbContext.AddRange(
            lessonPlan,
            trainee,
            openLesson,
            finishedLesson
        );

        _fixture.DbContext.SaveChanges();


        _fixture.DbContext.AddRange(
            new TraineeLessonProgress
            {
                Id = id + 4,
                Trainee = trainee,
                Lesson = openLesson,
                Status = Status.OPEN,
                Order = 1
            },

            new TraineeLessonProgress
            {
                Id = id + 5,
                Trainee = trainee,
                Lesson = finishedLesson,
                Status = Status.FINISHED,
                Order = 2
            }
        );


        _fixture.DbContext.SaveChanges();


        return new SeedData(
            trainee.Name,
            openLesson.Title,
            finishedLesson.Title
        );
    }



    private void LoginAsMentor()
    {
        _driver.Navigate().GoToUrl(
            $"{_fixture.BaseUrl}/Login/Login"
        );


        _wait.Until(d =>
            d.FindElement(By.Name("Email"))
        ).SendKeys(MentorEmail);


        _driver.FindElement(By.Name("Password"))
            .SendKeys(MentorPassword);


        _driver.FindElement(By.CssSelector("button[type='submit']"))
            .Click();


        _wait.Until(d =>
            d.Url.Contains(
                "/Mentor/Trainees",
                StringComparison.OrdinalIgnoreCase)
        );
    }



    private sealed record SeedData(
        string TraineeName,
        string OpenLessonTitle,
        string FinishedLessonTitle
    );
}