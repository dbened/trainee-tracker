using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Softwareproject.E2ETests;

public class FeedbackTests : IClassFixture<BrowserFixture>
{
    private readonly IWebDriver _driver;
    private readonly BrowserFixture _fixture;
    private readonly WebDriverWait _wait;
    public FeedbackTests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _driver = fixture.Driver;
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CreateAndDeleteFeedback_ShouldWork()
    {
        TestDataSeeder.CreateAcceptedLesson(_fixture.DbContext);
        LoginAsTrainee();
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/LessonPlan/LehrplanUebersicht");

        var feedbackLink = _wait.Until(d => d.FindElement(By.LinkText("Feedback geben")));
        feedbackLink.Click();

        new SelectElement(_wait.Until(d => d.FindElement(By.Name("Difficulty")))).SelectByValue("4");
        _driver.FindElement(By.Name("PriorKnowledge")).SendKeys("Grundkenntnisse C#");
        _driver.FindElement(By.Name("Effort")).SendKeys("5 Stunden");
        _driver.FindElement(By.Name("Comment")).SendKeys("E2E Test Feedback");

        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        Thread.Sleep(1000); // kurz warten, damit Redirect abgeschlossen ist

        _wait.Until(d => d.PageSource.Contains("Feedback Ändern"));
        Console.WriteLine("URL nach Speichern: " + _driver.Url);
        Assert.Contains("Bewertet", _driver.PageSource);

        var deleteButton = _wait.Until(d => d.FindElement(By.XPath("//button[contains(text(),'Feedback löschen')]")));

        deleteButton.Click();

        _wait.Until(d =>
        {
            try
            {
                return d.SwitchTo().Alert();
            }
            catch (NoAlertPresentException)
            {
                return null;
            }
        }).Accept();

        _wait.Until(d =>
            d.FindElements(By.XPath("//button[contains(text(),'Feedback löschen')]")).Count == 0
        );

        Assert.Contains("Akzeptiert", _driver.PageSource);
    }
    private void LoginAsTrainee()
    {
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Login/Login");
        _driver.FindElement(By.Id("Email")).SendKeys("trainee@test.com");
        _driver.FindElement(By.Id("Password")).SendKeys("123");
        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        _wait.Until(d => d.Url.Contains("/LehrplanUebersicht"));
    }
}
