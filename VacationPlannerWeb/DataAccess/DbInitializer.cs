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
                Name = "Admin"
            };

            _roleManager.CreateAsync(adminRole).Wait();

            var user = new User()
            {
                UserName = "admin",
                Email = "admin@default.com"
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
            //TODO: Add Vacation bookings data

            //var cat1 = new Categories { Name = "Standard", Description = "The Bakery's Standard pizzas all year around." };
            //var cat2 = new Categories { Name = "Spcialities", Description = "The Bakery's Speciality pizzas only for a limited time." };
            //var cat3 = new Categories { Name = "News", Description = "The Bakery's New pizzas on the menu." };

            //var cats = new List<Categories>()
            //{
            //    cat1, cat2, cat3
            //};

            //var piz1 = new Pizzas { Name = "Capricciosa", Price = 70.00M, Category = cat1 };
            //var piz2 = new Pizzas { Name = "Veggie", Price = 70.00M, Category = cat3 };
            //var piz3 = new Pizzas { Name = "Hawaii", Price = 75.00M, Category = cat1 };
            //var piz4 = new Pizzas { Name = "Margarita", Price = 65.00M, Category = cat1 };
            //var piz5 = new Pizzas { Name = "Kebab Special", Price = 85.00M, Category = cat2 };
            //var piz6 = new Pizzas { Name = "Pescatore", Price = 80.00M, Category = cat1 };
            //var piz7 = new Pizzas { Name = "Barcelona", Price = 70.00M, Category = cat1 };
            //var piz8 = new Pizzas { Name = "Flying Jacob", Price = 89.00M, Category = cat2 };
            //var piz9 = new Pizzas { Name = "Kentucky", Price = 69.00M, Category = cat3 };
            //var piz10 = new Pizzas { Name = "La Carne", Price = 75.00M, Category = cat1 };

            //var pizs = new List<Pizzas>()
            //{
            //    piz1, piz2, piz3, piz4, piz5, piz6, piz7, piz8, piz9, piz10
            //};

            bool roleExists = _roleManager.RoleExistsAsync("Manager").Result;
            if (!roleExists)
            {
                var managerRole = new Role()
                {
                    Name = "Manager"
                };
                _roleManager.CreateAsync(managerRole).Wait();
            }

            var user1 = new User { UserName = "user1@gmail.com", Email = "user1@gmail.com" };
            var user2 = new User { UserName = "user2@gmail.com", Email = "user2@gmail.com" };
            var user3 = new User { UserName = "user3@gmail.com", Email = "user3@gmail.com" };
            var user4 = new User { UserName = "user4@gmail.com", Email = "user4@gmail.com" };
            var user5 = new User { UserName = "user5@gmail.com", Email = "user5@gmail.com" };

            string userPassword = "Password123";

            var users = new List<User>()
            {
                user1, user2, user3, user4, user5
            };

            foreach (var user in users)
            {
                _userManager.CreateAsync(user, userPassword).Wait();
            }

            //_context.Categories.AddRange(cats);
            //_context.Pizzas.AddRange(pizs);

            _context.SaveChanges();
        }

        private void ClearDatabase()
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

            //var pizzas = _context.Pizzas.ToList();
            //_context.Pizzas.RemoveRange(pizzas);

            //var categories = _context.Categories.ToList();
            //_context.Categories.RemoveRange(categories);

            _context.SaveChanges();
        }
    }
}
