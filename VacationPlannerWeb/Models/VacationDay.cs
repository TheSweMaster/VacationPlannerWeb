using System;
using System.ComponentModel.DataAnnotations;

namespace VacationPlannerWeb.Models
{
    public class VacationDay
    {
        public int Id { get; set; }
        public int VacationBookingId { get; set; }
        public VacationBooking VacationBooking { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime VacationDate { get; set; }

    }
}
