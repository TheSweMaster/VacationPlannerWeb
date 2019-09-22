using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VacationPlannerWeb.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [ScaffoldColumn(false)]
        public string ReturnUrl { get; set; }
    }
}
