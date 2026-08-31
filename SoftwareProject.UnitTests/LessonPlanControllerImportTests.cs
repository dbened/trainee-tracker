using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Softwareproject.Controllers;
using Softwareproject.Data;
using Softwareproject.Models;

namespace SoftwareProject.UnitTests;

public class LessonPlanControllerImportTests
{
    // TempData wird von ImportLessonPlan für Erfolgs-/Fehlermeldungen genutzt, braucht dafür
    // aber einen echten ITempDataProvider. Für den Test genügt eine Variante, die nichts persistiert.
    private class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    // Jeder Test bekommt eine frisch geleerte In-Memory-Datenbank. Context.cs verwendet für den
    // InMemory-Modus einen fest verdrahteten Datenbanknamen ("LessonTestDb") für alle Instanzen –
    // EnsureDeleted() sorgt dafür, dass Tests sich nicht gegenseitig über Testfälle hinweg Daten sehen.
    private static Context CreateInMemoryContext()
    {
        var context = new Context(true);
        context.Database.EnsureDeleted();
        return context;
    }

    // ImportLessonPlan liest/schreibt über die echten Repository-Implementierungen, die alle
    // auf demselben (In-Memory-)Context arbeiten – genau wie in der DI-Konfiguration in
    // Program.cs, wo Context als Scoped registriert ist und sich alle Repositories eines
    // Requests eine Instanz teilen. Reine Fake-Repositories eignen sich hier nicht, weil
    // ImportLessonPlan über die lessonList-Navigation (LessonPlan ↔ Lesson) läuft, die nur
    // ein echter EF-Core-Context automatisch synchron hält.
    private static LessonPlanController CreateController(Context context)
    {
        var controller = new LessonPlanController(
            new TraineeRepository(context),
            new LessonPlanRepository(context),
            new TraineeLessonProgressRepository(context));

        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new NullTempDataProvider());

        return controller;
    }

    private static IFormFile CreateJsonFile(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "LessonPlan", "lessons.json")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/json"
        };
    }

    [Fact]
    public void ImportLessonPlan_NewPlan_CreatesPlanWithLessonsInJsonOrder()
    {
        using var context = CreateInMemoryContext();
        var controller = CreateController(context);

        const string json = """
        [
            { "id": 1, "title": "Erste Lektion", "url": "https://example.com/1", "estimate": 1.0, "deprecated": false },
            { "id": 2, "title": "Zweite Lektion", "url": "https://example.com/2", "estimate": 2.0, "deprecated": false }
        ]
        """;

        controller.ImportLessonPlan(CreateJsonFile(json), planId: null, PlanName: "Neuer Plan");

        var plan = Assert.Single(context.LessonPlans.Include(p => p.lessonList));
        Assert.Equal("Neuer Plan", plan.Description);
        Assert.Equal(2, plan.lessonList.Count);
        Assert.Equal(0, plan.lessonList.Single(l => l.Id == 1).SortOrder);
        Assert.Equal(1, plan.lessonList.Single(l => l.Id == 2).SortOrder);
    }

    [Fact]
    public void ImportLessonPlan_LessonIdBelongsToAnotherPlan_RejectsImportAndLeavesTargetPlanUnchanged()
    {
        using var context = CreateInMemoryContext();

        var existingPlan = new LessonPlan { Description = "Bestehender Plan" };
        context.LessonPlans.Add(existingPlan);
        context.SaveChanges();
        context.Lessons.Add(new Lesson
        {
            Id = 42,
            Title = "Fremde Lektion",
            URL = "https://example.com/42",
            LessonPlan = existingPlan,
            SortOrder = 0
        });
        context.SaveChanges();

        var targetPlan = new LessonPlan { Description = "Zielplan" };
        context.LessonPlans.Add(targetPlan);
        context.SaveChanges();

        var controller = CreateController(context);

        const string json = """
        [ { "id": 42, "title": "Fremde Lektion", "url": "https://example.com/42", "estimate": 1.0, "deprecated": false } ]
        """;

        controller.ImportLessonPlan(CreateJsonFile(json), planId: targetPlan.Id, PlanName: null);

        Assert.Equal(
            "Import abgebrochen: Folgende Lektions-IDs gehören bereits zu einem anderen Lehrplan: 42.",
            controller.TempData["ErrorMessage"]);

        var reloadedTargetPlan = context.LessonPlans.Include(p => p.lessonList).Single(p => p.Id == targetPlan.Id);
        Assert.Empty(reloadedTargetPlan.lessonList);
    }
}
