using Softwareproject.Models;
using System.Collections.Generic;

namespace Softwareproject.Data
{
    public interface ITraineeRepository
    {
        Trainee? GetById(int id);
        List<Trainee>? GetByMentorId(int mentorId);
        Trainee? GetByIdWithLessonPlan(int id);
        Trainee? GetByIdWithMentors(int id);
        Trainee? GetByEmailWithMentors(string email);
        Trainee? GetByEmail(String Email);
        List<Trainee> GetAll();

        void Create(Trainee trainee);
        void Update(Trainee trainee);
        void Delete(int id);

        bool Exists(int id);
    }
}