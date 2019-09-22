using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VacationPlannerWeb.Models
{
    public class AbsenceType
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public ICollection<VacationBooking> VacationBookings { get; set; }
    }
}