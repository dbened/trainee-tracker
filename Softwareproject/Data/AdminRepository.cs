using Softwareproject.Models;
using System.Collections.Generic;
using System.Linq;

namespace Softwareproject.Data
{
    public class AdminRepository : IAdminRepository
    {
        private readonly Context _context;

        public AdminRepository(Context context)
        {
            _context = context;
        }

        public Admin? GetById(int id)
            => _context.Admins.FirstOrDefault(a => a.Id == id);

        public Admin? GetByEmail(string email)
            => _context.Admins.FirstOrDefault(a => a.EmailAddress.ToLower() == email.ToLower());

        public List<Admin> GetAll()
            => _context.Admins.ToList();

        public bool Exists(int id)
            => _context.Admins.Any(a => a.Id == id);

        public bool ExistsByEmail(string email)
            => _context.Admins.Any(a => a.EmailAddress == email);

        public void Create(Admin admin)
        {
            _context.Admins.Add(admin);
            _context.SaveChanges();
        }

        public void Update(Admin admin)
        {
            _context.Admins.Update(admin);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var admin = _context.Admins.Find(id);
            if (admin != null)
            {
                _context.Admins.Remove(admin);
                _context.SaveChanges();
            }
        }
    }
}