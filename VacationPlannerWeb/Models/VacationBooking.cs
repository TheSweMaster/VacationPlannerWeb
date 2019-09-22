using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using VacationPlannerWeb.Validations;

namespace VacationPlannerWeb.Models
{
    public class VacationBooking
    {
        public VacationBooking()
        {
            VacationDays = new HashSet<VacationDay>();
        }
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        [FromBookingDateValidations(2, 12)]
        public DateTime FromDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        [ToBookingDateValidation(2, 12)]
        public DateTime ToDate { get; set; }
        public int? AbsenceTypeId { get; set; }
        public AbsenceType AbsenceType { get; set; }
        public string Approval { get; set; }

        [DataType(DataType.Text)]
        public string Comment { get; set; }
        public ICollection<VacationDay> VacationDays { get; set; }
    }

    public enum ApprovalState
    {
        Pending,
        Approved,
        Denied,
    }
}
