using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using VacationPlannerWeb.Models;

namespace VacationPlannerWeb.DataAccess
{
    public class DbInitializer
    {
        private readonly AppDbContext _context;
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;

        public DbInitializer(IServiceProvider services)
        {
            _context = services.GetRequiredService<AppDbContext>();
            _roleManager = services.GetRequiredService<RoleManager<Role>>();
            _userManager = services.GetRequiredService<UserManager<User>>();
        }

        public void Initialize()
        {
            _context.Database.EnsureCreated();

            if (_context.Users.Any())
            {
                return;
            }

            ClearDatabase();
            CreateAdminRole();
            SeedDatabase();
        }

        private void CreateAdminRole()
        {
            bool roleExists = _roleManager.RoleExistsAsync("Admin").Result;
            if (roleExists)
            {
                return;
            }

            var adminRole = new Role()
            {
                Name = "Admin",
                Shortening = "Admin"
            };

            _roleManager.CreateAsync(adminRole).Wait();

            var user = new User()
            {
                FirstName = "Admin",
                LastName = "Admin",
                DisplayName = "Admin",
                UserName = "admin@gmail.com",
                Email = "admin@gmail.com"
            };

            string adminPassword = "Password123";
            var userResult = _userManager.CreateAsync(user, adminPassword).Result;

            if (userResult.Succeeded)
            {
                _userManager.AddToRoleAsync(user, "Admin").Wait();
            }
        }

        private void SeedDatabase()
        {
            var team1 = new Team { Name = "Pandas", Shortening = "Pand" };
            var team2 = new Team { Name = "Tigers", Shortening = "Tigr" };
            var team3 = new Team { Name = "Rabbits", Shortening = "Rabt" };

            var teams = new List<Team>()
            {
                team1, team2, team3
            };

            _context.Teams.AddRange(teams);

            var dep1 = new Department { Name = "Developer", Shortening = "Dev" };
            var dep2 = new Department { Name = "Economy", Shortening = "Eco" };
            var dep3 = new Department { Name = "Support", Shortening = "Sup" };

            var deps = new List<Department>()
            {
                dep1, dep2, dep3
            };

            _context.Departments.AddRange(deps);

            bool roleExists = _roleManager.RoleExistsAsync("Manager").Result;
            if (!roleExists)
            {
                var managerRole = new Role()
                {
                    Name = "Manager",
                    Shortening = "Mangr"
                };
                _roleManager.CreateAsync(managerRole).Wait();
            }

            var user1 = new User { UserName = "user1@gmail.com", Email = "user1@gmail.com", FirstName = "Pelle", LastName = "Svantesson", DisplayName = "Pelle Svantesson", TeamId = team1.Id, DepartmentId = dep3.Id };
            var user2 = new User { UserName = "user2@gmail.com", Email = "user2@gmail.com", FirstName = "Thom", LastName = "Ivarsson", DisplayName = "Thom Ivarsson", TeamId = team2.Id, DepartmentId = dep2.Id };
            var user3 = new User { UserName = "user3@gmail.com", Email = "user3@gmail.com", FirstName = "Britta", LastName = "Johnsson", DisplayName = "Britta Johnsson", TeamId = team3.Id, DepartmentId = dep1.Id };
            var user4 = new User { UserName = "user4@gmail.com", Email = "user4@gmail.com", FirstName = "Einar", LastName = "Andersson", DisplayName = "Einar Andersson", TeamId = team1.Id, DepartmentId = dep2.Id };
            var user5 = new User { UserName = "user5@gmail.com", Email = "user5@gmail.com", FirstName = "Sarah", LastName = "Qvistsson", DisplayName = "Sarah Qvistsson", TeamId = team2.Id, DepartmentId = dep3.Id };

            string userPassword = "Password123";

            var users = new List<User>()
            {
                user1, user2, user3, user4, user5
            };

            foreach (var user in users)
            {
                _userManager.CreateAsync(user, userPassword).Wait();
            }

            var abs1 = new AbsenceType { Name = "Vacation" };
            var abs2 = new AbsenceType { Name = "Leave" };
            var abs3 = new AbsenceType { Name = "Away" };

            var abses = new List<AbsenceType>()
            {
                abs1, abs2, abs3
            };

            var vac1 = new VacationBooking { Comment = "Trip to Paris", Approval = ApprovalState.Pending.ToString(), FromDate = DateTime.Now.Date.AddDays(-3), ToDate = DateTime.Now.Date.AddDays(3), User = user1, AbsenceType = abs1 };
            var vac2 = new VacationBooking { Comment = "Away from home", Approval = ApprovalState.Pending.ToString(), FromDate = DateTime.Now.Date.AddDays(10), ToDate = DateTime.Now.Date.AddDays(12), User = user1, AbsenceType = abs3 };
            var vac3 = new VacationBooking { Comment = "Party day", Approval = ApprovalState.Approved.ToString(), FromDate = DateTime.Now.Date.AddDays(15), ToDate = DateTime.Now.Date.AddDays(17), User = user1, AbsenceType = abs1 };
            var vac4 = new VacationBooking { Comment = "Going to Hawaii", Approval = ApprovalState.Pending.ToString(), FromDate = DateTime.Now.Date.AddDays(8), ToDate = DateTime.Now.Date.AddDays(16), User = user2, AbsenceType = abs1 };
            var vac5 = new VacationBooking { Comment = "Tripping", Approval = ApprovalState.Denied.ToString(), FromDate = DateTime.Now.Date.AddDays(2), ToDate = DateTime.Now.Date.AddDays(4), User = user2, AbsenceType = abs2 };
            var vac6 = new VacationBooking { Comment = "Cruise trip", Approval = ApprovalState.Pending.ToString(), FromDate = DateTime.Now.Date.AddDays(11), ToDate = DateTime.Now.Date.AddDays(14), User = user3, AbsenceType = abs1 };
            var vac7 = new VacationBooking { Comment = "Barcelona", Approval = ApprovalState.Approved.ToString(), FromDate = DateTime.Now.Date.AddDays(-1), ToDate = DateTime.Now.Date.AddDays(4), User = user3, AbsenceType = abs1 };
            var vac8 = new VacationBooking { Comment = "Road trip", Approval = ApprovalState.Denied.ToString(), FromDate = DateTime.Now.Date.AddDays(5), ToDate = DateTime.Now.Date.AddDays(20), User = user4, AbsenceType = abs2 };
            var vac9 = new VacationBooking { Comment = "Kentucky", Approval = ApprovalState.Pending.ToString(), FromDate = DateTime.Now.Date.AddDays(20), ToDate = DateTime.Now.Date.AddDays(25), User = user5, AbsenceType = abs3 };
            var vac10 = new VacationBooking { Comment = "Far away", Approval = ApprovalState.Approved.ToString(), FromDate = DateTime.Now.Date.AddDays(-8), ToDate = DateTime.Now.Date.AddDays(-2), User = user5, AbsenceType = abs1 };

            var vacs = new List<VacationBooking>()
            {
                vac1, vac2, vac3, vac4, vac5, vac6, vac7, vac8, vac9, vac10
            };

            foreach (var vac in vacs)
            {
                vac.VacationDays = GetVacationDayFromBookings(vac);
            }

            _context.AbsenceTypes.AddRange(abses);
            _context.VacationBookings.AddRange(vacs);

            _context.SaveChanges();
        }

        public List<VacationDay> GetVacationDayFromBookings(VacationBooking vacationBooking)
        {
            var vacdayList = new List<VacationDay>();
            for (DateTime d = vacationBooking.FromDate; d <= vacationBooking.ToDate; d = d.AddDays(1))
            {
                var vacday = new VacationDay()
                {
                    Id = 0,
                    VacationDate = d,
                    VacationBookingId = vacationBooking.Id,
                };
                if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }
                vacdayList.Add(vacday);
            }
            return vacdayList;
        }

        private void ClearDatabase(bool clearAll = false)
        {
            var departments = _context.Departments.ToList();
            _context.Departments.RemoveRange(departments);

            var teams = _context.Teams.ToList();
            _context.Teams.RemoveRange(teams);

            var absenceTypes = _context.AbsenceTypes.ToList();
            _context.AbsenceTypes.RemoveRange(absenceTypes);

            var vacationDays = _context.VacationDays.ToList();
            _context.VacationDays.RemoveRange(vacationDays);

            var vacationBookings = _context.VacationBookings.ToList();
            _context.VacationBookings.RemoveRange(vacationBookings);

            if (clearAll)
            {
                var userRolesRemove = _context.UserRoles.ToList();
                _context.UserRoles.RemoveRange(userRolesRemove);

                var roles = _context.Roles.ToList();
                _context.Roles.RemoveRange(roles);

                var usersRemove = _context.Users.ToList();
                _context.Users.RemoveRange(usersRemove);
            }
            else
            {
                var users = _context.Users.ToList();
                var userRoles = _context.UserRoles.ToList();

                foreach (var user in users)
                {
                    if (!userRoles.Any(r => r.UserId == user.Id))
                    {
                        _context.Users.Remove(user);
                    }
                }
            }

            _context.SaveChanges();
        }
    }
}
