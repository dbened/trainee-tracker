using System.Text.Json.Serialization;

namespace Softwareproject.Models
{
    public class LessonPlan
    {
        public int Id { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("isDeprecated")]
        public bool IsDeprecated { get; set; }

        [JsonPropertyName("lessonList")]
        public ICollection<Lesson> lessonList { get; set; } = new List<Lesson>();
    }
}