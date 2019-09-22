using System;
using System.ComponentModel.DataAnnotations;

namespace VacationPlannerWeb.Models
{
    public class WorkFreeDay
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [DataType(DataType.Text)]
        public string Name { get; set; }
        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime Date { get; set; }
        public bool Custom { get; set; }
    }
}
