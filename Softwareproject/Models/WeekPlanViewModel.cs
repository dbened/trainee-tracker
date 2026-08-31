namespace Softwareproject.Models;

public class WeekPlanViewModel
{
    public int Offset { get; set; }
    public DateTime StartOfWeek { get; set; }
    public DateTime TraineeStartDate { get; set; }
    public DateTime TraineeEndDate { get; set; }
    public bool IsApiError { get; set; } = false;
    
    // Enthält exakt 5 Einträge (Montag bis Freitag)
    public List<DayPlan> Days { get; set; } = new List<DayPlan>();
}

public class DayPlan
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = "";
    public bool IsDayOff { get; set; } 
    public List<TraineeLessonProgress> Lessons { get; set; } = new List<TraineeLessonProgress>();
}
