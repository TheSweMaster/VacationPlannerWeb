using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VacationPlannerWeb.Models;

namespace VacationPlannerWeb.DataAccess
{
    public class AppDbContext : IdentityDbContext<User, Role, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }

        public DbSet<VacationBooking> VacationBookings { get; set; }
        public DbSet<VacationDay> VacationDays { get; set; }
        public DbSet<WorkFreeDay> WorkFreeDays { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<AbsenceType> AbsenceTypes { get; set; }
    }
}
