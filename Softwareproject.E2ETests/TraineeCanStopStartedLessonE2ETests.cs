using Microsoft.AspNetCore.Identity;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Softwareproject.Data;
using Softwareproject.Models;

namespace Softwareproject.E2ETests;

// E2E-Test für LessonController.StopLesson: Der Trainee kann eine gestartete Lektion selbst
// wieder stoppen (STARTED -> OPEN), z.B. falls sie doch noch nicht fertig ist.
public class TraineeCanStopStartedLessonE2ETests : IClassFixture<BrowserFixture>
{
    private const string TraineePassword = "123";

    private readonly BrowserFixture _fixture;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public TraineeCanStopStartedLessonE2ETests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _driver = fixture.Driver;
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void Trainee_CanStopStartedLesson()
    {
        var seed = CreateSeedData();

        LoginAsTrainee(seed.TraineeEmail);

        var startedCard = FindLessonCard(seed.StartedLessonTitle);

        // Prüfen, dass die Lektion wirklich gestartet ist
        Assert.Contains("In Bearbeitung", startedCard.Text);
        Assert.Contains("Stoppen", startedCard.Text);

        // Button "Stoppen" klicken
        startedCard.FindElement(
            By.XPath(".//button[contains(normalize-space(),'Stoppen')]")
        ).Click();

        // StopLesson leitet auf dieselbe LehrplanUebersicht-URL zurück (keine echte
        // Navigation) – deshalb hier direkt auf den neuen Kartentext warten statt auf einen
        // URL-Wechsel, der nie eintritt. WaitForCardText fängt dabei auch eine
        // StaleElementReferenceException ab, falls der Reload zwischen Suchen und Lesen liegt.
        var startedCardText = WaitForCardText(seed.StartedLessonTitle, text => text.Contains("Offen"));

        // Nach dem Stoppen muss wieder "Starten" erscheinen
        Assert.Contains("Offen", startedCardText);
        Assert.Contains("Starten", startedCardText);
        Assert.DoesNotContain("Stoppen", startedCardText);
        Assert.DoesNotContain("Abgeben", startedCardText);
    }

    private SeedData CreateSeedData()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var baseId = Random.Shared.Next(1_000_000, 1_500_000_000);
        var hasher = new PasswordHasher<object>();

        var lessonPlan = new LessonPlan
        {
            Id = baseId,
            Description = $"E2E Trainee Stop Plan {unique}"
        };

        var traineeEmail = $"e2e-trainee-{unique}@test.com";
        var trainee = new Trainee
        {
            Id = baseId + 1,
            Name = $"E2E Trainee {unique}",
            EmailAddress = traineeEmail,
            PasswordHash = hasher.HashPassword(new object(), TraineePassword),
            IsActive = true,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7)),
            LessonPlan = lessonPlan
        };

        var startedLesson = new Lesson
        {
            Id = baseId + 2,
            Title = $"E2E Started Lesson {unique}",
            URL = "https://example.com/started",
            LessonPlan = lessonPlan,
            SortOrder = 1
        };

        lessonPlan.lessonList.Add(startedLesson);

        var startedProgress = new TraineeLessonProgress
        {
            Id = baseId + 3,
            Trainee = trainee,
            Lesson = startedLesson,
            Status = Status.STARTED,
            Order = 1
        };

        _fixture.DbContext.AddRange(lessonPlan, trainee, startedLesson, startedProgress);
        _fixture.DbContext.SaveChanges();

        return new SeedData(traineeEmail, startedLesson.Title);
    }

    private IWebElement FindLessonCard(string lessonTitle)
    {
        return _wait.Until(d => d.FindElement(By.XPath(
            $"//div[contains(@class,'lesson-card')][.//h3[normalize-space()='{lessonTitle}']]")));
    }

    // Wartet, bis die Karte den erwarteten Text enthält, und gibt genau den Text zurück,
    // mit dem die Bedingung erfüllt war (kein erneutes Lesen mehr nötig). Fängt dabei
    // StaleElementReferenceException ab, falls zwischen Klick und Lesen ein Seiten-Reload liegt.
    private string WaitForCardText(string lessonTitle, Func<string, bool> condition)
    {
        string? lastText = null;
        _wait.Until(d =>
        {
            try
            {
                var card = d.FindElement(By.XPath(
                    $"//div[contains(@class,'lesson-card')][.//h3[normalize-space()='{lessonTitle}']]"));
                lastText = card.Text;
                return condition(lastText);
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });
        return lastText!;
    }

    private void LoginAsTrainee(string email)
    {
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Login/Login");

        _wait.Until(d => d.FindElement(By.Name("Email"))).SendKeys(email);
        _driver.FindElement(By.Name("Password")).SendKeys(TraineePassword);
        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        _wait.Until(d => d.Url.Contains("/LehrplanUebersicht", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record SeedData(string TraineeEmail, string StartedLessonTitle);
}