using Microsoft.EntityFrameworkCore;
using Softwareproject.Models;

namespace Softwareproject.Data
{
    //zentraler Datenbankzugriff für die gesamte Anwendung.
    // Jedes DbSet<T> repräsentiert eine Tabelle in der SQLite-Datenbank (AdminDatabase.db).
    public class Context : DbContext
    {
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Mentor> Mentors { get; set; }
        public DbSet<Trainee> Trainees { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<LessonPlan> LessonPlans { get; set; }
        public DbSet<TraineeLessonProgress> TraineeLessonProgresses { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Break> Breaks { get; set; }
        public DbSet<Statistic> Statistics { get; set; }

        // Steuert ob die SQLite-Datei oder eine In-Memory-Datenbank verwendet wird.
        // In-Memory wird z.B. für Tests genutzt (kein persistenter Zustand).
        public Boolean useInMemory { get; set; }

        public Context()
        {
            useInMemory = false;
        }

        // Konstruktor für Tests: useInMemory = true → keine Datei, Daten nur im RAM
        public Context(bool useInMemory)
        {
            this.useInMemory = useInMemory;
        }

        // Beziehungen konfigurieren
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1:1-Beziehung LessonPlan ↔ Trainee
            // Der Foreign Key "TraineeId" liegt als Shadow Property auf dem LessonPlan-Table
            // Navigation ist nur von Trainee-Seite möglich: trainee.LessonPlan
            modelBuilder.Entity<Trainee>()
                .HasOne(t => t.LessonPlan)
                .WithMany()
                .IsRequired(false);

            modelBuilder.Entity<Trainee>()
                .HasMany(t => t.MentorList)
                .WithMany();
        }

        // Datenbankverbindung konfigurieren
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (useInMemory)
            {
                // In-Memory-DB für Tests – Daten gehen beim Neustart verloren
                optionsBuilder.UseInMemoryDatabase("LessonTestDb");
            }
            else
            {
                // SQLite-Datei im Projektverzeichnis
                optionsBuilder.UseSqlite("Data Source=AdminDatabase.db");
            }
        }
    }
}
