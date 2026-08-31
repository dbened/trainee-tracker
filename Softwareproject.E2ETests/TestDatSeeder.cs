using Softwareproject.Data;
using Softwareproject.Models;

namespace Softwareproject.E2ETests;

public static class TestDataSeeder
{
    public static void CreateAcceptedLesson(Context context)
    {
        var trainee = context.Trainees
            .First(x => x.EmailAddress == "trainee@test.com");

        var lesson = context.Lessons.First();

        var exists = context.TraineeLessonProgresses.Any(x =>
            x.Trainee.Id == trainee.Id &&
            x.Lesson.Id == lesson.Id &&
            x.Status == Status.ACCEPTED);

        if (!exists)
        {
            context.TraineeLessonProgresses.Add(
                new TraineeLessonProgress
                {
                    Trainee = trainee,
                    Lesson = lesson,
                    Status = Status.ACCEPTED
                });

            context.SaveChanges();
        }
    }
}