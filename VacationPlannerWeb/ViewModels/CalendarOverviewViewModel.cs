using VacationPlannerWeb.Models;
using System;
using System.Collections.Generic;

namespace VacationPlannerWeb.ViewModels
{
    public class CalendarOverviewViewModel
    {
        public DateTime? Date { get; set; }
        public int Year { get; set; }
        public int WeekNumber { get; set; }
        public string SortOrder { get; set; }
        public List<string> AbsenceTypes { get; set; }
        public Dictionary<User, List<CalendarDay>> AllUsersCalendarData { get; set; }
        public List<CalendarDay> CalendarDaysList { get; set; }
        public List<FilterItem> RoleFilter { get; set; }
        public List<FilterItem> DepartmentFilter { get; set; }
        public List<FilterItem> TeamFilter { get; set; }
        public string RoleFilterString { get; set; }
        public string DepartmentFilterString { get; set; }
        public string TeamFilterString { get; set; }
        public string PostBackActionName { get; set; }
    }
}
