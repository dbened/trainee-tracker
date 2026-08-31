using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Softwareproject.Data;
using Softwareproject.Models;

namespace Softwareproject.Controllers
{
    [Authorize]
    public class LessonPlanController : Controller
    {
        // Repository für Lehrplan-Operationen
        private readonly ILessonPlanRepository _lessonPlanRepository;

        // Repository für den Fortschritt eines Trainees bei einzelnen Lektionen
        private readonly ITraineeLessonProgressRepository _progressRepository;
        private readonly ITraineeRepository _traineeRepository;

        public LessonPlanController(ITraineeRepository traineeRpository, ILessonPlanRepository lessonPlanRepository, ITraineeLessonProgressRepository progressRepository)
        {
            _lessonPlanRepository = lessonPlanRepository;
            _progressRepository = progressRepository;
            _traineeRepository = traineeRpository;
        }

        
        // POST /LessonPlan/ImportLessonPlan
        // Verarbeitet eine hochgeladene JSON-Datei mit einer Liste von Lektionen.
        // Die Datei enthält ein flaches Array: [{ "id": 34973, "title": "...", "url": "...", ... }, ...]
        // planId: null/0 → neuen Lehrplan anlegen; ansonsten wird der bestehende Plan aktualisiert
        // (Fortschritt der Trainees bleibt dabei erhalten).
        [HttpPost]
        [Authorize(Roles = "Admin,Mentor")]
        public IActionResult ImportLessonPlan(IFormFile LessonPlan, int? planId, string? PlanName)
        {
            // JSON-Datei auslesen und als Liste von Lesson-Objekten deserialisieren
            using var reader = new StreamReader(LessonPlan.OpenReadStream());
            var json = reader.ReadToEnd();

            List<Lesson>? importedLessons;
            try
            {
                importedLessons = JsonSerializer.Deserialize<List<Lesson>>(json);
            }
            catch (JsonException)
            {
                TempData["ErrorMessage"] = "Die hochgeladene Datei ist kein gültiges JSON oder enthält unvollständige Lektionen (title/url sind Pflichtfelder).";
                return RedirectToAction("Import");
            }

            // Abbruch bei leerer oder ungültiger Datei
            if (importedLessons == null || !importedLessons.Any())
            {
                TempData["ErrorMessage"] = "Die hochgeladene Datei ist leer oder ungültig.";
                return RedirectToAction("Import");
            }

            // Zielplan (nur lesend) ermitteln: der auf der Import-Seite ausgewählte bestehende Plan,
            // oder null, wenn ein neuer Plan angelegt werden soll bzw. der gewählte nicht mehr existiert.
            var targetPlanId = planId.HasValue && planId.Value > 0 && _lessonPlanRepository.Exists(planId.Value)
                ? planId
                : null;

            // HashSet der IDs aus der JSON-Datei – für schnellen Zugriff
            var importedIds = importedLessons.Select(l => l.Id).ToHashSet();

            // Lesson.Id kommt unverändert aus der JSON-Datei (kein Auto-increment) und ist
            // tabellenweit eindeutig – eine Lektion gehört immer zu genau einem Plan.
            // Gehört eine importierte ID bereits zu einem ANDEREN Plan, würde das Speichern
            // später mit einer SQLite-Exception abbrechen. Das hier vorher prüfen, BEVOR
            // überhaupt ein (neuer) Plan angelegt wird – sonst bliebe bei einem Abbruch ein leerer Plan zurück.
            var conflictingIds = _lessonPlanRepository.GetAll()
                .SelectMany(p => p.lessonList.Where(l => importedIds.Contains(l.Id) && p.Id != targetPlanId))
                .Select(l => l.Id)
                .ToList();
            if (conflictingIds.Any())
            {
                TempData["ErrorMessage"] = $"Import abgebrochen: Folgende Lektions-IDs gehören bereits zu einem anderen Lehrplan: {string.Join(", ", conflictingIds)}.";
                return RedirectToAction("Import");
            }

            LessonPlan plan;
            if (targetPlanId.HasValue)
            {
                plan = _lessonPlanRepository.GetById(targetPlanId.Value)!;
            }
            else
            {
                var description = string.IsNullOrWhiteSpace(PlanName) ? "Importierter Lehrplan" : PlanName.Trim();
                plan = new LessonPlan { Description = description };
                _lessonPlanRepository.Create(plan);

                // Neu laden, damit die lessonList-Collection initialisiert ist
                plan = _lessonPlanRepository.GetById(plan.Id)!;
            }

            // Lektionen, die im letzten Import vorhanden waren, jetzt aber fehlen → als deprecated markieren.
            // Sie werden nicht gelöscht, damit bereits gestartete Fortschritte erhalten bleiben.
            foreach (var lesson in plan.lessonList.Where(l => !importedIds.Contains(l.Id)))
                lesson.IsDeprecated = true;

            // Importierte Lektionen verarbeiten – Reihenfolge im JSON bestimmt SortOrder
            var newLessons = new List<Lesson>();
            for (int i = 0; i < importedLessons.Count; i++)
            {
                var imported = importedLessons[i];

                // Prüfen ob diese Lektion (anhand der ID) schon im Plan existiert
                var existing = plan.lessonList.FirstOrDefault(l => l.Id == imported.Id);

                if (existing != null)
                {
                    // Lektion bereits vorhanden: nur Metadaten aktualisieren.
                    // Der TraineeLessonProgress (Status, Fortschritt) bleibt unverändert!
                    existing.Title = imported.Title;
                    existing.URL = imported.URL;
                    existing.EstimatedDays = imported.EstimatedDays;
                    existing.IsDeprecated = imported.IsDeprecated;
                    existing.SortOrder = i;
                }
                else
                {
                    // Neue Lektion: mit der expliziten ID aus der JSON-Datei anlegen.
                    // (DatabaseGeneratedOption.None in Lesson.cs sorgt dafür, dass EF diese ID übernimmt)
                    var newLesson = new Lesson
                    {
                        Id = imported.Id,
                        Title = imported.Title,
                        URL = imported.URL,
                        EstimatedDays = imported.EstimatedDays,
                        IsDeprecated = imported.IsDeprecated,
                        SortOrder = i,
                        LessonPlan = plan  // Navigation statt expliziter FK-ID (shadow property)
                    };
                    plan.lessonList.Add(newLesson);
                    newLessons.Add(newLesson);
                }
            }

            // Alle Änderungen (deprecated + aktualisiert + neu) in einem DB-Aufruf speichern
            _lessonPlanRepository.Update(plan);

            // Für jede neu hinzugefügte (nicht-deprecated) Lektion einen Fortschrittseintrag anlegen,
            // falls dem Lehrplan ein Trainee zugewiesen ist.
            if (newLessons.Any())
            {
                // Den Trainee suchen, der diesen Lehrplan hat.
                // Der FK liegt auf LessonPlan-Seite (shadow property "TraineeId"), die Navigation
                // ist aber nur auf Trainee-Seite (Trainee.LessonPlan) verfügbar.
                var assignedTrainee = _traineeRepository.GetAll()
                    .FirstOrDefault(t => t.LessonPlan != null && t.LessonPlan.Id == plan.Id);

                if (assignedTrainee != null)
                {
                    foreach (var lesson in newLessons.Where(l => !l.IsDeprecated))
                    {
                        // Neuer Fortschrittseintrag mit Status OPEN – der Trainee muss die Lektion noch starten
                        _progressRepository.Create(new TraineeLessonProgress
                        {
                            Status = Status.OPEN,
                            Trainee = assignedTrainee,
                            Lesson = lesson
                        });
                    }
                }
            }

            TempData["SuccessMessage"] = "Der Lehrplan wurde erfolgreich importiert und aktualisiert.";
            return RedirectToAction("Import");
        }
        
        //zeigt das Import-Formular an
        // ViewBag.Plans füllt das Dropdown, mit dem zwischen "neuer Plan" und
        // "bestehenden Plan aktualisieren" gewählt werden kann.
        [HttpGet]
        [Authorize(Roles = "Admin,Mentor")]
        public IActionResult Import()
        {
            ViewBag.Plans = _lessonPlanRepository.GetAll();
            return View();
        }
        
        //Mentor-/Admin-Übersicht aller Lehrpläne
        [HttpGet]
        [Authorize(Roles = "Admin,Mentor")]
        public IActionResult Verwaltung()
        {
            var plans = _lessonPlanRepository.GetAll();
            return View(plans);
        }
        
        //markiert einen Lehrplan als inaktiv (nicht gelöscht)
        [HttpPost]
        [Authorize(Roles = "Admin,Mentor")]
        public IActionResult Deactivate(int id)
        {
            var plan = _lessonPlanRepository.GetById(id);
            if (plan != null)
            {
                plan.IsDeprecated = true;
                _lessonPlanRepository.Update(plan);
            }
            return RedirectToAction("Verwaltung");
        }
        
        //Trainee-Ansicht: zeigt alle eigenen Lektionen
        // Sortierung nach Priorität: abgelehnte zuerst (müssen überarbeitet werden), dann aktive, etc.
        // Deprecated Lektionen werden ausgeblendet, außer der Trainee hat sie bereits angefangen (nicht OPEN)
        [HttpGet]
        [Authorize(Roles = "Trainee")]
        public IActionResult LehrplanUebersicht()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            var progressList = _progressRepository.GetAll()
                .Where(p => p.Lesson != null &&
                        p.Trainee != null &&
                        p.Trainee.EmailAddress == email &&
                        (!p.Lesson.IsDeprecated || p.Status != Status.OPEN)
                )
                .OrderBy(p => StatusSortOrder(p.Status))
                .ThenBy(p => p.Order)
                .ToList();

            return View(progressList);
        }
        
        //Mentor-/Admin-Ansicht eines bestimmten Plans
        // Zeigt die aktiven Lektionen des Plans (ohne Trainee-Bearbeitungsstand,
        // der wird über die Anträge-Inbox im RequestController abgewickelt)
        [HttpGet]
        [Authorize(Roles = "Admin,Mentor")]
        public IActionResult LehrplanUebersichtVerwaltung(int planId)
        {
            var plan = _lessonPlanRepository.GetById(planId);
            var lessons = plan?.lessonList
                .Where(l => !l.IsDeprecated)
                .OrderBy(l => l.SortOrder)
                .ToList() ?? [];

            return View(lessons);
        }
        
        // Hilfsfunktion: liefert die Sortierprioritätszahl für einen Status.
        // Kleinere Zahl = weiter oben in der Liste.
        // Abgelehnte Lektionen stehen ganz oben, damit der Trainee sie sofort sieht.
        private static int StatusSortOrder(Status status) => status switch
        {
            Status.REJECTED => 1,  // ganz oben – muss überarbeitet werden
            Status.ACCEPTED => 2,  // akzeptiert – wartet auf Feedback/Bewertung
            Status.FINISHED => 3,  // abgegeben – wartet auf Review des Mentors
            Status.STARTED => 4,   // in Bearbeitung
            Status.OPEN => 5,      // noch nicht begonnen
            Status.SKIPPED => 6,   // übersprungen
            Status.RATED => 7,     // abgeschlossen und bewertet
            _ => 8
        };
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult TraineeLessonPlan(int traineeId)
        {
            var trainee = _traineeRepository.GetByIdWithLessonPlan(traineeId);

            if (trainee == null)
                return NotFound();

            var progress = _progressRepository
                .GetAll()
                .Where(p => p.Trainee.Id == traineeId)
                .OrderBy(p => StatusSortOrder(p.Status))
                .ThenBy(p => p.Order)
                .ToList();

            ViewBag.Trainee = trainee;

            return View(progress);
        }


        // ── MENTOR-AKTIONEN AUF DEM LEHRPLAN EINES TRAINEES ──────────────────────
        // Werden aus TraineeLessonPlan.cshtml aufgerufen. Der Mentor kann dieselben
        // Statusübergänge wie der Trainee auslösen (starten, stoppen, abgeben,
        // überspringen, reaktivieren, überarbeiten) und zusätzlich abgegebene
        // Lektionen akzeptieren oder ablehnen. Nach jeder Aktion wird zurück auf die
        // Lehrplan-Ansicht genau dieses Trainees geleitet (nicht auf die Trainee-eigene
        // LehrplanUebersicht wie im LessonController).


        // POST /LessonPlan/StartLesson – OPEN → STARTED
        [HttpPost]
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult StartLesson(int progressId)
        {
            var progress = _progressRepository.GetById(progressId);
            if (progress == null) return NotFound();
            if (!Validate(progress)) return Forbid();

            if (progress.Status == Status.OPEN)
            {
                progress.Status = Status.STARTED;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return BackToTraineePlan(progress);
        }

        // POST /LessonPlan/StopLesson – STARTED → OPEN
        [HttpPost]
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult StopLesson(int progressId)
        {
            var progress = _progressRepository.GetById(progressId);
            if (progress == null) return NotFound();
            if (!Validate(progress)) return Forbid();

            if (progress.Status == Status.STARTED)
            {
                progress.Status = Status.OPEN;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return BackToTraineePlan(progress);
        }

        // POST /LessonPlan/SubmitLesson – STARTED → FINISHED
        [HttpPost]
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult SubmitLesson(int progressId)
        {
            var progress = _progressRepository.GetById(progressId);
            if (progress == null) return NotFound();
            if (!Validate(progress)) return Forbid();

            if (progress.Status == Status.STARTED)
            {
                progress.Status = Status.FINISHED;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return BackToTraineePlan(progress);
        }

        // POST /LessonPlan/ResubmitLesson – REJECTED → FINISHED (Überarbeitung abgeben)
        [HttpPost]
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult ResubmitLesson(int progressId)
        {
            var progress = _progressRepository.GetById(progressId);
            if (progress == null) return NotFound();
            if (!Validate(progress)) return Forbid();

            if (progress.Status == Status.REJECTED)
            {
                progress.Status = Status.FINISHED;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return BackToTraineePlan(progress);
        }

        // POST /LessonPlan/SkipLesson – OPEN → SKIPPED (überspringen)
        [HttpPost]
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult SkipLesson(int progressId)
        {
            var progress = _progressRepository.GetById(progressId);
            if (progress == null) return NotFound();
            if (!Validate(progress)) return Forbid();

            if (progress.Status == Status.OPEN)
            {
                progress.Status = Status.SKIPPED;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return BackToTraineePlan(progress);
        }

        // POST /LessonPlan/UnskipLesson – SKIPPED → OPEN (wieder aktivieren)
        [HttpPost]
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult UnskipLesson(int progressId)
        {
            var progress = _progressRepository.GetById(progressId);
            if (progress == null) return NotFound();
            if (!Validate(progress)) return Forbid();

            if (progress.Status == Status.SKIPPED)
            {
                progress.Status = Status.OPEN;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return BackToTraineePlan(progress);
        }
        
        // POST /LessonPlan/UnacceptLesson – ACCEPTED → FINISHED (Akzeptanz zurücknehmen)
        [HttpPost]
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult UnacceptLesson(int progressId)
        {
            var progress = _progressRepository.GetById(progressId);
            if (progress == null) return NotFound();
            if (!Validate(progress)) return Forbid();

            if (progress.Status == Status.ACCEPTED)
            {
                progress.Status = Status.FINISHED;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return BackToTraineePlan(progress);
        }
        
        // POST /LessonPlan/UnfinishLesson – FINISHED → STARTED (Mentor-Pendant zu LessonController.UnfinishLesson)
        [HttpPost]
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult UnfinishLesson(int progressId)
        {
            var progress = _progressRepository.GetById(progressId);
            if (progress == null) return NotFound();
            if (!Validate(progress)) return Forbid();

            if (progress.Status == Status.FINISHED)
            {
                progress.Status = Status.STARTED;
                progress.StatusChangedAt = DateTime.Now;
                _progressRepository.Update(progress);
            }
            return BackToTraineePlan(progress);
        }

        // Nach einer Mentor-Aktion zurück zur Lehrplan-Ansicht desselben Trainees.
        // traineeId wird aus dem geladenen Fortschrittseintrag abgeleitet
        // (GetById lädt die Trainee-Navigation mit).
        private IActionResult BackToTraineePlan(TraineeLessonProgress progress)
            => RedirectToAction(nameof(TraineeLessonPlan), new { traineeId = progress.Trainee.Id });

        // ── REIHENFOLGE INNERHALB EINER STATUSGRUPPE ANPASSEN ────────────────────
        // Der Mentor kann, während er in einem bestimmten Trainee eingeloggt ist, die
        // Reihenfolge der Lektionen individuell anpassen. Die Anzeige ist primär nach
        // Status sortiert (REJECTED oben usw.); innerhalb desselben Status entscheidet
        // das Feld Order. Per Auf/Ab-Pfeil tauscht der Mentor eine Lektion mit ihrem
        // direkten Vorgänger/Nachfolger desselben Status. Über die Statusgrenze hinaus
        // wird nicht verschoben.

        // POST /LessonPlan/MoveLessonUp – mit dem Vorgänger desselben Status tauschen
        [HttpPost]
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult MoveLessonUp(int progressId) => MoveLesson(progressId, -1);

        // POST /LessonPlan/MoveLessonDown – mit dem Nachfolger desselben Status tauschen
        [HttpPost]
        [Authorize(Roles = "Mentor,Admin")]
        public IActionResult MoveLessonDown(int progressId) => MoveLesson(progressId, +1);

        // Tauscht den Order-Wert eines Fortschrittseintrags mit dem des Nachbarn innerhalb
        // seiner Statusgruppe. direction: -1 = nach oben, +1 = nach unten.
        private IActionResult MoveLesson(int progressId, int direction)
        {
            var target = _progressRepository.GetById(progressId);
            if (target == null) return NotFound();

            // Statusgruppe = alle Lektionen desselben Trainees mit demselben Status,
            // in der aktuell angezeigten Reihenfolge (nach Order).
            var group = _progressRepository.GetAll()
                .Where(p => p.Trainee.Id == target.Trainee.Id && p.Status == target.Status)
                .OrderBy(p => p.Order)
                .ToList();

            var index = group.FindIndex(p => p.Id == target.Id);
            var swapIndex = index + direction;

            // Bereits ganz oben/unten in der Gruppe → nichts zu tun.
            if (index < 0 || swapIndex < 0 || swapIndex >= group.Count)
                return BackToTraineePlan(target);

            // Order-Werte der beiden Nachbarn tauschen.
            var neighbor = group[swapIndex];
            (target.Order, neighbor.Order) = (neighbor.Order, target.Order);
            _progressRepository.Update(target);
            _progressRepository.Update(neighbor);

            return BackToTraineePlan(target);
        }
        // Prüft, ob der eingeloggte Nutzer diesen Fortschrittseintrag bearbeiten darf:
        // Trainees nur ihre eigenen Einträge, Mentoren nur die ihrer zugewiesenen Trainees,
        // Admins immer. progress.Trainee.MentorList wird von GetById mitgeladen, daher genügt
        // ein direkter Vergleich – kein zusätzlicher Repository-Zugriff nötig.
        private bool Validate(TraineeLessonProgress? progress)
        {
            if (progress?.Trainee == null)
                return false;

            var email = User.FindFirstValue(ClaimTypes.Email);

            if (User.IsInRole("Trainee"))
                return progress.Trainee.EmailAddress == email;

            if (User.IsInRole("Admin"))
                return true;

            if (User.IsInRole("Mentor"))
                return progress.Trainee.MentorList.Any(m => m.EmailAddress == email);

            return false;
        }
    }
}
