using System;

namespace VacationPlannerWeb.ViewModels
{
    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public bool IsWeekend { get; set; }
        public bool IsHoliday { get; set; }
        public bool IsToday { get; set; }
        public bool IsStartOfWeek { get; set; }
        public int WeekNumber { get; set; }
        public bool IsPlannedVacation { get; set; }
        public string Approval { get; set; }
        public string Note { get; set; }
        public string AbsenceType { get; set; }

        public int VacationBookingId { get; set; }


    }
}
