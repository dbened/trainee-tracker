using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Softwareproject.Models;

namespace Softwareproject.E2ETests;

public class MentorCanResetSkippedLessonE2ETests : IClassFixture<BrowserFixture>
{
    private const string MentorEmail = "mentor@test.com";
    private const string MentorPassword = "123";

    private readonly BrowserFixture _fixture;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;


    public MentorCanResetSkippedLessonE2ETests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _driver = fixture.Driver;
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
    }


    [Fact]
    public void Mentor_CanResetSkippedLesson()
    {
        var seed = CreateSeedData();

        LoginAsMentor();

        _driver.Navigate()
            .GoToUrl($"{_fixture.BaseUrl}/Mentor/Trainees");


        var traineeCard = _wait.Until(d =>
            d.FindElement(By.XPath(
                $"//article[contains(@class,'trainee-card')]" +
                $"[.//div[contains(@class,'name') and " +
                $"normalize-space()='{seed.TraineeName}']]"
            )));


        traineeCard.FindElement(
            By.XPath(".//a[contains(.,'Trainee öffnen')]"))
            .Click();


        var skippedCard = FindLessonCard(seed.SkippedLessonTitle);


        Assert.Contains(
            "Übersprungen",
            skippedCard.Text);


        skippedCard.FindElement(
            By.XPath(
                ".//button[contains(normalize-space(),'Wieder aktivieren')]"
            ))
            .Click();


        // wait until the card changed after reload
        _wait.Until(d =>
        {
            try
            {
                var card = FindLessonCard(seed.SkippedLessonTitle);
                return card.Text.Contains("Überspringen");
            }
            catch
            {
                return false;
            }
        });


        skippedCard = FindLessonCard(seed.SkippedLessonTitle);


        Assert.Contains(
            "Überspringen",
            skippedCard.Text);

        Assert.DoesNotContain(
            "Übersprungen",
            skippedCard.Text);
    }



    private SeedData CreateSeedData()
    {
        var mentor = _fixture.DbContext.Mentors
            .First(x => x.EmailAddress == MentorEmail);


        var baseId = Math.Abs(Guid.NewGuid().GetHashCode());


        var lessonPlan = new LessonPlan
        {
            Id = baseId,
            Description = $"E2E Mentor Reset Plan {baseId}"
        };


        var trainee = new Trainee
        {
            Id = baseId + 1,
            Name = $"E2E Trainee {baseId}",
            EmailAddress = $"e2e-reset-{baseId}@test.com",
            PasswordHash = string.Empty,
            IsActive = true,
            StartDate = DateOnly.FromDateTime(
                DateTime.Today.AddDays(-7)),
            LessonPlan = lessonPlan
        };


        trainee.MentorList.Add(mentor);



        var skippedLesson = new Lesson
        {
            Id = baseId + 2,
            Title = $"E2E Skipped Lesson {baseId}",
            URL = "https://example.com/skipped",
            LessonPlan = lessonPlan,
            SortOrder = 1
        };


        lessonPlan.lessonList.Add(skippedLesson);



        var skippedProgress = new TraineeLessonProgress
        {
            Id = baseId + 3,
            Trainee = trainee,
            Lesson = skippedLesson,
            Status = Status.SKIPPED,
            Order = 1
        };


        _fixture.DbContext.AddRange(
            lessonPlan,
            trainee,
            skippedLesson,
            skippedProgress);


        _fixture.DbContext.SaveChanges();


        return new SeedData(
            trainee.Name,
            skippedLesson.Title);
    }



    private IWebElement FindLessonCard(string lessonTitle)
    {
        return _wait.Until(d =>
        {
            try
            {
                var element = d.FindElement(By.XPath(
                    $"//div[contains(@class,'lesson-card')]" +
                    $"[.//h3[normalize-space()='{lessonTitle}']]"
                ));

                return element.Displayed
                    ? element
                    : null;
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
        });
    }



    private void LoginAsMentor()
    {
        _driver.Navigate()
            .GoToUrl($"{_fixture.BaseUrl}/Login/Login");


        var email = _wait.Until(
            d => d.FindElement(By.Name("Email")));

        email.Clear();
        email.SendKeys(MentorEmail);


        var password = _driver.FindElement(
            By.Name("Password"));

        password.Clear();
        password.SendKeys(MentorPassword);


        _driver.FindElement(
            By.CssSelector("button[type='submit']"))
            .Click();


        _wait.Until(d =>
            d.Url.Contains(
                "/Mentor/Trainees",
                StringComparison.OrdinalIgnoreCase));
    }



    private sealed record SeedData(
        string TraineeName,
        string SkippedLessonTitle);
}