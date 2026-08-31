using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Softwareproject.Data;
using Softwareproject.Models;

namespace Softwareproject.E2ETests;

// E2E-Test für LessonPlanController.UnacceptLesson: Der Mentor kann eine bereits akzeptierte
// Lektion eines Trainees zurückstufen (ACCEPTED -> FINISHED), z.B. wenn ein Review doch
// nachgebessert werden muss.
public class MentorCanUnacceptLessonE2ETests : IClassFixture<BrowserFixture>
{
    private const string MentorEmail = "mentor@test.com";
    private const string MentorPassword = "123";

    private readonly BrowserFixture _fixture;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public MentorCanUnacceptLessonE2ETests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _driver = fixture.Driver;
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void Mentor_CanSetAcceptedLessonBackToFinished()
    {
        var seed = CreateSeedData();

        LoginAsMentor();

        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Mentor/Trainees");

        var traineeCard = _wait.Until(d => d.FindElement(By.XPath(
            $"//article[contains(@class,'trainee-card')][.//div[contains(@class,'name') and normalize-space()='{seed.TraineeName}']]")));

        traineeCard.FindElement(By.XPath(".//a[contains(.,'Trainee öffnen')]"))
            .Click();

        _wait.Until(d => d.Url.Contains("/LessonPlan/TraineeLessonPlan", StringComparison.OrdinalIgnoreCase));

        var acceptedCard = FindLessonCard(seed.AcceptedLessonTitle);

        // Prüfen, dass die Lektion wirklich akzeptiert ist
        Assert.Contains("Akzeptiert", acceptedCard.Text);

        // Button "Akzeptanz zurücknehmen" klicken
        acceptedCard.FindElement(
            By.XPath(".//button[contains(normalize-space(),'Akzeptanz zurücknehmen')]")
        ).Click();

        // UnacceptLesson leitet auf dieselbe TraineeLessonPlan-URL zurück (keine echte
        // Navigation) – deshalb hier direkt auf den neuen Kartentext warten statt auf einen
        // URL-Wechsel, der nie eintritt. WaitForCardText fängt dabei auch eine
        // StaleElementReferenceException ab, falls der Reload zwischen Suchen und Lesen liegt.
        var acceptedCardText = WaitForCardText(seed.AcceptedLessonTitle, text => text.Contains("Abgegeben"));

        // Nach dem Zurücknehmen muss der Status wieder "Abgegeben" sein, der Button verschwindet
        Assert.Contains("Abgegeben", acceptedCardText);
        Assert.DoesNotContain("Akzeptiert", acceptedCardText);
        Assert.DoesNotContain("Akzeptanz zurücknehmen", acceptedCardText);
    }

    private SeedData CreateSeedData()
    {
        var mentor = _fixture.DbContext.Mentors.First(x => x.EmailAddress == MentorEmail);

        var unique = Guid.NewGuid().ToString("N")[..8];
        var baseId = Random.Shared.Next(1_000_000, 1_500_000_000);

        var lessonPlan = new LessonPlan
        {
            Id = baseId,
            Description = $"E2E Mentor Unaccept Plan {unique}"
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

        var acceptedLesson = new Lesson
        {
            Id = baseId + 2,
            Title = $"E2E Accepted Lesson {unique}",
            URL = "https://example.com/accepted",
            LessonPlan = lessonPlan,
            SortOrder = 1
        };

        lessonPlan.lessonList.Add(acceptedLesson);

        var acceptedProgress = new TraineeLessonProgress
        {
            Id = baseId + 3,
            Trainee = trainee,
            Lesson = acceptedLesson,
            Status = Status.ACCEPTED,
            Order = 1
        };

        _fixture.DbContext.AddRange(lessonPlan, trainee, acceptedLesson, acceptedProgress);
        _fixture.DbContext.SaveChanges();

        return new SeedData(
            trainee.Name,
            acceptedLesson.Title
        );
    }

    private IWebElement FindLessonCard(string lessonTitle)
    {
        return _wait.Until(d => d.FindElement(By.XPath(
            $"//div[contains(@class,'lesson-card')][.//h3[normalize-space()='{lessonTitle}']]")));
    }

    // Wartet, bis die Karte den erwarteten Text enthält, und gibt genau den Text zurück,
    // mit dem die Bedingung erfüllt war (kein erneutes Lesen mehr nötig). Fängt dabei
    // StaleElementReferenceException ab: Sucht/list die Karte nicht erst frisch, wenn zwischen
    // Klick und Lesen ein Seiten-Reload liegt (z.B. weil eine Aktion auf dieselbe URL
    // zurückleitet, sodass ein reiner URL-Wechsel-Wait nicht als Signal taugt).
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

    private void LoginAsMentor()
    {
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Login/Login");

        _wait.Until(d => d.FindElement(By.Name("Email"))).SendKeys(MentorEmail);
        _driver.FindElement(By.Name("Password")).SendKeys(MentorPassword);
        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        _wait.Until(d => d.Url.Contains("/Mentor/Trainees", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record SeedData(string TraineeName, string AcceptedLessonTitle);
}
