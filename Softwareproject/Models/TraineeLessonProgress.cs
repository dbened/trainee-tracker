namespace Softwareproject.Models;

// Verbindungstabelle zwischen Trainee und Lesson.
// Speichert den aktuellen Bearbeitungsstatus einer Lektion für einen bestimmten Trainee.
// Wird beim Import automatisch für jede neue Lektion mit Status OPEN angelegt,
// sobald dem Lehrplan ein Trainee zugewiesen ist.
public class TraineeLessonProgress
{
    public int Id { get; set; }

    // Ablehnungsgrund – nur gesetzt wenn Status == REJECTED.
    // Nullable (String?), da alle anderen Status keinen Ablehnungsgrund haben.
    // Wäre das Feld NOT NULL, würden Einträge ohne Ablehnungsgrund die DB-Constraint verletzen.
    public String? RefusalReason { get; set; }

    // Aktueller Bearbeitungsstatus der Lektion (OPEN, STARTED, FINISHED, ACCEPTED, REJECTED, SKIPPED, RATED)
    public Status Status { get; set; }

    // Navigation zum Trainee – EF verwaltet den Shadow-FK "TraineeId" in der Tabelle
    public Trainee Trainee { get; set; }

    // Navigation zur Lektion – EF verwaltet den Shadow-FK "LessonId" in der Tabelle
    public Lesson Lesson { get; set; }

    // Zeitpunkt der letzten Statusänderung
    public DateTime? StatusChangedAt { get; set; }

    public int Order { get; set; }
}
