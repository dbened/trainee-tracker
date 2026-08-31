using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Softwareproject.Models;
using Xunit;

namespace Softwareproject.E2ETests;

public class MentorCanStartLessonE2ETests : IClassFixture<BrowserFixture>
{
    private const string MentorEmail = "mentor@test.com";
    private const string MentorPassword = "123";

    private readonly BrowserFixture _fixture;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;


    public MentorCanStartLessonE2ETests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _driver = fixture.Driver;
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
    }


    [Fact]
    public void Mentor_CanStartOpenLesson()
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
            ".//a[contains(.,'Trainee öffnen')]"
        )).Click();


        _wait.Until(d =>
            d.Url.Contains(
                "/LessonPlan/TraineeLessonPlan",
                StringComparison.OrdinalIgnoreCase
            )
        );


        WaitForLessonCard(seed.OpenLessonTitle);


        Assert.Contains(
            "Offen",
            GetLessonCardText(seed.OpenLessonTitle)
        );


        // Click Starten
        _wait.Until(d =>
        {
            try
            {
                var card = FindLessonCard(seed.OpenLessonTitle);

                var button = card.FindElement(
                    By.XPath(".//button[contains(normalize-space(),'Starten')]")
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


        // Wait until OPEN -> STARTED
        _wait.Until(d =>
        {
            try
            {
                var text = GetLessonCardText(seed.OpenLessonTitle);

                return text.Contains("Abgeben")
                       && text.Contains("Stoppen");
            }
            catch
            {
                return false;
            }
        });


        var finalText = GetLessonCardText(seed.OpenLessonTitle);


        Assert.Contains(
            "Abgeben",
            finalText
        );


        Assert.Contains(
            "Stoppen",
            finalText
        );
    }



    private string GetLessonCardText(string title)
    {
        return _wait.Until(d =>
        {
            try
            {
                return FindLessonCard(title).Text;
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        });
    }



    private void WaitForLessonCard(string title)
    {
        _wait.Until(d =>
        {
            try
            {
                return FindLessonCard(title).Displayed;
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



    private IWebElement FindLessonCard(string lessonTitle)
    {
        return _driver.FindElement(By.XPath(
            $"//div[contains(@class,'lesson-card')][.//h3[normalize-space()='{lessonTitle}']]"
        ));
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
            Description = $"E2E Mentor Start Plan {unique}"
        };


        var trainee = new Trainee
        {
            Id = id + 1,
            Name = $"E2E Trainee {unique}",
            EmailAddress = $"e2e-start-{unique}@test.com",
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


        var openProgress = new TraineeLessonProgress
        {
            Id = id + 3,
            Trainee = trainee,
            Lesson = openLesson,
            Status = Status.OPEN,
            Order = 1
        };


        lessonPlan.lessonList.Add(openLesson);


        _fixture.DbContext.AddRange(
            lessonPlan,
            trainee,
            openLesson,
            openProgress
        );


        _fixture.DbContext.SaveChanges();


        return new SeedData(
            trainee.Name,
            openLesson.Title
        );
    }



    private void LoginAsMentor()
    {
        _driver.Manage().Cookies.DeleteAllCookies();


        _driver.Navigate().GoToUrl(
            $"{_fixture.BaseUrl}/Login/Login"
        );


        _wait.Until(d =>
            d.FindElement(By.Name("Email"))
        )
        .SendKeys(MentorEmail);


        _driver.FindElement(By.Name("Password"))
            .SendKeys(MentorPassword);


        _driver.FindElement(By.CssSelector("button[type='submit']"))
            .Click();


        _wait.Until(d =>
            d.Url.Contains(
                "/Mentor/Trainees",
                StringComparison.OrdinalIgnoreCase
            )
        );
    }



    private sealed record SeedData(
        string TraineeName,
        string OpenLessonTitle
    );
}