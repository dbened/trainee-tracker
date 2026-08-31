using System.ComponentModel.DataAnnotations;

namespace Softwareproject.Models
{

    public class Trainee : User
    {
        [Required]
        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public ICollection<Mentor> MentorList { get; set; }
            = new List<Mentor>();
        public LessonPlan? LessonPlan { get; set; }
    }
}