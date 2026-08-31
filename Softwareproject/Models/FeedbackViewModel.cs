using System.ComponentModel.DataAnnotations;

namespace Softwareproject.Models
{
    public class FeedbackViewModel
    {
        [Range(0, 5)]
        public int Difficulty { get; set; }
        public String PriorKnowledge { get; set; } = string.Empty;
        public String Effort { get; set; } = string.Empty;
        public String Comment { get; set; } = string.Empty;
        public Trainee? Trainee { get; set; }
        public Lesson? Lesson { get; set; }
        public DateOnly SubmitDate { get; set; }
    }
}