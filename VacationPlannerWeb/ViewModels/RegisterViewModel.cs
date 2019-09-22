using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using VacationPlannerWeb.Models;

namespace VacationPlannerWeb.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DisplayName("First Name")]
        public string FirstName { get; set; }

        [Required]
        [DisplayName("Last Name")]
        public string LastName { get; set; }

        [DisplayName("Team")]
        public int? TeamId { get; set; }
        public Team Team { get; set; }

        [DisplayName("Department")]
        public int? DepartmentId { get; set; }
        public Department Department { get; set; }

        public List<string> Errors { get; set; }
    }
}
