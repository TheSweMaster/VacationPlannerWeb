using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationPlannerWeb.DataAccess;
using VacationPlannerWeb.Models;
using System.Globalization;
using VacationPlannerWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using VacationPlannerWeb.Extensions;

namespace VacationPlannerWeb.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private static readonly CultureInfo _cultureInfo = new CultureInfo("sv-SE");

        private const string SessionKeyRoleFilter = "_RoleFilter";
        private const string SessionKeyDepartmentFilter = "_DepartmentFilter";
        private const string SessionKeyTeamsFilter = "_TeamFilter";
        private const string NoneRoleId = "#None-Role-Id";
        private const string NoneDepartmentId = "#None-Department-Id";
        private const string NoneTeamId = "#None-Team-Id";

        public CalendarController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> MyCalendar(int year, int month)
        {
            PagingLogicAndValidationForYearAndMonth(ref year, ref month);

            var firstDayOfMonth = CalendarHelper.GetFirstDayOfMonth(year, month);
            var shortDayOfWeekList = CalendarHelper.GetDayOfWeekListShort();
            var dayOfWeekList = CalendarHelper.GetDayOfWeekList();

            var user = await GetCurrentUser();

            var vacBookingList = await GetVacationBookingsByUser(user.Id);

            var absenceTypesList = await _context.AbsenceTypes.Select(x => x.Name).ToListAsync();
            var workFreeDaysList = await _context.WorkFreeDays.ToListAsync();

            var vacDaysList = GetAllVacationDaysFromBookings(vacBookingList);

            DateTime dateOfFirstDayInWeekOfMonth = CalendarHelper.GetFirstDayInWeek(firstDayOfMonth, dayOfWeekList);

            var dataLists = new CalendarDataLists(vacBookingList, workFreeDaysList, vacDaysList);

            var weekCalendarDayDic = new Dictionary<int, List<CalendarDay>>();

            var displayDatesOfWeek = 7; //Change to 5 to exclude saturdays and sundays
            const int amountOfWeeks = 6;
            const int totalDaysOfWeek = 7;
            for (int w = 0; w < amountOfWeeks; w++)
            {
                DateTime firstDayInWeek = dateOfFirstDayInWeekOfMonth.AddDays(w * totalDaysOfWeek);
                int weekNumber = CalendarHelper.GetISO8601WeekNumber(firstDayInWeek, _cultureInfo);
                List<CalendarDay> calDaysInWeek = GetCalendarDaysInWeek(dataLists, firstDayInWeek, displayDatesOfWeek);
                weekCalendarDayDic.Add(weekNumber, calDaysInWeek);
            }

            var calendarVM = new CalendarViewModel
            {
                Year = year,
                Month = month,
                Date = firstDayOfMonth,
                DayOfWeekList = shortDayOfWeekList,
                WeekCalendarData = weekCalendarDayDic,
                AbsenceTypes = absenceTypesList,
                PostBackActionName = nameof(MyCalendar)
            };
            return View(calendarVM);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManagerOverview(int year, int weeknumber, string sortOrder)
        {
            PagingLogicAndValidationForYearAndWeekNumber(ref year, ref weeknumber, _cultureInfo);

            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["RoleSortParam"] = sortOrder == "role_desc" ? "role_desc" : "role";
            ViewData["DepartmentSortParam"] = sortOrder == "department_desc" ? "department_desc" : "department";
            ViewData["TeamSortParam"] = sortOrder == "team_desc" ? "team_desc" : "team";

            var roleFilter = await GetRoleFilterItems();
            var departmentFilter = await GetDepartmentFilterItems();
            var teamFilter = await GetTeamFilterItems();

            var roleFilterString = HttpContext.Session.GetString(SessionKeyRoleFilter);
            var departmentFilterString = HttpContext.Session.GetString(SessionKeyDepartmentFilter);
            var teamFilterString = HttpContext.Session.GetString(SessionKeyTeamsFilter);

            var userList = await GetAllUsersWithRoles();
            userList = FilterUserList(roleFilter, departmentFilter, teamFilter, userList);
            var sortedUsersList = GetSortedUserList(sortOrder, userList);

            var absenceTypesList = await _context.AbsenceTypes.Select(x => x.Name).ToListAsync();
            var workFreeDaysList = await _context.WorkFreeDays.ToListAsync();

            DateTime currentFirstDayInWeek = CalendarHelper.FirstDateOfWeekISO8601(year, weeknumber, _cultureInfo);
            var userCalendarDayDic = await GetUserCalendarDayDictionary(sortedUsersList, workFreeDaysList, currentFirstDayInWeek);

            List<CalendarDay> caldaysList = GetAllCalendarDays(currentFirstDayInWeek);

            var calendarVM = new CalendarOverviewViewModel
            {
                Year = year,
                WeekNumber = weeknumber,
                Date = currentFirstDayInWeek,
                AbsenceTypes = absenceTypesList,
                AllUsersCalendarData = userCalendarDayDic,
                CalendarDaysList = caldaysList,
                SortOrder = sortOrder,
                RoleFilter = roleFilter,
                RoleFilterString = roleFilterString,
                DepartmentFilter = departmentFilter,
                DepartmentFilterString = departmentFilterString,
                TeamFilter = teamFilter,
                TeamFilterString = teamFilterString,
                PostBackActionName = nameof(ManagerOverview)
            };
            return View(calendarVM);
        }

        [Authorize]
        public async Task<IActionResult> UserOverview(int year, int weeknumber, string sortOrder)
        {
            PagingLogicAndValidationForYearAndWeekNumber(ref year, ref weeknumber, _cultureInfo);

            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["RoleSortParam"] = sortOrder == "role_desc" ? "role_desc" : "role";
            ViewData["DepartmentSortParam"] = sortOrder == "department_desc" ? "department_desc" : "department";
            ViewData["TeamSortParam"] = sortOrder == "team_desc" ? "team_desc" : "team";

            var roleFilter = await GetRoleFilterItems();
            var departmentFilter = await GetDepartmentFilterItems();
            var teamFilter = await GetTeamFilterItems();

            var roleFilterString = HttpContext.Session.GetString(SessionKeyRoleFilter);
            var departmentFilterString = HttpContext.Session.GetString(SessionKeyDepartmentFilter);
            var teamFilterString = HttpContext.Session.GetString(SessionKeyTeamsFilter);

            var userList = await GetAllUsersWithRoles();
            userList = FilterUserList(roleFilter, departmentFilter, teamFilter, userList);
            var sortedUsersList = GetSortedUserList(sortOrder, userList);

            var absenceTypesList = await _context.AbsenceTypes.Select(x => x.Name).ToListAsync();
            var workFreeDaysList = await _context.WorkFreeDays.ToListAsync();

            DateTime currentFirstDayInWeek = CalendarHelper.FirstDateOfWeekISO8601(year, weeknumber, _cultureInfo);

            var userCalendarDayDic = await GetUserCalendarDayDictionary(sortedUsersList, workFreeDaysList, currentFirstDayInWeek, true);

            List<CalendarDay> caldaysList = GetAllCalendarDays(currentFirstDayInWeek);

            var calendarVM = new CalendarOverviewViewModel
            {
                Year = year,
                WeekNumber = weeknumber,
                Date = currentFirstDayInWeek,
                AbsenceTypes = absenceTypesList,
                AllUsersCalendarData = userCalendarDayDic,
                CalendarDaysList = caldaysList,
                SortOrder = sortOrder,
                RoleFilter = roleFilter,
                RoleFilterString = roleFilterString,
                DepartmentFilter = departmentFilter,
                DepartmentFilterString = departmentFilterString,
                TeamFilter = teamFilter,
                TeamFilterString = teamFilterString,
                PostBackActionName = nameof(UserOverview)
            };
            return View(calendarVM);
        }


        private async Task<List<FilterItem>> GetTeamFilterItems()
        {
            var teamFilter = HttpContext.Session.Get<List<FilterItem>>(SessionKeyTeamsFilter);
            teamFilter = (teamFilter == null || teamFilter.Count == 0)
                ? AddNoneTeamsFilterItem(await GetTeamsToCheckBoxItemList()) : teamFilter;
            return teamFilter;
        }

        private async Task<List<FilterItem>> GetDepartmentFilterItems()
        {
            var departmentFilter = HttpContext.Session.Get<List<FilterItem>>(SessionKeyDepartmentFilter);
            departmentFilter = (departmentFilter == null || departmentFilter.Count == 0)
                ? AddNoneDepartmentFilterItem(await GetDepartmentsToCheckBoxItemList()) : departmentFilter;
            return departmentFilter;
        }

        private async Task<List<FilterItem>> GetRoleFilterItems()
        {
            var roleFilter = HttpContext.Session.Get<List<FilterItem>>(SessionKeyRoleFilter);
            roleFilter = (roleFilter == null || roleFilter.Count == 0)
                ? AddNoneRolesFilterItem(await GetRolesToCheckBoxItemList()) : roleFilter;
            return roleFilter;
        }

        private List<User> FilterUserList(List<FilterItem> roleFilter, List<FilterItem> departmentFilter, List<FilterItem> teamFilter, List<User> userList)
        {
            userList = FilterUserRoles(userList, roleFilter);
            userList = FilterUserDepartments(userList, departmentFilter);
            userList = FilterUserTeams(userList, teamFilter);
            userList = GetDistinctUsers(userList);
            return userList;
        }

        private async Task<Dictionary<User, List<CalendarDay>>> GetUserCalendarDayDictionary(IEnumerable<User> sortedUsersList, List<WorkFreeDay> workFreeDaysList, DateTime currentFirstDayInWeek, bool excludeAbsenceType = false)
        {
            var userCalendarDayDic = new Dictionary<User, List<CalendarDay>>();

            foreach (var user in sortedUsersList)
            {
                var userVacBookingList = await GetVacationBookingsByUser(user.Id, excludeAbsenceType);
                if (userVacBookingList.Count == 0)
                {
                    continue;
                }
                var userVacDaysList = GetAllVacationDaysFromBookings(userVacBookingList);
                var dataLists = new CalendarDataLists(userVacBookingList, workFreeDaysList, userVacDaysList);

                var displayDatesOfWeek = 5; //Change to 7 to include saturdays and sundays
                var allUserCalendarDays = GetAllCalendarDays(currentFirstDayInWeek, dataLists, displayDatesOfWeek);

                user.Team = await _context.Teams.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.TeamId);
                user.Department = await _context.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.DepartmentId);
                userCalendarDayDic.Add(user, allUserCalendarDays);
            }

            return userCalendarDayDic;
        }

        private List<FilterItem> AddNoneTeamsFilterItem(List<FilterItem> list)
        {
            list.Add(
                new FilterItem()
                {
                    Id = NoneTeamId,
                    Name = "No Teams",
                    Selected = true,
                });
            return list;
        }

        private List<FilterItem> AddNoneDepartmentFilterItem(List<FilterItem> list)
        {
            list.Add(
                new FilterItem()
                {
                    Id = NoneDepartmentId,
                    Name = "No Departments",
                    Selected = true,
                });
            return list;
        }

        private List<FilterItem> AddNoneRolesFilterItem(List<FilterItem> list)
        {
            list.Add(
                new FilterItem()
                {
                    Id = NoneRoleId,
                    Name = "No Roles",
                    Selected = true,
                });
            return list;
        }

        private async Task<List<User>> GetAllUsersWithRoles()
        {
            var userList = await _context.Users.AsNoTracking().Where(x => !x.IsHidden).ToListAsync();
            foreach (var user in userList)
            {
                var userRoleIds = _context.UserRoles.AsNoTracking()
                    .Where(x => x.UserId == user.Id).Select(x => x.RoleId);
                var roles = _context.Roles.AsNoTracking()
                    .Join(userRoleIds, role => role.Id, id => id, (role, id) => role);
                user.Roles = await roles.ToListAsync();
            }
            return userList;
        }

        private static IEnumerable<User> GetSortedUserList(string sortOrder, IEnumerable<User> userList)
        {
            IEnumerable<User> userOrdered;
            switch (sortOrder)
            {
                case "name_desc":
                    userOrdered = userList.OrderByDescending(x => x.DisplayName);
                    break;
                case "role":
                    userOrdered = userList.OrderBy(x => x.Roles.FirstOrDefault()?.Name);
                    break;
                case "role_desc":
                    userOrdered = userList.OrderByDescending(x => x.Roles.FirstOrDefault()?.Name);
                    break;
                case "department":
                    userOrdered = userList.OrderBy(x => x.DepartmentId);
                    break;
                case "department_desc":
                    userOrdered = userList.OrderByDescending(x => x.DepartmentId);
                    break;
                case "team":
                    userOrdered = userList.OrderBy(x => x.TeamId);
                    break;
                case "team_desc":
                    userOrdered = userList.OrderByDescending(x => x.TeamId);
                    break;
                default:
                    userOrdered = userList.OrderBy(x => x.DisplayName);
                    break;
            }
            return userOrdered;
        }

        private List<User> FilterUserRoles(List<User> userList, List<FilterItem> roleFilterList)
        {
            var list = new List<User>();
            foreach (var roleFilterItem in roleFilterList)
            {
                if (roleFilterItem.Selected)
                {
                    list.AddRange(roleFilterItem.Id == NoneRoleId
                        ? userList.Where(x => x.IsHidden == false).Where(x => !x.Roles.Any())
                        : userList.Where(u => u.Roles.Any(r => r.Id == roleFilterItem.Id)));
                }
            }
            return list;
        }

        private List<User> FilterUserDepartments(List<User> userList, List<FilterItem> departmentFilter)
        {
            var list = new List<User>();
            foreach (var dep in departmentFilter)
            {
                if (dep.Selected)
                {
                    list.AddRange(dep.Id == NoneDepartmentId
                        ? userList.Where(x => x.IsHidden == false)
                            .Where(x => x.DepartmentId == 0 || x.DepartmentId == null)
                        : userList.Where(x => x.DepartmentId?.ToString() == dep.Id));
                }
            }
            return list;
        }

        private List<User> FilterUserTeams(List<User> userList, List<FilterItem> teamsFilter)
        {
            var list = new List<User>();
            foreach (var team in teamsFilter)
            {
                if (team.Selected)
                {
                    list.AddRange(team.Id == NoneTeamId
                        ? userList.Where(x => x.IsHidden == false).Where(x => x.TeamId == 0 || x.TeamId == null)
                        : userList.Where(x => x.TeamId?.ToString() == team.Id));
                }
            }
            return list;
        }

        private static List<User> GetDistinctUsers(List<User> userList)
        {
            return userList.GroupBy(u => u.Id).Select(g => g.First()).ToList();
        }

        [HttpPost, ActionName("OverviewSetFilter")]
        public async Task<IActionResult> OverviewSetFilter([Bind] CalendarOverviewViewModel model)
        {
            var roleFilter = model.RoleFilter;
            var departmentFilter = model.DepartmentFilter;
            var teamFilter = model.TeamFilter;

            roleFilter = (roleFilter == null || roleFilter.Count == 0)
                ? AddNoneRolesFilterItem(await GetRolesToCheckBoxItemList()) : roleFilter;
            departmentFilter = (departmentFilter == null || departmentFilter.Count == 0)
                ? AddNoneDepartmentFilterItem(await GetDepartmentsToCheckBoxItemList()) : departmentFilter;
            teamFilter = (teamFilter == null || teamFilter.Count == 0)
                ? AddNoneTeamsFilterItem(await GetTeamsToCheckBoxItemList()) : teamFilter;

            var calendarVM = new CalendarOverviewViewModel
            {
                Year = model.Year,
                WeekNumber = model.WeekNumber,
                SortOrder = model.SortOrder,
            };

            HttpContext.Session.Set(SessionKeyRoleFilter, roleFilter);
            HttpContext.Session.Set(SessionKeyDepartmentFilter, departmentFilter);
            HttpContext.Session.Set(SessionKeyTeamsFilter, teamFilter);
            return RedirectToAction(model.PostBackActionName, calendarVM);
        }

        private List<CalendarDay> GetAllCalendarDays(DateTime currentFirstDayInWeek)
        {
            var displayDatesOfWeek = 5; //Change to 7 to include saturdays and sundays
            const int amountOfWeeks = 6;
            const int totalDaysOfWeek = 7;
            return Enumerable.Range(0, amountOfWeeks)
                .SelectMany(num =>
                    GetCalendarDaysInWeek(currentFirstDayInWeek.AddDays(num * totalDaysOfWeek), displayDatesOfWeek))
                .ToList();
        }

        private static List<CalendarDay> GetAllCalendarDays(DateTime currentFirstDayInWeek, CalendarDataLists dataLists, int displayDatesOfWeek)
        {
            const int amountOfWeeks = 6;
            const int totalDaysOfWeek = 7;
            return Enumerable.Range(0, amountOfWeeks)
                .SelectMany(num =>
                    GetCalendarDaysInWeek(dataLists, currentFirstDayInWeek.AddDays(num * totalDaysOfWeek), displayDatesOfWeek))
                .ToList();
        }

        private async Task<List<FilterItem>> GetRolesToCheckBoxItemList()
        {
            return await _context.Roles.Where(x => x.Name != "Admin" && x.Name != "Manager")
                .Select(x => new FilterItem { Id = x.Id, Name = $"{x.Name} - {x.Shortening}", Selected = true }).ToListAsync();
        }

        private async Task<List<FilterItem>> GetDepartmentsToCheckBoxItemList()
        {
            return await _context.Departments.Select(x => new FilterItem { Id = x.Id.ToString(), Name = $"{x.Name} - {x.Shortening}", Selected = true }).ToListAsync();
        }

        private async Task<List<FilterItem>> GetTeamsToCheckBoxItemList()
        {
            return await _context.Teams.Select(x => new FilterItem { Id = x.Id.ToString(), Name = $"{x.Name} - {x.Shortening}", Selected = true }).ToListAsync();
        }

        private void PagingLogicAndValidationForYearAndWeekNumber(ref int year, ref int weeknumber, CultureInfo culture)
        {
            var today = DateTime.Today;

            if (year < DateTime.MinValue.Year || year > DateTime.MaxValue.Year)
            {
                year = today.Year;
                weeknumber = CalendarHelper.GetISO8601WeekNumber(today, culture);
            }

            var lastWeekOfYear = CalendarHelper.GetLastWeekOfYear(year, culture);
            if (year > 0 && weeknumber <= 0)
            {
                year -= 1;
                weeknumber = lastWeekOfYear;
            }
            if (year > 0 && weeknumber > lastWeekOfYear)
            {
                year += 1;
                weeknumber = 1;
            }

        }

        private static List<CalendarDay> GetCalendarDaysInWeek(CalendarDataLists dataLists, DateTime firstDayInWeek, int displayDatesOfWeek)
        {
            return Enumerable.Range(0, displayDatesOfWeek)
                .Select(num => GetCalendarDay(dataLists, firstDayInWeek.AddDays(num))).ToList();
        }

        private static List<CalendarDay> GetCalendarDaysInWeek(DateTime firstDayInWeek, int displayDatesOfWeek)
        {
            return Enumerable.Range(0, displayDatesOfWeek)
                .Select(num => GetCalendarDay(firstDayInWeek.AddDays(num))).ToList();
        }

        private static CalendarDay GetCalendarDay(CalendarDataLists dataLists, DateTime weekDate)
        {
            string approval = null;
            string absenceType = null;
            bool isPlanned = false;
            var vacationBookingId = 0;
            bool isHoliday = false;
            string note = null;

            if (dataLists.VacDaysList.Any(v => v.VacationDate == weekDate))
            {
                var vacBookingId = dataLists.VacDaysList.FirstOrDefault(v => v.VacationDate == weekDate).VacationBookingId;
                var vacbooking = dataLists.VacBookingList.FirstOrDefault(v => v.Id == vacBookingId);
                approval = vacbooking.Approval;
                absenceType = vacbooking.AbsenceType?.Name;
                isPlanned = true;
                vacationBookingId = vacBookingId;
            }
            if (dataLists.HolidayList.Contains(weekDate))
            {
                isHoliday = true;
                note = dataLists.WorkFreeDaysList.FirstOrDefault(x => x.Date == weekDate).Name;
            }

            return new CalendarDay()
            {
                Approval = approval,
                AbsenceType = absenceType,
                IsPlannedVacation = isPlanned,
                VacationBookingId = vacationBookingId,
                IsHoliday = isHoliday,
                Note = note,
                IsToday = (weekDate == DateTime.Today),
                IsWeekend = (weekDate.DayOfWeek == DayOfWeek.Saturday || weekDate.DayOfWeek == DayOfWeek.Sunday),
                IsStartOfWeek = (weekDate.DayOfWeek == DayOfWeek.Monday),
                Date = weekDate,
                WeekNumber = CalendarHelper.GetISO8601WeekNumber(weekDate, _cultureInfo),
            };
        }

        private static CalendarDay GetCalendarDay(DateTime weekDate)
        {
            string approval = null;
            string absenceType = null;
            bool isPlanned = false;
            var vacationBookingId = 0;
            bool isHoliday = false;
            string note = null;

            return new CalendarDay()
            {
                Approval = approval,
                AbsenceType = absenceType,
                IsPlannedVacation = isPlanned,
                VacationBookingId = vacationBookingId,
                IsHoliday = isHoliday,
                Note = note,
                IsToday = (weekDate == DateTime.Today),
                IsWeekend = (weekDate.DayOfWeek == DayOfWeek.Saturday || weekDate.DayOfWeek == DayOfWeek.Sunday),
                IsStartOfWeek = (weekDate.DayOfWeek == DayOfWeek.Monday),
                Date = weekDate,
                WeekNumber = CalendarHelper.GetISO8601WeekNumber(weekDate, _cultureInfo),
            };
        }

        public class CalendarDataLists
        {
            public CalendarDataLists(List<VacationBooking> vacBookingList,
                List<WorkFreeDay> workFreeDaysList,
                List<VacationDay> vacDaysList)
            {
                VacBookingList = vacBookingList;
                WorkFreeDaysList = workFreeDaysList;
                HolidayList = workFreeDaysList.Select(x => x.Date).ToList(); ;
                VacDaysList = vacDaysList;
            }

            public List<VacationBooking> VacBookingList { get; }
            public List<WorkFreeDay> WorkFreeDaysList { get; }
            public List<DateTime> HolidayList { get; }
            public List<VacationDay> VacDaysList { get; }
        }

        private static List<VacationDay> GetAllVacationDaysFromBookings(List<VacationBooking> vacBookingList)
        {
            return vacBookingList.SelectMany(x => x.VacationDays).ToList();
        }

        private async Task<List<VacationBooking>> GetVacationBookingsByUser(string userId, bool excludeAbsenceType = false)
        {
            if (excludeAbsenceType)
            {
                return await _context.VacationBookings.AsNoTracking()
                .Include(v => v.User).Include(v => v.VacationDays)
                .Where(x => x.UserId == userId).ToListAsync();
            }
            else
            {
                return await _context.VacationBookings.AsNoTracking()
                .Include(v => v.User).Include(v => v.VacationDays).Include(v => v.AbsenceType)
                .Where(x => x.UserId == userId).ToListAsync();
            }

        }

        private async Task<List<VacationBooking>> GetVacationBookingsByUserNoAbsenceType(string userId)
        {
            return await _context.VacationBookings.AsNoTracking()
                .Include(v => v.User).Include(v => v.VacationDays)
                .Where(x => x.UserId == userId).ToListAsync();
        }

        private async Task<User> GetCurrentUser()
        {
            return await _userManager.GetUserAsync(HttpContext.User);
        }

        private static void PagingLogicAndValidationForYearAndMonth(ref int year, ref int month)
        {
            var today = DateTime.Today;
            if (year > 0 && month <= 0)
            {
                year -= 1;
                month = 12;
            }
            if (year > 0 && month >= 13)
            {
                year += 1;
                month = 1;
            }

            if (year < DateTime.MinValue.Year || year > DateTime.MaxValue.Year)
            {
                year = today.Year;
                month = today.Month;
            }
        }
    }
}