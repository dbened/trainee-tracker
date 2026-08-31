using OpenQA.Selenium;
using OpenQA.Selenium.DevTools.V148.WebAuthn;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Softwareproject.E2ETests;

// End-to-End-Test für den Aufgabenbereich "WeekPlan" (WeekPlanController + Weekplan-Views).
//
// Wie der Lehrplan-Import-Test steuert dieser Test einen echten Browser über Selenium und
// klickt sich durch die Oberfläche wie ein echter Benutzer. Der konkrete Browser wird NICHT
// mehr hier im Test erzeugt, sondern über die geteilte BrowserFixture bereitgestellt
// (IClassFixture) – dadurch steht im Test selbst keine Browser-/Chrome-spezifische
// Konfiguration mehr, und alle Tests einer Klasse teilen sich einen Browser-Prozess.
//
// VORAUSSETZUNG zum Ausführen:
//   Die App muss laufen:  im Ordner "Softwareproject"  ->  dotnet run
//   (erreichbar unter http://localhost:5002; überschreibbar über E2E_BASE_URL).
public class WeekPlanE2ETest : IClassFixture<BrowserFixture>
{
    private readonly BrowserFixture _fixture;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public WeekPlanE2ETest(BrowserFixture fixture)
    {
        _fixture = fixture;
        _driver = fixture.Driver;
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GetWeekPlan_ReturnsSuccessAndCorrectStructure()
    {
        // Arrange: Login als Trainee
        LoginAsTrainee("trainee@test.com");

        // Act: Navigiere zum Wochenplan
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/WeekPlan/Index");

        // Assert: Prüfen, ob die Seite erfolgreich geladen wurde und die erwarteten Elemente vorhanden sind

        // 1. Titel
        var pageTitle = _wait.Until(d => d.FindElement(By.TagName("h1")));
        Assert.Contains("Wochenplan", pageTitle.Text);

        // 2. API-Warnung (falls vorhanden)
        var warning = _driver.FindElements(By.ClassName("api-warning"));
        if (warning.Count > 0) Assert.Contains("Zeiterfassungssystem ist momentan nicht erreichbar", warning[0].Text);

        // 3. Kalender-Tage
        var calendar = _driver.FindElement(By.ClassName("calendar"));
        var days = calendar.FindElements(By.ClassName("day-column"));
        Assert.Equal(5, days.Count);
    }

    [Fact]
    public void WeekPlan_Navigation_ShouldUpdateDateRange()
    {
        // Arrange: Login als Trainee
        LoginAsTrainee("trainee@test.com");

        // Act: Navigiere zum Wochenplan
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/WeekPlan/Index");

        // Aktuelle Datumsspanne aus der Navigation lesen
        var dateRange = _wait.Until(d => d.FindElement(By.CssSelector(".week-navigation span")));
        string initialDateText = dateRange.Text;

        // Act: Auf den "»" Button klicken
        var nextWeekButton = _driver.FindElement(By.XPath("//a[contains(text(), '»')]"));
        nextWeekButton.Click();

        // Assert: Prüfen, ob sich der Text in der Navigation geändert hat
        _wait.Until(d => d.FindElement(By.CssSelector(".week-navigation span")).Text != initialDateText);
        
        var newDateText = _driver.FindElement(By.CssSelector(".week-navigation span")).Text;
        
        Assert.NotEqual(initialDateText, newDateText);
        Assert.Contains("Woche vom", newDateText);
    }

    // Meldet einen Trainee über das echte Login-Formular an
    private void LoginAsTrainee(string email)
    {
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Login/Login");

        _driver.FindElement(By.Id("Email")).SendKeys(email);
        _driver.FindElement(By.Id("Password")).SendKeys("123");
        _driver.FindElement(By.CssSelector("form button[type='submit']")).Click();

        _wait.Until(d => d.Url.Contains("/LessonPlan/LehrplanUebersicht", StringComparison.OrdinalIgnoreCase));
    }
}
// Benedikt Dippner