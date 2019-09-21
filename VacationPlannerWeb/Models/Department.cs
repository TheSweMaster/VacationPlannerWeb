using System.ComponentModel.DataAnnotations;

namespace VacationPlannerWeb.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

        [Required]
        [StringLength(5)]
        public string Shortening { get; set; }
    }
}
