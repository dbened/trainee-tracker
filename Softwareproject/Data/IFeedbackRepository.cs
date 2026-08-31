using Softwareproject.Models;
using System.Collections.Generic;

namespace Softwareproject.Data
{
    public interface IFeedbackRepository
    {
        Feedback? GetById(int id);
        List<Feedback> GetAll();

        void Create(Feedback feedback);
        void Update(Feedback feedback);
        void Delete(int id);

        bool Exists(int id);
    }
}