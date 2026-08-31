namespace Softwareproject.Models
{
    // ViewModel für die Anträge-Inbox des Mentors.
    // Bündelt die beiden Request-Arten, die ein Mentor an einer Stelle bearbeiten kann:
    //   - offene Break-Anträge seiner Trainees (Accepted == false)
    //   - abgegebene Lessons seiner Trainees (Status == FINISHED), die auf Review warten
    // Die ausführliche Bearbeitung pro Lehrplan bleibt zusätzlich in der
    // LehrplanUebersichtVerwaltung erhalten (zwei Wege auf dieselbe Logik).
    public class RequestInboxViewModel
    {
        public List<Break> OpenBreaks { get; set; } = new();
        public List<TraineeLessonProgress> OpenLessons { get; set; } = new();
    }
}