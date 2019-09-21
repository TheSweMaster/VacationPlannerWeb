using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VacationPlannerWeb.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [DisplayName("User Name")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [ScaffoldColumn(false)]
        public string ReturnUrl { get; set; }
    }
}
