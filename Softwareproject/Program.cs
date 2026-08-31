using Microsoft.EntityFrameworkCore;
using Softwareproject.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Softwareproject.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Cookie-basierte Authentifizierung konfigurieren.
// Nach erfolgreichem Login wird ein verschlüsseltes Cookie gesetzt,
// das bei jedem Request geprüft wird (UseAuthentication weiter unten).
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Login";         // Weiterleitung bei nicht angemeldetem Zugriff
        options.AccessDeniedPath = "/Login/Login";  // Weiterleitung bei fehlenden Rechten
    });

builder.Services.AddAuthorization();
builder.Services.AddSession();

// In-Memory-DB für ApplicationContext (wird im Projekt aktuell nicht aktiv genutzt)
builder.Services.AddDbContext<Softwareproject.Data.ApplicationContext>(options =>
    options.UseInMemoryDatabase("LessonPlanDb"));

// Repositories per Dependency Injection registrieren.
// Controller bekommen die Instanz automatisch über den Konstruktor injiziert.
builder.Services.AddScoped<Softwareproject.Data.ILessonPlanRepository, Softwareproject.Data.LessonPlanRepository>();
builder.Services.AddScoped<Softwareproject.Data.ITraineeLessonProgressRepository, Softwareproject.Data.TraineeLessonProgressRepository>();
builder.Services.AddScoped<Softwareproject.Data.IAdminRepository, Softwareproject.Data.AdminRepository>();
builder.Services.AddScoped<Softwareproject.Data.IMentorRepository, Softwareproject.Data.MentorRepository>();
builder.Services.AddScoped<Softwareproject.Data.ITraineeRepository, Softwareproject.Data.TraineeRepository>();
builder.Services.AddScoped<Softwareproject.Data.IFeedbackRepository, Softwareproject.Data.FeedbackRepository>();
builder.Services.AddScoped<Softwareproject.Data.IBreakRepository, Softwareproject.Data.BreakRepository>();
builder.Services.AddScoped<IStatisticRepository, StatisticRepository>();
builder.Services.AddScoped<IWeekPlanService, WeekPlanService>();

builder.Services.AddHttpClient();

builder.Services.AddScoped<IStatisticsRefreshService, StatisticsRefreshService>();

builder.Services.AddHostedService<StatisticsRefreshBackgroundService>();

// SQLite-Datenbankkontext erstellen und Testdaten einspielen (falls DB noch leer).
// Als Singleton registriert: eine Instanz für die gesamte App-Laufzeit.
builder.Services.AddDbContext<Context>(options =>
    options.UseSqlite("Data Source=AdminDatabase.db"));


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<Context>();
    DbInitializer.InitializeDatabase(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // wwwroot-Ordner (CSS, JS, Bilder) zugänglich machen

app.UseRouting();

// WICHTIG: UseAuthentication muss VOR UseAuthorization stehen.
// Ohne UseAuthentication werden Login-Cookies nie geprüft →
// alle [Authorize]-Routen würden sofort zu /Login umleiten, obwohl man eingeloggt ist.
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Standard-Route: /Login/Login als Startseite (kein öffentliches Home)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Login}/{id?}");

app.Run();
