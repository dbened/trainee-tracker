using Softwareproject.Data;
using Softwareproject.Models;

namespace SoftwareProject.UnitTests.FakeRepositories;

public class FakeAdminRepository : IAdminRepository
{
    private readonly List<Admin> _admins = new();

    public List<Admin> GetAll() => _admins;

    public Admin? GetById(int id)
        => _admins.FirstOrDefault(a => a.Id == id);

    public Admin? GetByEmail(string email)
        => _admins.FirstOrDefault(a => a.EmailAddress == email);

    public void Create(Admin admin)
    {
        _admins.Add(admin);
    }

    public void Update(Admin admin)
    {
        // Objects are stored by reference, so nothing is needed.
    }

    public void Delete(int id)
    {
        var admin = GetById(id);
        if (admin != null)
            _admins.Remove(admin);
    }

    public bool Exists(int id)
        => _admins.Any(a => a.Id == id);
}
