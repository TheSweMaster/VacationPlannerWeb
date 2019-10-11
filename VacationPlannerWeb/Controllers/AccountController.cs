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
            user.ManagerUser = await _context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.ManagerUserId);

            var managerForUsers = await _context.Users.AsNoTracking()
                .Where(x => x.ManagerUserId != null).Where(x => x.ManagerUserId == user.Id)
                .Select(x => x.DisplayName).ToListAsync();

            ViewData["ManagerForUserNames"] = string.Join(", ", managerForUsers);
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

            var managerForUsers = await _context.Users.AsNoTracking()
                .Where(x => x.ManagerUserId != null).Where(x => x.ManagerUserId == user.Id)
                .Select(x => x.DisplayName).ToListAsync();

            ViewData["ManagerForUserNames"] = managerForUsers.Any() ? string.Join(", ", managerForUsers) : "< None >";
            ViewData["TeamId"] = new SelectList(await GetTeamsDisplayList(), "Id", "Name", user.TeamId);
            ViewData["DepartmentId"] = new SelectList(await GetDepartmentDisplayList(), "Id", "Name", user.DepartmentId);
            ViewData["ManagerUserId"] = new SelectList(await GetManagersDisplayList(), "Id", "DisplayName", user.ManagerUserId);
            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, [Bind("Id,FirstName,LastName,TeamId,DepartmentId,ManagerUserId")] User user)
        {
            if (id != user?.Id)
            {
                return BadRequest();
            }

            var teamId = user.TeamId;
            var depId = user.DepartmentId;
            var manId = user.ManagerUserId;
            var firstName = user.FirstName;
            var lastName = user.LastName;

            user = await _context.Users.FindAsync(user.Id);
            user.FirstName = firstName;
            user.LastName = lastName;
            user.DisplayName = $"{firstName} {lastName}";
            user.TeamId = teamId;
            user.DepartmentId = depId;
            user.ManagerUserId = manId;

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
                return RedirectToAction(nameof(Index));
            }

            ViewData["TeamId"] = new SelectList(await GetTeamsDisplayList(), "Id", "Name", user.TeamId);
            ViewData["DepartmentId"] = new SelectList(await GetDepartmentDisplayList(), "Id", "Name", user.DepartmentId);
            ViewData["ManagerUserId"] = new SelectList(await GetManagersDisplayList(), "Id", "DisplayName", user.ManagerUserId);
            return View(user);
        }

        [Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await GetCurrentUser();
            if (user == null)
            {
                return NotFound();
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin == false)
            {
                if (user.Id != id)
                {
                    return View(nameof(AccessDenied)); ;
                }
            }

            var userToEdit = await _context.Users.SingleOrDefaultAsync(u => u.Id == id);
            if (userToEdit == null)
            {
                return NotFound();
            }

            ViewData["TeamId"] = new SelectList(await GetTeamsDisplayList(), "Id", "Name", userToEdit.TeamId);
            ViewData["DepartmentId"] = new SelectList(await GetDepartmentDisplayList(), "Id", "Name", userToEdit.DepartmentId);
            return View(userToEdit);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,FirstName,LastName,TeamId,DepartmentId")] User user)
        {
            if (id != user?.Id)
            {
                return NotFound();
            }

            var currentUser = await GetCurrentUser();
            
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            if (isAdmin == false)
            {
                if (currentUser?.Id != user.Id)
                {
                    return View(nameof(AccessDenied));
                }
            }

            var teamId = user.TeamId;
            var depId = user.DepartmentId;
            var firstName = user.FirstName;
            var lastName = user.LastName;

            user = await _context.Users.FindAsync(user.Id);
            user.FirstName = firstName;
            user.LastName = lastName;
            user.DisplayName = $"{firstName} {lastName}";
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
                return RedirectToAction(nameof(UserProfile));
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
            
            user.Team = await _context.Teams.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.TeamId);
            user.Department = await _context.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.DepartmentId);

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
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                ViewData["DeleteError"] = $"You can't remove an Admin user.";
                return View(nameof(RemoveUser), user);
            }

            var userVacBookings = await _context.VacationBookings.Include(x => x.VacationDays).Where(x => x.UserId == user.Id).ToListAsync();
            var userVacDays = userVacBookings.SelectMany(x => x.VacationDays);
            var userRoles = await _context.UserRoles.Where(x => x.UserId == user.Id).ToListAsync();

            var usersToRemoveManager = _context.Users.Where(x => x.ManagerUserId == user.Id).ToList();
            usersToRemoveManager.ForEach(u => u.ManagerUserId = null);

            try
            {
                _context.VacationDays.RemoveRange(userVacDays);
                _context.VacationBookings.RemoveRange(userVacBookings);
                _context.UserRoles.RemoveRange(userRoles);
                _context.Users.UpdateRange(usersToRemoveManager);

                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                ViewData["DeleteError"] = $"Something went wrong when removing the user. {deleteResult.ToString()}";
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

        private async Task<List<User>> GetManagersDisplayList()
        {
            var managerList = await _userManager.GetUsersInRoleAsync("Manager");
            managerList.Add(new User { DisplayName = "< None >", Id = null });
            return managerList.OrderBy(d => d.DisplayName).ToList();
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
                    if (userRole == "Manager")
                    {
                        var usersToRemoveManager = _context.Users.Where(x => x.ManagerUserId == user.Id).ToList();
                        usersToRemoveManager.ForEach(u => u.ManagerUserId = null);

                        _context.Users.UpdateRange(usersToRemoveManager);
                        await _context.SaveChangesAsync();
                    }
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
        public async Task<IActionResult> Register()
        {
            ViewData["TeamId"] = new SelectList(await GetTeamsDisplayList(), "Id", "Name");
            ViewData["DepartmentId"] = new SelectList(await GetDepartmentDisplayList(), "Id", "Name");

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
                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DisplayName = $"{model.FirstName} {model.LastName}",
                    TeamId = model.TeamId,
                    DepartmentId = model.DepartmentId
                };

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

            ViewData["TeamId"] = new SelectList(await GetTeamsDisplayList(), "Id", "Name", model.TeamId);
            ViewData["DepartmentId"] = new SelectList(await GetDepartmentDisplayList(), "Id", "Name", model.DepartmentId);
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

            var user = await _userManager.FindByNameAsync(model.Email);

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