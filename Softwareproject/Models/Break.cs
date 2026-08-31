namespace Softwareproject.Models
{
    public class Break
    {
        public int Id { get; set; }
        public bool Accepted { get; set; }
        public String Description { get; set; }
        public DateOnly StartingDate { get; set; }
        public DateOnly EndDate { get; set; }
        public Trainee Trainee { get; set; }
    }
}