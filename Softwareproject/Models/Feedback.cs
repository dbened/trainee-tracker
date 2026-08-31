using System.ComponentModel.DataAnnotations;

namespace Softwareproject.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        [Range(0, 5, ErrorMessage = "Nur Zahlen von 1 bis 5 erlaubt.")]
        public int Difficulty { get; set; } = 0;
        public String? PriorKnowledge { get; set; } = string.Empty;
        public String? Effort { get; set; } = string.Empty;
        public String? Comment { get; set; } = string.Empty;
        public Trainee? Trainee { get; set; }
        public int LessonId { get; set; }
        public DateOnly SubmitDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    }
}