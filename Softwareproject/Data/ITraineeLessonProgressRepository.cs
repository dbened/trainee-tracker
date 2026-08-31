using Softwareproject.Models;
using System.Collections.Generic;

namespace Softwareproject.Data
{
    public interface ITraineeLessonProgressRepository
    {
        TraineeLessonProgress? GetById(int id);
        List<TraineeLessonProgress> GetAll();

        void Create(TraineeLessonProgress progress);
        void Update(TraineeLessonProgress progress);
        void Delete(int id);

        bool Exists(int id);
    }
}