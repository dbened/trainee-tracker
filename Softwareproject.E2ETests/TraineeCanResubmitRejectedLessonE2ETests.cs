using Microsoft.AspNetCore.Identity;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Softwareproject.Data;
using Softwareproject.Models;

namespace Softwareproject.E2ETests;

// E2E-Test für LessonController.ResubmitLesson: Der Trainee kann eine vom Mentor abgelehnte
// Lektion nach der Überarbeitung erneut abgeben (REJECTED -> FINISHED).
public class TraineeCanResubmitRejectedLessonE2ETests : IClassFixture<BrowserFixture>
{
    private const string TraineePassword = "123";

    private readonly BrowserFixture _fixture;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public TraineeCanResubmitRejectedLessonE2ETests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _driver = fixture.Driver;
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void Trainee_CanResubmitRejectedLesson()
    {
        var seed = CreateSeedData();

        LoginAsTrainee(seed.TraineeEmail);

        var rejectedCard = FindLessonCard(seed.RejectedLessonTitle);

        // Prüfen, dass die Lektion wirklich abgelehnt ist, inkl. Ablehnungsgrund
        Assert.Contains("Abgelehnt", rejectedCard.Text);
        Assert.Contains(seed.RefusalReason, rejectedCard.Text);
        Assert.Contains("Überarbeitung abgeben", rejectedCard.Text);

        // Button "Überarbeitung abgeben" klicken
        rejectedCard.FindElement(
            By.XPath(".//button[contains(normalize-space(),'Überarbeitung abgeben')]")
        ).Click();

        // ResubmitLesson leitet auf dieselbe LehrplanUebersicht-URL zurück (keine echte
        // Navigation) – deshalb hier direkt auf den neuen Kartentext warten statt auf einen
        // URL-Wechsel, der nie eintritt. WaitForCardText fängt dabei auch eine
        // StaleElementReferenceException ab, falls der Reload zwischen Suchen und Lesen liegt.
        var rejectedCardText = WaitForCardText(seed.RejectedLessonTitle, text => text.Contains("Abgegeben"));

        // Nach dem erneuten Abgeben muss der Status "Abgegeben" sein, Ablehnungsgrund verschwindet
        Assert.Contains("Abgegeben", rejectedCardText);
        Assert.Contains("Zurückziehen", rejectedCardText);
        Assert.DoesNotContain("Abgelehnt", rejectedCardText);
        Assert.DoesNotContain(seed.RefusalReason, rejectedCardText);
    }

    private SeedData CreateSeedData()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var baseId = Random.Shared.Next(1_000_000, 1_500_000_000);
        var hasher = new PasswordHasher<object>();
        var refusalReason = $"Bitte nacharbeiten {unique}";

        var lessonPlan = new LessonPlan
        {
            Id = baseId,
            Description = $"E2E Trainee Resubmit Plan {unique}"
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

        var rejectedLesson = new Lesson
        {
            Id = baseId + 2,
            Title = $"E2E Rejected Lesson {unique}",
            URL = "https://example.com/rejected",
            LessonPlan = lessonPlan,
            SortOrder = 1
        };

        lessonPlan.lessonList.Add(rejectedLesson);

        var rejectedProgress = new TraineeLessonProgress
        {
            Id = baseId + 3,
            Trainee = trainee,
            Lesson = rejectedLesson,
            Status = Status.REJECTED,
            RefusalReason = refusalReason,
            Order = 1
        };

        _fixture.DbContext.AddRange(lessonPlan, trainee, rejectedLesson, rejectedProgress);
        _fixture.DbContext.SaveChanges();

        return new SeedData(traineeEmail, rejectedLesson.Title, refusalReason);
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

    private sealed record SeedData(string TraineeEmail, string RejectedLessonTitle, string RefusalReason);
}