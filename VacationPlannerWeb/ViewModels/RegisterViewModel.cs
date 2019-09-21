using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

        public List<string> Errors { get; set; }
    }
}
