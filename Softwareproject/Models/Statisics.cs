namespace Softwareproject.Models
{
    public class Statistic
    {
        public int Id { get; set; }

        public Trainee? Trainee { get; set; }

        public DateTime Date { get; set; }

        // Personentage
        public double PresentDays { get; set; }

        public double CompletedDays { get; set; }

        public double RemainingDays { get; set; }

        public double BufferDays { get; set; }

        public double ForecastBufferDays { get; set; }

        // Prozent
        public double Speed { get; set; }

        // Falls die API nicht erreichbar war
        public bool WorkingHoursAvailable { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}