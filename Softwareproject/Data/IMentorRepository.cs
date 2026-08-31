using Softwareproject.Models;
using System.Collections.Generic;

namespace Softwareproject.Data
{
    public interface IMentorRepository
    {
        Mentor? GetById(int id);
        Mentor? GetByEmail(String Email);
        List<Mentor> GetAll();

        void Create(Mentor mentor);
        void Update(Mentor mentor);
        void Delete(int id);

        bool Exists(int id);
    }
}