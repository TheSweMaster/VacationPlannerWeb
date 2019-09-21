using VacationPlannerWeb.Models;
using System;
using System.Collections.Generic;

namespace VacationPlannerWeb.ViewModels
{
    public class CalendarViewModel
    {
        public DateTime Date { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public IEnumerable<string> DayOfWeekList { get; set; }
        public List<string> AbsenceTypes { get; set; }
        public IEnumerable<VacationDay> VacationDays { get; set; }
        public IEnumerable<VacationBooking> VacationBookings { get; set; }
        public Dictionary<int, List<CalendarDay>> WeekCalendarData { get; set; }
        public string PostBackActionName { get; set; }
        public string YearAsString
        {
            get { return Date.ToString("yyyy"); }
        }
        public string MonthAsString
        {
            get { return Date.ToString("MMMM"); }
        }

    }
}
