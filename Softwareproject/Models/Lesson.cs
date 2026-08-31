using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Softwareproject.Models
{
    public class Lesson
    {
        // [JsonPropertyName] mappt das C#-Property auf das entsprechende JSON-Feld beim Import.
        // Die JSON-Datei liefert z.B. { "id": 34973, "title": "...", "url": "...", ... }

        // [DatabaseGenerated(None)] teilt EF Core mit, dass die ID NICHT von der DB generiert wird.
        // Ohne dieses Attribut würde EF die explizite ID ignorieren und SQLite eine eigene (1, 2, 3 ...)
        // vergeben – das würde Re-Import und Deduplication anhand der originalen Lektion-IDs unmöglich machen.
        [JsonPropertyName("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public required string Title { get; set; }

        // Geschätzter Aufwand in Personentagen (PT), nullable weil manche Lektionen keinen Schätzwert haben
        [JsonPropertyName("estimate")]
        public double? EstimatedDays { get; set; }

        [JsonPropertyName("url")]
        public required string URL { get; set; }

        // true = Lektion ist veraltet/entfernt. Deprecated Lektionen werden ausgeblendet,
        // solange der Trainee sie noch nicht begonnen hat (Status OPEN).
        [JsonPropertyName("deprecated")]
        public bool IsDeprecated { get; set; }

        // Position der Lektion im Lehrplan (0-basiert, bestimmt durch die Reihenfolge im JSON)
        public int SortOrder { get; set; }

        // Feedback-Einträge des Trainees zu dieser Lektion (nach Abschluss)
        public ICollection<Feedback> FeedbackList { get; set; } = new List<Feedback>();

        // Navigation zum übergeordneten Lehrplan.
        public LessonPlan? LessonPlan { get; set; }
    }
}
