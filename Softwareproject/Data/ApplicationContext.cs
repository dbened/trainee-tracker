using Microsoft.EntityFrameworkCore;
using Softwareproject.Models;
using Softwareproject.Data;

namespace Softwareproject.Data
{
    public class ApplicationContext(DbContextOptions<ApplicationContext> options) : DbContext(options)
    {
        public DbSet<Lesson> Lessons { get; set; }
    }
}