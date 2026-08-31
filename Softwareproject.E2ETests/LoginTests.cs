using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Softwareproject.E2ETests;

// End-to-end tests for the login functionality.
// Selenium automates a real browser and verifies that
// the application behaves correctly from a user's perspective.
public class LoginTests : IClassFixture<BrowserFixture>
{
    // Shared browser fixture used by all tests.
    private readonly BrowserFixture _fixture;
    // Selenium WebDriver used to control the browser.
    private readonly IWebDriver _driver;
    /// Constructor receives the shared browser fixture.
    public LoginTests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _driver = fixture.Driver;
    }
    
    // Verifies that a valid administrator login redirects
    // the user to the administrator page.
    [Fact]
    public void Login_WithValidCredentials_ShouldRedirect()
    {
        // Open the login page.
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Login/Login");
        // Wait up to 10 seconds for page elements to appear.
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        // Enter the administrator email.
        wait.Until(d => d.FindElement(By.Name("Email")))
            .SendKeys("admin@test.com");
        // Enter the administrator password.
        wait.Until(d => d.FindElement(By.Name("Password")))
            .SendKeys("123");
        // Click the login button.
        wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")))
            .Click();
        // Wait until the browser leaves the login page.
        wait.Until(d => !d.Url.Contains("/Login/Login"));
        // Verify that the administrator page was opened.
        Assert.Contains("/Admin/Passwords", _driver.Url);
    }

    [Fact]
    public void Login_WithMentorCredentials_ShouldRedirectToTraineesOverview()
    {
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Login/Login");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

        wait.Until(d => d.FindElement(By.Name("Email")))
            .SendKeys("mentor@test.com");

        wait.Until(d => d.FindElement(By.Name("Password")))
            .SendKeys("123");

        wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")))
            .Click();

        wait.Until(d => d.Url.Contains("/Mentor/Trainees"));

        Assert.Contains("Trainees", _driver.PageSource);
    }
    
    // Verifies that an incorrect password displays an error message.
    [Fact]
    public void Login_WithWrongPassword_ShouldShowError()
    {
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Login/Login");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

        wait.Until(d => d.FindElement(By.Name("Email")))
            .SendKeys("mentor@test.com");
        // Enter an incorrect password.
        wait.Until(d => d.FindElement(By.Name("Password")))
            .SendKeys("WrongPassword");
        // Submit the login form.
        wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")))
            .Click();
        // Wait until the error message appears.
        var error = wait.Until(d =>
            d.FindElement(By.CssSelector("[style*='color:red']"))
        );
        // Verify that the error message is visible.
        Assert.True(error.Displayed);
    }
    
    [Fact]
    public void LoginPage_ShouldContainLoginControls()
    {
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Login/Login");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        // Verify the email input exists.
        Assert.True(
            wait.Until(d => d.FindElement(By.Name("Email"))).Displayed
        );
        // Verify the password input exists.
        Assert.True(
            wait.Until(d => d.FindElement(By.Name("Password"))).Displayed
        );
        // Verify the login button exists.
        Assert.True(
            wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']"))).Displayed
        );
    }
    
    // Verifies that clicking the "Forgot Password" link opens the browser prompt.
    [Fact]
    public void ForgotPassword_ShouldOpenPrompt()
    {
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/Login/Login");

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        // Click the "Forgot Password" link
        wait.Until(d => d.FindElement(By.LinkText("Passwort vergessen?")))
            .Click();
        // Wait until the JavaScript alert appears.
        var alert = wait.Until(d =>
        {
            try
            {
                // Return the alert once it exists.
                return d.SwitchTo().Alert();
            }
            catch (NoAlertPresentException)
            {
                // Keep waiting if the alert has not appeared yet.
                return null;
            }
        });
        // Enter an email address into the prompt.
        alert.SendKeys("false@test.com");
        // Confirm the prompt.
        alert.Accept();
    }
}