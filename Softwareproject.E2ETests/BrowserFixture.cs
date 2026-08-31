using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Softwareproject.Data;

namespace Softwareproject.E2ETests;

public class BrowserFixture : IDisposable
{
    public Context DbContext { get; }

    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5002";

    public IWebDriver Driver { get; }

    public BrowserFixture()
    {
        // Ausgehend von AppContext.BaseDirectory (dem festen Build-Output-Ordner dieses
        // Testprojekts) so lange nach oben laufen, bis ein Ordner mit einem "Softwareproject"-
        // Unterordner gefunden wird. AppContext.BaseDirectory statt Directory.GetCurrentDirectory(),
        // weil Letzteres unten selbst überschrieben wird (Environment.CurrentDirectory ist
        // prozessweiter, mutierbarer Zustand) – bei mehreren BrowserFixture-Instanzen im selben
        // Prozess (mehrere Testklassen) würde sonst jede weitere bereits vom verschobenen
        // Arbeitsverzeichnis der vorherigen aus rechnen. Eine feste Anzahl an .Parent-Aufrufen
        // ist außerdem fragil, weil die Build-Output-Tiefe je nach SDK/Testhost variieren kann.
        var solutionRoot = new DirectoryInfo(AppContext.BaseDirectory);
        while (solutionRoot != null && !Directory.Exists(Path.Combine(solutionRoot.FullName, "Softwareproject")))
        {
            solutionRoot = solutionRoot.Parent;
        }

        if (solutionRoot == null)
        {
            throw new DirectoryNotFoundException(
                $"Konnte den 'Softwareproject'-Ordner nicht finden (ausgehend von {AppContext.BaseDirectory}).");
        }

        var dbPath = Path.Combine(solutionRoot.FullName, "Softwareproject", "AdminDatabase.db");

        Console.WriteLine("DB Pfad: " + dbPath);

        Environment.CurrentDirectory = Path.GetDirectoryName(dbPath)!;

        DbContext = new Context();

        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1280,900");
        options.AddArgument("--disable-software-rasterizer");

        Driver = new ChromeDriver(options);
        Driver.Navigate().GoToUrl(BaseUrl);
    }

    public void Dispose()
    {
        DbContext.Dispose();

        Driver.Quit();
        Driver.Dispose();
    }
}