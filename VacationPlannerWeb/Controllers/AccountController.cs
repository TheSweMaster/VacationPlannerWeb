using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using VacationPlannerWeb.ViewModels;
using VacationPlannerWeb.DataAccess;
using VacationPlannerWeb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VacationPlannerWeb.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        public AccountController(AppDbContext context, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(bool showHidden = false)
        {
            var allUsers = await _context.Users.AsNoTracking().Where(x => x.IsHidden == showHidden).ToListAsync();
            var allUsersWithTeamsAndDepartment = new List<User>();
            foreach (var user in allUsers)
            {
                user.Team = await _context.Teams.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.TeamId);
                user.Department = await _context.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.DepartmentId);
                allUsersWithTeamsAndDepartment.Add(user);
            }

            var accountViewModel = new AccountViewModel()
            {
                Users = allUsersWithTeamsAndDepartment,
                ShowHidden = !showHidden,
            };

            return View(accountViewModel);
        }

        [Authorize]
        public async Task<IActionResult> ListAll()
        {
            var allUsers = await _context.Users.AsNoTracking().Where(x => x.IsHidden == false).ToListAsync();
            var allUsersWithTeamsAndDepartment = new List<User>();
            foreach (var user in allUsers)
            {
                user.Team = await _context.Teams.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.TeamId);
                user.Department = await _context.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.DepartmentId);
                allUsersWithTeamsAndDepartment.Add(user);
            }

            var accountViewModel = new AccountViewModel()
            {
                Users = allUsersWithTeamsAndDepartment,
            };

            return View(accountViewModel);
        }

        private async Task<Dictionary<string, List<User>>> GetUserRoleDictionary()
        {
            var userRoleDictionary = new Dictionary<string, List<User>>();

            var rolesList = await _context.Roles.ToListAsync();
            foreach (var role in rolesList)
            {
                var users = await _userManager.GetUsersInRoleAsync(role.Name);
                var res = userRoleDictionary.TryAdd(role.Name, users.ToList());
                if (!res)
                {
                    userRoleDictionary[role.Name].AddRange(users);
                }
            }

            return userRoleDictionary;
        }

        [Authorize]
        public async Task<IActionResult> UserProfile()
        {
            var user = await GetCurrentUser();
            if (user == null)
            {
                return NotFound($"The user was not found in the database.");
            }

            var startDate = new DateTime(DateTime.Now.Year, 4, 1);
            var endDate = new DateTime(DateTime.Now.Year, 3, 31);

            if (DateTime.Now.Date >= startDate)
            {
                endDate = endDate.AddYears(1);
            }
            else
            {
                startDate = startDate.AddYears(-1);
            }

            var vacdays = await _context.VacationDays.AsNoTracking()
                .Where(v => v.VacationBooking.UserId == user.Id && v.VacationBooking.Approval != ApprovalState.Denied.ToString())
                .Select(v => v.VacationDate)
                .Where(x => x.Date >= startDate && x.Date <= endDate).ToListAsync();

            user.Team = await _context.Teams.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.TeamId);
            user.Department = await _context.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.DepartmentId);

            ViewData["VacationDaysCount"] = vacdays.Count;
            ViewData["VacationDaysCountPeriod"] = $"{startDate.ToString("yyyy-MM-dd")} - {endDate.ToString("yyyy-MM-dd")}";
            return View(user);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            ViewData["TeamId"] = new SelectList(await GetTeamsDisplayList(), "Id", "Name", user.TeamId);
            ViewData["DepartmentId"] = new SelectList(await GetDepartmentDisplayList(), "Id", "Name", user.DepartmentId);
            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, [Bind("Id,TeamId,DepartmentId")] User user)
        {
            //TODO: Add edit without admin permissision for user
            if (id != user.Id)
            {
                return BadRequest();
            }
            var teamId = user.TeamId;
            var depId = user.DepartmentId;

            user = await _context.Users.FindAsync(user.Id);
            user.TeamId = teamId;
            user.DepartmentId = depId;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }

            ViewData["TeamId"] = new SelectList(await GetTeamsDisplayList(), "Id", "Name", user.TeamId);
            ViewData["DepartmentId"] = new SelectList(await GetDepartmentDisplayList(), "Id", "Name", user.DepartmentId);
            return View(user);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveUser(string id)
        {
            var user = await _context.Users.AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("RemoveUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserConfirmed(string id)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var userVacBookings = await _context.VacationBookings.Include(x => x.VacationDays).Where(x => x.UserId == user.Id).ToListAsync();
            var userVacDays = userVacBookings.SelectMany(x => x.VacationDays);
            var userRoles = await _context.UserRoles.Where(x => x.UserId == user.Id).ToListAsync();

            try
            {
                _context.VacationDays.RemoveRange(userVacDays);
                _context.VacationBookings.RemoveRange(userVacBookings);
                _context.UserRoles.RemoveRange(userRoles);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }

            user.FirstName = null;
            user.LastName = null;
            user.DisplayName = null;
            user.Email = null;
            user.DepartmentId = null;
            user.TeamId = null;
            user.IsHidden = true;

            var deleteResult = await _userManager.UpdateAsync(user);
            if (!deleteResult.Succeeded)
            {
                ViewData["DeleteError"] = "Something went wrong when removing the user.";
                var notChangedUser = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);
                return View(nameof(RemoveUser), notChangedUser);
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<Team>> GetTeamsDisplayList()
        {
            var teamsList = await _context.Teams.ToListAsync();
            teamsList.Add(new Team() { Name = "< None >" });
            return teamsList.OrderBy(t => t.Id).ToList();
        }

        private async Task<List<Department>> GetDepartmentDisplayList()
        {
            var depList = await _context.Departments.ToListAsync();
            depList.Add(new Department() { Name = "< None >" });
            return depList.OrderBy(d => d.Id).ToList();
        }

        private bool UserExists(string id)
        {
            return _context.Users.Any(u => u.Id == id);
        }

        private Task<User> GetCurrentUser()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddRoleForUser(string userEmail, string userRole)
        {
            var user = await _userManager.FindByEmailAsync(userEmail);
            var role = await _context.Roles.FirstOrDefaultAsync(x => x.Name == userRole);
            if (user != null && role != null)
            {
                IdentityResult result = await _userManager.AddToRoleAsync(user, userRole);
                if (result.Succeeded)
                {
                    return Ok($"Added user: {user.DisplayName} to the Role {userRole}.");
                }
                return BadRequest($"Error: {result.ToString()}");
            }
            return user == null
                ? NotFound($"Can't find user {userEmail}.")
                : NotFound($"Can't find role {userRole}");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveRoleForUser(string userEmail, string userRole)
        {
            var user = await _userManager.FindByEmailAsync(userEmail);
            var role = await _context.Roles.FirstOrDefaultAsync(x => x.Name == userRole);
            if (user != null && role != null)
            {
                var result = await _userManager.RemoveFromRoleAsync(user, userRole);
                if (result.Succeeded)
                {
                    return Ok($"Removed user: {user.DisplayName} from the Role {userRole}.");
                }
                return BadRequest($"Error: {result.ToString()}");
            }
            return user == null
                ? NotFound($"Can't find user {userEmail}.")
                : NotFound($"Can't find role {userRole}");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveRoles(string userEmail)
        {
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var result = await _userManager.RemoveFromRolesAsync(user, roles);

                if (result.Succeeded)
                {
                    return Ok($"Removed all roles for user: {user.DisplayName}");
                }
                return BadRequest($"Error: {result.ToString()}");
            }
            return NotFound($"Can't find user {userEmail}.");
        }

        #region Account Login
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel
            {
                Errors = new List<string>()
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    model.Errors = result.Errors.Select(x => x.Description).ToList();
                }
            }
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Login(string returnUrl)
        {
            return View(new LoginViewModel
            {
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

                if (result.Succeeded)
                {
                    if (string.IsNullOrEmpty(model.ReturnUrl))
                        return RedirectToAction("Index", "Home");

                    return Redirect(model.ReturnUrl);
                }
            }

            ModelState.AddModelError("", "Username or Password was invalid.");
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
        #endregion
    }
}