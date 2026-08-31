using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Softwareproject.E2ETests;

// E2E-Test für den Lehrplan-Import: loggt sich als Mentor ein, lädt über die Import-Seite eine
// JSON-Datei mit neuen Lektionen hoch und prüft, dass der dabei neu angelegte Lehrplan
// anschließend in der Lehrplan-Übersicht auftaucht.
//
// Nutzt aktuell dieselbe AdminDatabase.db wie die lokale Entwicklung – eine eigene, isolierte
// Test-Datenbank für E2E-Läufe ist ein offener CI/CD-Punkt (Aufgabe 9), nicht Teil dieses Tests.
public class LessonPlanImportE2ETests : IClassFixture<BrowserFixture>
{
    private readonly IWebDriver _driver;
    private readonly BrowserFixture _fixture;
    private readonly WebDriverWait _wait;

    public LessonPlanImportE2ETests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _driver = fixture.Driver;
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ImportLessonPlan_UploadedJsonFile_CreatesNewPlanVisibleInVerwaltung()
    {
        LoginAsMentor();

        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/LessonPlan/Import");

        var planName = $"E2E Test Plan {Guid.NewGuid():N}";
        _driver.FindElement(By.Name("PlanName")).SendKeys(planName);

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "TestData", "e2e_lessonplan.json");
        _driver.FindElement(By.Name("LessonPlan")).SendKeys(jsonPath);

        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        try
        {
            _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/LessonPlan/Verwaltung");

            _wait.Until(d => d.PageSource.Contains(planName));
            Assert.Contains(planName, _driver.PageSource);
        }
        finally
        {
            // Es gibt (noch) keinen Hart-Löschen-Button in der Oberfläche (nur "Deaktivieren"),
            // daher räumen wir den beim Import angelegten Testplan direkt über den DbContext
            // wieder auf. So bleibt die AdminDatabase.db nach jedem Testlauf im Ausgangszustand
            // und muss nicht manuell zurückgesetzt werden.
            DeleteLessonPlan(planName);
        }
    }

    private void DeleteLessonPlan(string planName)
    {
        var plan = _fixture.DbContext.LessonPlans
            .Include(p => p.lessonList)
            .FirstOrDefault(p => p.Description == planName);

        if (plan == null) return;

        _fixture.DbContext.Lessons.RemoveRange(plan.lessonList);
        _fixture.DbContext.LessonPlans.Remove(plan);
        _fixture.DbContext.SaveChanges();
    }

    private void LoginAsMentor()
    {
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Login/Login");
        _driver.FindElement(By.Id("Email")).SendKeys("mentor@test.com");
        _driver.FindElement(By.Id("Password")).SendKeys("123");
        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        _wait.Until(d => d.Url.Contains("/Mentor/Trainees"));
    }
}
