using Softwareproject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Softwareproject.Data
{
    // Befüllt die Datenbank beim ersten Start mit Testdaten.
    // Wird in Program.cs aufgerufen, bevor der Web-Server startet.
    // Die Methode prüft zuerst ob schon Daten vorhanden sind (Admins.Any()) –
    // falls ja, wird nichts gemacht (idempotent, kein doppeltes Seeding).
    //
    // Um die DB zurückzusetzen: AdminDatabase.db löschen → beim nächsten Start neu angelegt.
    public class DbInitializer
    {
        public static void InitializeDatabase(Context context)
        {
            // Erstellt die SQLite-Datei und alle Tabellen, falls noch nicht vorhanden.
            // Nutzt das EF Core Modell (DbSets + OnModelCreating) als Schema-Definition.
            context.Database.EnsureCreated();

            // Läuft bei JEDEM Start (anders als der Rest dieser Methode, der nur beim allerersten
            // Start ausgeführt wird), damit Änderungen an lehrplan_devop.json auch bei bereits
            // bestehender DB übernommen werden.
            ImportDevOpsLehrplan(context);

            // Guard: Wenn bereits ein Admin existiert, wurde die DB schon initialisiert → abbrechen
            if (context.Admins.Any())
            {
                return;
            }
            var hasher = new PasswordHasher<object>();
            // Test-Admin für Login (Email: admin@test.com, Passwort: 123)
            context.Admins.Add(new Admin
            {
                Name = "Admin Anton",
                EmailAddress = "admin@test.com",
                PasswordHash = hasher.HashPassword(new object(), "123"),
                IsActive = true
            });
            context.SaveChanges();

            // Test-Trainee – StartDate ist DateOnly (kein Uhrzeit-Anteil)
            var trainee = new Trainee
            {
                Name = "Max Mustermann",
                EmailAddress = "trainee@test.com",
                PasswordHash = hasher.HashPassword(new object(), "123"),
                IsActive = true,
                StartDate = DateOnly.FromDateTime(new DateTime(2026, 1, 1)),
                EndDate = DateOnly.FromDateTime(new DateTime(2027, 8, 8))
            };
            var trainee2 = new Trainee
            {
                Name = "Maximilian Mustermann",
                EmailAddress = "trainee2@test.com",
                PasswordHash = hasher.HashPassword(new object(), "123"),
                IsActive = true,
                StartDate = DateOnly.FromDateTime(new DateTime(2026, 1, 2)),
                EndDate = DateOnly.FromDateTime(new DateTime(2027, 8, 9))
            };
            context.Trainees.Add(trainee);
            context.Trainees.Add(trainee2);
            context.SaveChanges(); // SaveChanges nötig, damit trainee.Id von der DB gesetzt wird

            // Test-Mentor
            var mentor = new Mentor
            {
                Name = "Mentor Mustermann",
                EmailAddress = "mentor@test.com",
                PasswordHash = hasher.HashPassword(new object(), "123"),
                IsActive = true
            };
            context.Mentors.Add(mentor);
            context.SaveChanges();

            // Mentor dem Trainee zuordnen, damit dessen Breakrequests in der
            // Mentor-Request-Übersicht (nur eigene Trainees) erscheinen.
            trainee.MentorList.Add(mentor);
            trainee2.MentorList.Add(mentor);
            context.SaveChanges();

            // Beispiel-Breakrequest: offen (Accepted = false) und damit in der
            // Request-Übersicht des Mentors zum Annehmen/Ablehnen sichtbar.
            context.Breaks.Add(new Break
            {
                Description = "Urlaub – Familienfeier",
                StartingDate = DateOnly.FromDateTime(new DateTime(2026, 7, 13)),
                EndDate = DateOnly.FromDateTime(new DateTime(2026, 7, 17)),
                Accepted = false,
                Trainee = trainee
            });
            context.SaveChanges();

            // Lehrplan anlegen (noch ohne Trainee-Zuweisung)
            var plan = new LessonPlan { Description = "Importierter Lehrplan" };
            context.LessonPlans.Add(plan);
            context.SaveChanges(); // SaveChanges nötig, damit plan.Id gesetzt wird

            // Trainee dem Lehrplan zuweisen.
            trainee.LessonPlan = plan;
            context.SaveChanges();

            // Test-Lektionen mit expliziten IDs aus dem echten Lehrplan-JSON.
            // [DatabaseGenerated(DatabaseGeneratedOption.None)] in Lesson.cs sorgt dafür,
            // dass EF diese IDs nicht ignoriert und SQLite keine eigenen generiert.
            var lessons = new List<Lesson>
            {
                new Lesson { Id = 1001, Title = "000 Start here!", URL = "https://example.com/curriculum/start", EstimatedDays = 0, SortOrder = 0, LessonPlan = plan },
                new Lesson { Id = 1002, Title = "105 Ruby basics", URL = "https://example.com/curriculum/ruby-basics", EstimatedDays = 2, SortOrder = 1, LessonPlan = plan },
                new Lesson { Id = 1003, Title = "110 API-Dokumentation", URL = "https://example.com/curriculum/api-basics", EstimatedDays = 0.5, SortOrder = 2, LessonPlan = plan },
                new Lesson { Id = 1004, Title = "120 Git basics", URL = "https://example.com/curriculum/git-basics", EstimatedDays = 1, SortOrder = 3, LessonPlan = plan },
                new Lesson { Id = 1005, Title = "125 Gems & Bundler", URL = "https://example.com/curriculum/gems-bundler", EstimatedDays = 1, SortOrder = 4, LessonPlan = plan },
                new Lesson { Id = 1006, Title = "130 Rails basics", URL = "https://example.com/curriculum/rails-basics", EstimatedDays = 4, SortOrder = 5, LessonPlan = plan },
            };
            context.Lessons.AddRange(lessons);
            context.SaveChanges();

            // Fortschrittseinträge in verschiedenen Zuständen – für UI-Tests aller Status-Ansichten.
            // Jeder Eintrag verbindet einen Trainee mit einer Lektion und hält den aktuellen Status.
            // RefusalReason ist nullable – nur bei REJECTED gesetzt.
            context.TraineeLessonProgresses.AddRange(new List<TraineeLessonProgress>
            {
                new TraineeLessonProgress { Status = Status.REJECTED, RefusalReason = "Programmierübungen unvollständig.", Trainee = trainee, Lesson = lessons[0], StatusChangedAt = new DateTime(2026, 07, 09), Order = 1 },
                new TraineeLessonProgress { Status = Status.ACCEPTED, Trainee = trainee, Lesson = lessons[1], StatusChangedAt = new DateTime(2026, 07, 10), Order = 2 },
                new TraineeLessonProgress { Status = Status.FINISHED, Trainee = trainee, Lesson = lessons[2], StatusChangedAt = new DateTime(2026, 07, 13), Order = 3 },
                new TraineeLessonProgress { Status = Status.STARTED,  Trainee = trainee, Lesson = lessons[3], StatusChangedAt = DateTime.Today, Order = 4 },
                new TraineeLessonProgress { Status = Status.OPEN,     Trainee = trainee, Lesson = lessons[4], Order = 5 },
                new TraineeLessonProgress { Status = Status.SKIPPED,   Trainee = trainee, Lesson = lessons[5], Order = 6 },
            });
            context.TraineeLessonProgresses.AddRange(new List<TraineeLessonProgress>
            {
                new TraineeLessonProgress { Status = Status.RATED, Trainee = trainee2, Lesson = lessons[0], Order = 1 },
                new TraineeLessonProgress { Status = Status.RATED, Trainee = trainee2, Lesson = lessons[1], Order = 2 },
                new TraineeLessonProgress { Status = Status.RATED, Trainee = trainee2, Lesson = lessons[2], Order = 3 },
                new TraineeLessonProgress { Status = Status.RATED, Trainee = trainee2, Lesson = lessons[3], Order = 4 },
                new TraineeLessonProgress { Status = Status.RATED, Trainee = trainee2, Lesson = lessons[4], Order = 5 },
                new TraineeLessonProgress { Status = Status.RATED, Trainee = trainee2, Lesson = lessons[5], Order = 6 },
            });
            context.Feedbacks.AddRange(new List<Feedback>
            {
                new()
                {
                    Difficulty = 1,
                    PriorKnowledge = "Sehr erfahren mit Ruby",
                    Effort = "1h",
                    Comment = "Sehr einfach und schnell durchgearbeitet.",
                    Trainee = trainee2,
                    LessonId = lessons[0].Id
                },
                new()
                {
                     Difficulty = 2,
                     PriorKnowledge = "Grundkenntnisse vorhanden",
                     Effort = "3h",
                     Comment = "Guter Einstieg, verständlich erklärt.",
                     Trainee = trainee2,
                     LessonId = lessons[1].Id
                },
                new()
                {
                    Difficulty = 4,
                    PriorKnowledge = "Keine Vorkenntnisse",
                    Effort = "8h",
                    Comment = "Etwas schwierig, aber machbar mit Recherche.",
                    Trainee = trainee2,
                    LessonId = lessons[2].Id
                },
                new()
                {
                    Difficulty = 3,
                    PriorKnowledge = "Git Basics bekannt",
                    Effort = "5h",
                    Comment = "Praktisch und gut aufgebaut.",
                    Trainee = trainee2,
                    LessonId = lessons[3].Id
                },
                new()
                {
                    Difficulty = 3,
                    PriorKnowledge = "Grundlagen vorhanden",
                    Effort = "4h",
                    Comment = "Ganz okay verständlich, aber teilweise etwas trocken.",
                    Trainee = trainee2,
                    LessonId = lessons[4].Id
                },
                new()
                {
                    Difficulty = 5,
                    PriorKnowledge = "Fortgeschritten",
                    Effort = "10h",
                    Comment = "Sehr anspruchsvoll, aber gut dokumentiert.",
                    Trainee = trainee2,
                    LessonId = lessons[5].Id
                }

            });
            context.SaveChanges();



            // Zusätzliche neutrale Demo-Accounts.
            var demoAdmin = new Admin
            {
                Name = "Demo Admin",
                EmailAddress = "admin@example.com",
                PasswordHash = hasher.HashPassword(new object(), "DemoPassword123!"),
                IsActive = true
            };

            var demoTrainee = new Trainee
            {
                Name = "Demo Trainee",
                EmailAddress = "trainee@example.com",
                PasswordHash = hasher.HashPassword(new object(), "DemoPassword123!"),
                IsActive = true,
                StartDate = DateOnly.FromDateTime(new DateTime(2026, 1, 1)),
                EndDate = DateOnly.FromDateTime(new DateTime(2027, 12, 31)),
            };

            var demoMentor = new Mentor
            {
                Name = "Demo Mentor",
                EmailAddress = "mentor@example.com",
                PasswordHash = hasher.HashPassword(new object(), "DemoPassword123!"),
                IsActive = true
            };

            demoTrainee.MentorList.Add(demoMentor);
            context.Trainees.Add(demoTrainee);
            context.Mentors.Add(demoMentor);
            context.Admins.Add(demoAdmin);
            context.SaveChanges();
        }

        // Lädt demo_curriculum.json (gleiches Format wie der JSON-Import über
        // LessonPlanController.ImportLessonPlan) in einen festen "DevOps Lehrplan".
        // Legt den Plan beim ersten Aufruf an, aktualisiert danach bei jedem Start Metadaten
        // bestehender Lektionen und markiert aus der Datei entfernte Lektionen als deprecated,
        // statt sie zu löschen (damit begonnener Trainee-Fortschritt erhalten bleibt).
        private static void ImportDevOpsLehrplan(Context context)
        {
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "demo_curriculum.json");
            if (!File.Exists(jsonPath))
            {
                return;
            }

            var importedLessons = JsonSerializer.Deserialize<List<Lesson>>(File.ReadAllText(jsonPath));
            if (importedLessons == null || !importedLessons.Any())
            {
                return;
            }

            const string planDescription = "DevOps Lehrplan";

            var plan = context.LessonPlans
                .Include(p => p.lessonList)
                .FirstOrDefault(p => p.Description == planDescription);

            if (plan == null)
            {
                plan = new LessonPlan { Description = planDescription };
                context.LessonPlans.Add(plan);
                context.SaveChanges(); // SaveChanges nötig, damit plan.Id gesetzt wird

                plan = context.LessonPlans.Include(p => p.lessonList).First(p => p.Id == plan.Id);
            }

            var importedIds = importedLessons.Select(l => l.Id).ToHashSet();

            // Lektionen, die nicht mehr in der Datei enthalten sind, als deprecated markieren
            foreach (var lesson in plan.lessonList.Where(l => !importedIds.Contains(l.Id)))
            {
                lesson.IsDeprecated = true;
            }

            for (int i = 0; i < importedLessons.Count; i++)
            {
                var imported = importedLessons[i];
                var existing = plan.lessonList.FirstOrDefault(l => l.Id == imported.Id);

                if (existing != null)
                {
                    existing.Title = imported.Title;
                    existing.URL = imported.URL;
                    existing.EstimatedDays = imported.EstimatedDays;
                    existing.IsDeprecated = imported.IsDeprecated;
                    existing.SortOrder = i;
                }
                else
                {
                    plan.lessonList.Add(new Lesson
                    {
                        Id = imported.Id,
                        Title = imported.Title,
                        URL = imported.URL,
                        EstimatedDays = imported.EstimatedDays,
                        IsDeprecated = imported.IsDeprecated,
                        SortOrder = i,
                        LessonPlan = plan
                    });
                }
            }

            context.SaveChanges();
        }
    }
}
