using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Softwareproject.Data;
using Softwareproject.Models;

namespace Softwareproject.E2ETests;

// E2E-Test für RequestController.DenyLesson: Der Mentor lehnt eine abgegebene Lektion aus
// seiner Anträge-Inbox ab (FINISHED -> REJECTED). Läuft über das Bestätigungs-Modal auf
// /Request/Index, das bei "Ablehnen" zusätzlich einen Pflicht-Ablehnungsgrund abfragt.
public class MentorCanDenyFinishedLessonE2ETests : IClassFixture<BrowserFixture>
{
    private const string MentorEmail = "mentor@test.com";
    private const string MentorPassword = "123";

    private readonly BrowserFixture _fixture;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public MentorCanDenyFinishedLessonE2ETests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _driver = fixture.Driver;
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void Mentor_CanDenyFinishedLesson()
    {
        var seed = CreateSeedData();
        var refusalReason = $"Bitte nacharbeiten {Guid.NewGuid():N}"[..30];

        LoginAsMentor();

        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Request/Index");

        var cardXpath = $"//div[contains(@class,'card')][.//p[contains(text(),'{seed.LessonTitle}')]]";
        var card = _wait.Until(d => d.FindElement(By.XPath(cardXpath)));

        // "Ablehnen"-Button GENAU in dieser Karte klicken -> Bestätigungs-Modal öffnet sich
        // und fragt (weil data-needs-reason="true") zusätzlich einen Ablehnungsgrund ab.
        card.FindElement(By.XPath(".//form[@action='/Request/DenyLesson']//button[@type='submit']")).Click();

        _wait.Until(d => d.FindElement(By.Id("confirmModal")).GetAttribute("class")!.Contains("open"));
        _driver.FindElement(By.Id("modalReasonInput")).SendKeys(refusalReason);
        _driver.FindElement(By.Id("modalConfirm")).Click();

        // Nach dem Ablehnen ist die Lektion nicht mehr FINISHED und verschwindet aus der Inbox.
        _wait.Until(d => d.FindElements(By.XPath(cardXpath)).Count == 0);
        Assert.Empty(_driver.FindElements(By.XPath(cardXpath)));

        // Zustandsübergang gegenprüfen: in der Trainee-Ansicht des Mentors muss die Lektion
        // jetzt als "Abgelehnt" mit dem eingegebenen Ablehnungsgrund geführt werden.
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Mentor/Trainees");

        var traineeCard = _wait.Until(d => d.FindElement(By.XPath(
            $"//article[contains(@class,'trainee-card')][.//div[contains(@class,'name') and normalize-space()='{seed.TraineeName}']]")));

        traineeCard.FindElement(By.XPath(".//a[contains(.,'Trainee öffnen')]")).Click();

        _wait.Until(d => d.Url.Contains("/LessonPlan/TraineeLessonPlan", StringComparison.OrdinalIgnoreCase));

        var lessonCard = FindLessonCard(seed.LessonTitle);
        Assert.Contains("Abgelehnt", lessonCard.Text);
        Assert.Contains(refusalReason, lessonCard.Text);
    }

    private SeedData CreateSeedData()
    {
        var mentor = _fixture.DbContext.Mentors.First(x => x.EmailAddress == MentorEmail);

        var unique = Guid.NewGuid().ToString("N")[..8];
        var baseId = Random.Shared.Next(1_000_000, 1_500_000_000);

        var lessonPlan = new LessonPlan
        {
            Id = baseId,
            Description = $"E2E Mentor Deny Plan {unique}"
        };

        var trainee = new Trainee
        {
            Id = baseId + 1,
            Name = $"E2E Trainee {unique}",
            EmailAddress = $"e2e-trainee-{unique}@test.com",
            PasswordHash = string.Empty,
            IsActive = true,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7)),
            LessonPlan = lessonPlan
        };

        trainee.MentorList.Add(mentor);

        var finishedLesson = new Lesson
        {
            Id = baseId + 2,
            Title = $"E2E Deny Lesson {unique}",
            URL = "https://example.com/deny",
            LessonPlan = lessonPlan,
            SortOrder = 1
        };

        lessonPlan.lessonList.Add(finishedLesson);

        var finishedProgress = new TraineeLessonProgress
        {
            Id = baseId + 3,
            Trainee = trainee,
            Lesson = finishedLesson,
            Status = Status.FINISHED,
            Order = 1
        };

        _fixture.DbContext.AddRange(lessonPlan, trainee, finishedLesson, finishedProgress);
        _fixture.DbContext.SaveChanges();

        return new SeedData(trainee.Name, finishedLesson.Title);
    }

    private IWebElement FindLessonCard(string lessonTitle)
    {
        return _wait.Until(d => d.FindElement(By.XPath(
            $"//div[contains(@class,'lesson-card')][.//h3[normalize-space()='{lessonTitle}']]")));
    }

    private void LoginAsMentor()
    {
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Login/Login");

        _wait.Until(d => d.FindElement(By.Name("Email"))).SendKeys(MentorEmail);
        _driver.FindElement(By.Name("Password")).SendKeys(MentorPassword);
        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        _wait.Until(d => d.Url.Contains("/Mentor/Trainees", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record SeedData(string TraineeName, string LessonTitle);
}

