using Softwareproject.Models;
using System.Collections.Generic;

namespace Softwareproject.Data
{
    public interface IBreakRepository
    {
        Break? GetById(int id);
        List<Break> GetAll();

        void Create(Break breakItem);
        void Update(Break breakItem);
        void Delete(int id);

        bool Exists(int id);
    }
}