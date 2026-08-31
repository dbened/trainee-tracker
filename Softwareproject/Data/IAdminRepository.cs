using Softwareproject.Models;
using System.Collections.Generic;

namespace Softwareproject.Data
{
    public interface IAdminRepository
    {
        Admin? GetById(int id);
        Admin? GetByEmail(String Email);
        List<Admin> GetAll();

        void Create(Admin admin);
        void Update(Admin admin);
        void Delete(int id);

        bool Exists(int id);
    }
}