using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VacationPlannerWeb.DataAccess;
using VacationPlannerWeb.Models;

namespace VacationPlannerWeb.Controllers
{
    [Authorize]
    public class VacationBookingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public VacationBookingsController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: VacationBookings
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUser();
            if (user == null)
            {
                return NotFound($"Current user was not found in the database.");
            }

            List<VacationBooking> vacBookingList;
            if (await HasRolesAsync(user, "Admin"))
            {
                vacBookingList = await GetAllVacationBookings();
            }
            else if (await HasRolesAsync(user, "Manager"))
            {
                vacBookingList = GetVacationBookingsByManager(user);
            }
            else
            {
                vacBookingList = await GetVacationBookingsByUser(user);
            }

            return View(vacBookingList);
        }

        private Task<User> GetCurrentUser()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> TodoList()
        {
            var user = await GetCurrentUser();
            var vacBookingList = await _context.VacationBookings.AsNoTracking().Include(v => v.AbsenceType).Include(v => v.User)
                .Where(v => v.Approval == ApprovalState.Pending.ToString())
                .OrderBy(x => x.FromDate).ToListAsync();

            var result = vacBookingList.Where(v => IsManagerForBookingUser(v, user)).ToList();

            return View(result);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public async Task<IActionResult> TodoListApproval(int vacationBookingId, string approvalState)
        {
            var vacationBooking = await _context.VacationBookings
                .Include(x => x.AbsenceType).Include(u => u.User)
                .SingleOrDefaultAsync(x => x.Id == vacationBookingId);
            if (vacationBooking == null)
            {
                return NotFound();
            }
            if (approvalState != ApprovalState.Approved.ToString() && approvalState != ApprovalState.Denied.ToString())
            {
                return BadRequest();
            }
            vacationBooking.Approval = approvalState;
            var hasChanges = _context.ChangeTracker.HasChanges();
            try
            {
                _context.Update(vacationBooking);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }

            return RedirectToAction(nameof(TodoList));
        }

        private async Task<List<VacationBooking>> GetVacationBookingsByUser(User user)
        {
            return await _context.VacationBookings.Include(v => v.User).Include(v => v.VacationDays).Include(v => v.AbsenceType)
                .Where(v => v.UserId == user.Id).ToListAsync();
        }

        private async Task<List<VacationBooking>> GetVacationBookingsNoTrackingByUserId(string userId)
        {
            return await _context.VacationBookings.AsNoTracking().Include(v => v.User).Include(v => v.VacationDays).Include(v => v.AbsenceType)
                .Where(v => v.UserId == userId).ToListAsync();
        }

        private async Task<List<VacationBooking>> GetAllVacationBookings()
        {
            return await _context.VacationBookings.Include(v => v.User).Include(v => v.VacationDays).Include(v => v.AbsenceType)
                .ToListAsync();
        }

        private List<VacationBooking> GetVacationBookingsByManager(User userManager)
        {
            var vacationBookingList = new List<VacationBooking>();
            var allBookings = _context.VacationBookings.Include(v => v.VacationDays).Include(v => v.AbsenceType).Include(v => v.User).ToList();
            vacationBookingList.AddRange(allBookings.Where(v => v.UserId == userManager.Id));
            vacationBookingList.AddRange(allBookings.Where(v => IsManagerForBookingUser(v, userManager)));
            
            return vacationBookingList.Distinct().ToList();
        }

        // GET: VacationBookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vacationBooking = await _context.VacationBookings
                .Include(v => v.User).Include(v => v.VacationDays).Include(v => v.AbsenceType)
                .SingleOrDefaultAsync(m => m.Id == id);

            if (vacationBooking == null)
            {
                return NotFound();
            }

            vacationBooking.VacationDays = vacationBooking.VacationDays.OrderBy(x => x.VacationDate).ToList();

            var user = await GetCurrentUser();

            if (!await HasRolesAsync(user, "Admin") && ! IsManagerForBookingUser(vacationBooking, user))
            {
                if (!IsOwner(vacationBooking, user))
                {
                    return View("AccessDenied");
                }
            }

            return View(vacationBooking);
        }

        private static bool IsOwner(VacationBooking vacationBooking, User user)
        {
            return vacationBooking.UserId == user.Id;
        }

        private bool IsManagerForBookingUser(VacationBooking vacationBooking, User user)
        {
            return vacationBooking.User.ManagerUserId == user.Id;
        }

        //Todo: Add both start and end-date as in parameters
        public async Task<IActionResult> Create(string startdate)
        {
            var vacBooking = new VacationBooking();
            if (!string.IsNullOrWhiteSpace(startdate))
            {
                vacBooking.FromDate = DateTime.Parse(startdate);
                vacBooking.ToDate = DateTime.Parse(startdate);
            }
            else
            {
                vacBooking.FromDate = DateTime.Today;
                vacBooking.ToDate = DateTime.Today;
            }
            ViewData["AbsenceTypes"] = new SelectList(await GetAbsenceTypes(), nameof(AbsenceType.Id), nameof(AbsenceType.Name));
            return View(vacBooking);
        }

        // POST: VacationBookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FromDate,ToDate,Comment,AbsenceTypeId")] VacationBooking vacationBooking)
        {
            var absenceType = await GetAbsenceTypeById(vacationBooking.AbsenceTypeId);
            if (absenceType == null)
            {
                AddInvalidAbsenceTypeError();
            }

            if (ModelState.IsValid)
            {
                vacationBooking.Approval = ApprovalState.Pending.ToString();

                bool isErrors = false;

                var user = await GetCurrentUser();

                var holidayList = _context.WorkFreeDays.AsNoTracking().Select(x => x.Date).ToList();
                var userVacbookings = await GetVacationBookingsByUser(user);

                var userVacDates = GetVacationDatesFromBookings(userVacbookings);

                GenerateVacationDaysListFromBooking(vacationBooking, holidayList, userVacDates,
                    out List<DateTime> doubleBookingDatesList, out List<VacationDay> vacdayList);

                isErrors = ValidateVacationDaysList(vacdayList, isErrors);
                isErrors = ValidateDoubleBookingDatesList(doubleBookingDatesList, isErrors);

                vacationBooking.VacationDays = vacdayList;
                vacationBooking.UserId = user.Id;
                vacationBooking.User = user;
                vacationBooking.AbsenceType = absenceType;

                if (!isErrors)
                {
                    _context.Add(vacationBooking);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }

            }
            ViewData["AbsenceTypes"] = new SelectList(await GetAbsenceTypes(), nameof(AbsenceType.Id), nameof(AbsenceType.Name));
            return View(vacationBooking);
        }

        private void AddInvalidAbsenceTypeError()
        {
            ModelState.AddModelError(nameof(VacationBooking.AbsenceTypeId), "Invalid AbscenceType");
        }

        private async Task<AbsenceType> GetAbsenceTypeById(int? absenceTypeId)
        {
            return await _context.AbsenceTypes.FindAsync(absenceTypeId);
        }

        // GET: VacationBookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vacationBooking = await _context.VacationBookings
                .Include(v => v.User).Include(v => v.VacationDays).Include(v => v.AbsenceType)
                .SingleOrDefaultAsync(m => m.Id == id);
            if (vacationBooking == null)
            {
                return NotFound();
            }

            var user = await GetCurrentUser();
            bool isNotEditable = false;

            if (!await HasRolesAsync(user, "Admin") && !IsManagerForBookingUser(vacationBooking, user))
            {
                if (!IsOwner(vacationBooking, user))
                {
                    return View("AccessDenied");
                }

                if (ApprovalIsNotPending(vacationBooking))
                {
                    isNotEditable = true;
                    ViewBag.NotEditableMessage = "You can't edit a vacation booking with Approved or Denied status. " +
                        "\nPlease delete existing booking and create a new one.";
                }
            }

            ViewBag.NotEditable = isNotEditable;
            ViewData["AbsenceTypes"] = new SelectList(await GetAbsenceTypes(), nameof(AbsenceType.Id), nameof(AbsenceType.Name), vacationBooking.AbsenceTypeId);
            ViewData["ApprovalStates"] = new SelectList(await GetApprovalStatesForUser(vacationBooking, user), "Value", "Value", vacationBooking.Approval);
            ViewData["UserId"] = new SelectList(await _context.Users.Where(x => x.Id == vacationBooking.UserId).ToListAsync(), "Id", "DisplayName", vacationBooking.UserId);
            return View(vacationBooking);
        }

        private async Task<List<AbsenceType>> GetAbsenceTypes()
        {
            return await _context.AbsenceTypes.AsNoTracking().ToListAsync();
        }

        ///<summary>
        ///Returns list of all valid approval states for the specified user.
        ///</summary>
        private async Task<IEnumerable<object>> GetApprovalStatesForUser(VacationBooking vacationBooking, User user)
        {
            var apporalStateList = (ApprovalState[])Enum.GetValues(typeof(ApprovalState));
            return await HasRolesAsync(user, "Admin") || IsManagerForBookingUser(vacationBooking, user)
                ? apporalStateList.Select(x => new { Value = x.ToString() })
                : apporalStateList.Select(x => new { Value = x.ToString() }).Where(a => a.Value == vacationBooking.Approval);
        }

        // POST: VacationBookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,FromDate,ToDate,Approval,Comment,AbsenceTypeId")] VacationBooking vacationBooking)
        {
            if (id != vacationBooking.Id)
            {
                return NotFound();
            }

            var vacbookingReadOnly = await _context.VacationBookings.AsNoTracking()
                .Include(v => v.User).Include(v => v.VacationDays).Include(v => v.AbsenceType)
                .SingleOrDefaultAsync(v => v.Id == id);

            if (vacbookingReadOnly.UserId != vacationBooking.UserId)
            {
                return NotFound();
            }

            var user = await GetCurrentUser();
            bool isNotEditable = false;

            if (!await HasRolesAsync(user, "Admin") && !IsManagerForBookingUser(vacbookingReadOnly, user))
            {
                if (ApprovalIsNotPending(vacbookingReadOnly))
                {
                    isNotEditable = true;
                    ViewBag.NotEditableMessage = "You can't edit a vacation booking with Approved or Denied status. " +
                        "\nPlease delete existing booking and create a new one.";
                    ModelState.AddModelError("UserId", "You can't edit a vacation booking with Approved or Denied status.");
                }

                ValidateVacationBookingChanges(vacationBooking, vacbookingReadOnly);
            }

            var absenceType = await GetAbsenceTypeById(vacationBooking.AbsenceTypeId);
            if (absenceType == null)
            {
                AddInvalidAbsenceTypeError();
            }

            if (ModelState.IsValid)
            {
                bool isErrors = false;

                var userVacbookings = await GetVacationBookingsNoTrackingByUserId(vacationBooking.UserId);
                var userVacDates = GetVacationDatesFromBookings(userVacbookings);
                var holidayList = await _context.WorkFreeDays.Select(x => x.Date).ToListAsync();
                var previousDates = vacbookingReadOnly.VacationDays.Select(x => x.VacationDate).ToList();

                GenerateVacationDaysListFromBooking(vacationBooking, holidayList, userVacDates, previousDates,
                    out List<DateTime> doubleBookingDatesList, out List<VacationDay> vacdayList);

                isErrors = ValidateVacationDaysList(vacdayList, isErrors);
                isErrors = ValidateDoubleBookingDatesList(doubleBookingDatesList, isErrors);

                vacationBooking.VacationDays = vacdayList;
                vacationBooking.User = vacbookingReadOnly.User;
                vacationBooking.AbsenceType = absenceType;

                if (!isErrors)
                {
                    var oldVacationDays = _context.VacationDays.Where(v => v.VacationBookingId == id);
                    try
                    {
                        _context.RemoveRange(oldVacationDays);
                        _context.Update(vacationBooking);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!VacationBookingExists(vacationBooking.Id))
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
            }

            if (vacationBooking.User == null)
            {
                vacationBooking.User = vacbookingReadOnly.User;
            }
            ViewBag.NotEditable = isNotEditable;
            ViewData["AbsenceTypes"] = new SelectList(await GetAbsenceTypes(), nameof(AbsenceType.Id), nameof(AbsenceType.Name), vacationBooking.AbsenceTypeId);
            ViewData["ApprovalStates"] = new SelectList(await GetApprovalStatesForUser(vacationBooking, user), "Value", "Value", vacationBooking.Approval);
            ViewData["UserId"] = new SelectList(await _context.Users.Where(x => x.Id == vacationBooking.UserId).ToListAsync(), "Id", "DisplayName", vacationBooking.UserId);
            return View(vacationBooking);
        }

        private static List<DateTime> GetVacationDatesFromBookings(List<VacationBooking> userVacbookings)
        {
            return userVacbookings.SelectMany(x => x.VacationDays.Select(d => d.VacationDate)).ToList();
        }

        private static void GenerateVacationDaysListFromBooking(VacationBooking vacationBooking, List<DateTime> holidayList, List<DateTime> userVacDates,
            out List<DateTime> doubleBookingDatesList, out List<VacationDay> vacdayList)
        {
            doubleBookingDatesList = new List<DateTime>();
            vacdayList = new List<VacationDay>();
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
                if (holidayList.Contains(d))
                {
                    continue;
                }
                if (userVacDates.Contains(d))
                {
                    doubleBookingDatesList.Add(d);
                    continue;
                }
                vacdayList.Add(vacday);
            }
        }

        private static void GenerateVacationDaysListFromBooking(VacationBooking vacationBooking, List<DateTime> holidayList, List<DateTime> userVacDates, List<DateTime> previousDates,
            out List<DateTime> doubleBookingDatesList, out List<VacationDay> vacdayList)
        {
            doubleBookingDatesList = new List<DateTime>();
            vacdayList = new List<VacationDay>();
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
                if (holidayList.Contains(d))
                {
                    continue;
                }
                if (previousDates.Contains(d))
                {
                    vacdayList.Add(vacday);
                    continue;
                }
                if (userVacDates.Contains(d))
                {
                    doubleBookingDatesList.Add(d);
                    continue;
                }
                vacdayList.Add(vacday);
            }
        }

        private bool ValidateDoubleBookingDatesList(List<DateTime> doubleBookingDatesList, bool isErrors)
        {
            if (doubleBookingDatesList.Count > 0)
            {
                var displayDatesString = DatesToDisplayString(doubleBookingDatesList);
                ModelState.AddModelError("FromDate", $"You have already booked a vacation on these dates: {displayDatesString}");
                ModelState.AddModelError("ToDate", $"You have already booked a vacation on these dates: {displayDatesString}");
                isErrors = true;
            }
            return isErrors;
        }

        private bool ValidateVacationDaysList(List<VacationDay> vacdayList, bool isErrors)
        {
            if (vacdayList.Count <= 0)
            {
                ModelState.AddModelError("FromDate", "You can't book a vacation with 0 vacation days.");
                ModelState.AddModelError("ToDate", "You can't book a vacation with 0 vacation days.");
                isErrors = true;
            }
            return isErrors;
        }

        ///<summary>
        ///Returns a string with all dates included in the list, formated and separated with ",".
        ///</summary>
        private static string DatesToDisplayString(List<DateTime> datesList)
        {
            var listOfDates = datesList.Select(x => x.Date.ToString("yyyy-MM-dd"));
            var displayDatesString = string.Join(", ", listOfDates);
            return displayDatesString;
        }

        private static void ValidateVacationBookingChanges(VacationBooking vacationBooking, VacationBooking vacbookingReadOnly)
        {
            vacationBooking.Approval = vacbookingReadOnly.Approval;
            vacationBooking.UserId = vacbookingReadOnly.UserId;
        }

        //private static bool VacBookingApprovalHasChanged(VacationBooking vacationBooking, VacationBooking vacbookingReadOnly)
        //{
        //    return vacbookingReadOnly.Approval != vacationBooking.Approval;
        //}

        ///<summary>
        ///Returns the result if the user has a role included in the string separated with ",".
        ///</summary>
        private async Task<bool> HasRolesAsync(User user, string rolesString)
        {
            var rolesArray = rolesString.Split(',');
            foreach (var role in rolesArray)
            {
                if (await _userManager.IsInRoleAsync(user, role))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ApprovalIsNotPending(VacationBooking vacbookingReadOnly)
        {
            return vacbookingReadOnly.Approval != ApprovalState.Pending.ToString();
        }

        // GET: VacationBookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vacationBooking = await _context.VacationBookings
                .Include(v => v.User).Include(x => x.AbsenceType).Include(x => x.AbsenceType)
                .SingleOrDefaultAsync(m => m.Id == id);
            if (vacationBooking == null)
            {
                return NotFound();
            }

            var user = await GetCurrentUser();

            if (!await HasRolesAsync(user, "Admin") && !IsManagerForBookingUser(vacationBooking, user))
            {
                if (!IsOwner(vacationBooking, user))
                {
                    return View("AccessDenied");
                }

                if (vacationBooking.Approval == ApprovalState.Approved.ToString() && vacationBooking.FromDate < DateTime.Today)
                {
                    return BadRequest("You can't remove an Approved vacation booking after it started, Ask your manager for help.");
                }
            }

            return View(vacationBooking);
        }

        // POST: VacationBookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vacationBooking = await _context.VacationBookings.Include(v => v.VacationDays).SingleOrDefaultAsync(m => m.Id == id);
            _context.VacationBookings.Remove(vacationBooking);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VacationBookingExists(int id)
        {
            return _context.VacationBookings.Any(e => e.Id == id);
        }
    }
}