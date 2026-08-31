using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Softwareproject.Models;
using Softwareproject.Data;

namespace Softwareproject.Services;

public class WeekPlanService : IWeekPlanService
{
    private readonly IBreakRepository _breakRepository;
    private readonly ITraineeLessonProgressRepository _progressRepository;
    private readonly HttpClient _httpClient;

    public WeekPlanService(IBreakRepository breakRepository, ITraineeLessonProgressRepository progressRepository, HttpClient httpClient)
    {
        _breakRepository = breakRepository;
        _progressRepository = progressRepository;
        _httpClient = httpClient;
    }

    public async Task<WeekPlanViewModel> GenerateWeekPlanAsync(Trainee trainee, int offset)
    {
        // Basisdatum anhand des Offsets berechnen und bei Wochenende auf Montag verschieben
        var targetDate = DateTime.Today.AddDays(offset * 7);
        if (targetDate.DayOfWeek == DayOfWeek.Saturday)
        {
            targetDate = targetDate.AddDays(2);
        }
        else if (targetDate.DayOfWeek == DayOfWeek.Sunday)
        {
            targetDate = targetDate.AddDays(1); 
        }
        
        // Montag dieser Zielwoche berechnen
        int diff = (7 + (targetDate.DayOfWeek - DayOfWeek.Monday)) % 7;
        var startOfWeek = targetDate.AddDays(-1 * diff);
        var endOfWeek = startOfWeek.AddDays(4);

        var vm = new WeekPlanViewModel
        {
            Offset = offset,
            StartOfWeek = startOfWeek,
            TraineeStartDate = trainee.StartDate.ToDateTime(TimeOnly.MinValue),
            TraineeEndDate = trainee.EndDate.ToDateTime(TimeOnly.MinValue)
        };

        // 1. Alle Fortschritte laden und nach definierter oder individualsierter Reihenfolge sortieren
        var progresses = _progressRepository.GetAll()
            .Where(p => p.Trainee != null && p.Trainee.Id == trainee.Id)
            .OrderBy(p => StatusSortOrder(p.Status)) 
            .ThenBy(p => p.Order)
            .ToList();

        // 2. Alle genehmigten Pausen laden
        var breaks = _breakRepository.GetAll()
            .Where(b => b.Trainee != null && b.Trainee.Id == trainee.Id && b.Accepted)
            .ToList();

        // 3. Arbeitszeiten aus der lokalen Demo-Konfiguration ermitteln (alternativ via API bei Firmen)
        var startApiDate = startOfWeek < DateTime.Today ? startOfWeek : DateTime.Today;
        var workingHours = GetDemoWorkingHours(startApiDate, endOfWeek);

        // falls Arbeitszeiten via API ermittelt werden und die Verbindung fehlschlägt, Standardwerte setzen
        /* bool isApiOffline = workingHours.Count == 0;

        if (isApiOffline)
        {
            // Wörterbuch mit Standardwerten füllen (8 Stunden für Mo-Fr)
            for (DateTime d = startApiDate; d <= endOfWeek; d = d.AddDays(1))
            {
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingHours[DateOnly.FromDateTime(d)] = 8.0;
                }
            }
            
            // der View Bescheid geben, um eine Warnung anzuzeigen
            vm.IsApiError = true;
        } */

        // 4. Projektion berechnen (ab Heute in die Zukunft)
        var openProgresses = progresses
            .Where(p => p.Status == Status.OPEN || p.Status == Status.STARTED || p.Status == Status.REJECTED)
            .ToList();
        
        var projectionQueue = new Queue<TraineeLessonProgress>(openProgresses);
        var currentLesson = projectionQueue.Count > 0 ? projectionQueue.Peek() : null;
        double remainingEffort = currentLesson != null ? CalculateEffort(currentLesson) : 0;

        var projectedDays = new Dictionary<DateTime, List<TraineeLessonProgress>>();

        // Wir simulieren die Tage ab heute, um zu wissen, welche Lektion in welche zukünftige Woche fällt
        for (DateTime date = DateTime.Today; date <= endOfWeek; date = date.AddDays(1))
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;

            double hoursToday = workingHours.ContainsKey(DateOnly.FromDateTime(date)) ? workingHours[DateOnly.FromDateTime(date)] : 0;   

            bool hasBreak = breaks.Any(b => DateOnly.FromDateTime(date) >= b.StartingDate && DateOnly.FromDateTime(date) <= b.EndDate);

            if (hasBreak)
            {
                hoursToday = 0;
            }

            double daysCapacity = hoursToday / 8.0; // 8 Stunden = 1 Personentag

            var lessonsToday = new List<TraineeLessonProgress>();

            while (daysCapacity > 0 && currentLesson != null)
            {
                if (!lessonsToday.Contains(currentLesson)) lessonsToday.Add(currentLesson);

                if (remainingEffort <= daysCapacity)
                {
                    daysCapacity -= remainingEffort;
                    projectionQueue.Dequeue();
                    currentLesson = projectionQueue.Count > 0 ? projectionQueue.Peek() : null;
                    remainingEffort = currentLesson != null ? CalculateEffort(currentLesson) : 0;
                }
                else
                {
                    remainingEffort -= daysCapacity;
                    daysCapacity = 0;
                }
            }
            projectedDays[date] = lessonsToday;
        }

        // 5. WOCHENPLAN ZUSAMMENBAUEN (Montag bis Freitag)
        var dayNames = new[] { "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag" };
        
        for (int i = 0; i < 5; i++)
        {
            var currentDay = startOfWeek.AddDays(i);
            double hours = workingHours.ContainsKey(DateOnly.FromDateTime(currentDay)) ? workingHours[DateOnly.FromDateTime(currentDay)] : 0;

            bool hasBreak = breaks.Any(b => DateOnly.FromDateTime(currentDay) >= b.StartingDate && DateOnly.FromDateTime(currentDay) <= b.EndDate);
            
            var dayPlan = new DayPlan
            {
                Date = currentDay,
                DayName = dayNames[i],
                IsDayOff = hours == 0 || hasBreak // Abwesend, wenn API 0 sagt oder Pause eingetragen ist
            };

            // Historie: Lektionen, die an diesem Tag einen Statuswechsel hatten
            if (currentDay < DateTime.Today)
            {
                dayPlan.Lessons = progresses
                    .Where(p => p.StatusChangedAt.HasValue && p.StatusChangedAt.Value.Date == currentDay.Date && p.Status != Status.SKIPPED)
                    .ToList();
            } 
            // Projektion: Lektionen, die an diesem Tag vorgesehen sind
            else
            {
                if (projectedDays.ContainsKey(currentDay))
                {
                    dayPlan.Lessons = projectedDays[currentDay];
                }
            }

            vm.Days.Add(dayPlan);
        }

        return vm;
    }

    private double CalculateEffort(TraineeLessonProgress progress) 
    {
        if (progress.Lesson == null) return 0;
        double effort = progress.Lesson.EstimatedDays ?? 0;
        
        // Restaufwand der Lektion für die Zukunfts-Projektion berechnen
        switch (progress.Status)
        {
            case Status.OPEN:
            case Status.STARTED:
                return effort; 
                
            case Status.REJECTED:
                return effort * (1 - 0.8); 
                
            default:
                return 0; 
        }
    }

    // Erzeugt lokale Demo-Arbeitszeiten, damit die Portfolio-Version ohne
    // externe Services, Zugangsdaten oder Firmeninfrastruktur lauffähig bleibt.
    private static Dictionary<DateOnly, double> GetDemoWorkingHours(DateTime start, DateTime end)
    {
        var workingHours = new Dictionary<DateOnly, double>();

        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            workingHours[DateOnly.FromDateTime(date)] = 8.0;
        }

        return workingHours;
    }

    // Hilfsfunktion: liefert die Sortierprioritätszahl für einen Status.
    // Kleinere Zahl = weiter oben in der Liste.
    // Abgelehnte Lektionen stehen ganz oben, damit der Trainee sie sofort sieht.
    private static int StatusSortOrder(Status status) => status switch
    {
        Status.REJECTED => 1,  // ganz oben – muss überarbeitet werden
        Status.ACCEPTED => 2,  // akzeptiert – wartet auf Feedback/Bewertung
        Status.FINISHED => 3,  // abgegeben – wartet auf Review des Mentors
        Status.STARTED => 4,   // in Bearbeitung
        Status.OPEN => 5,      // noch nicht begonnen
        Status.SKIPPED => 6,   // übersprungen
        Status.RATED => 7,     // abgeschlossen und bewertet
        _ => 8
    };
}
// Benedikt Dippner