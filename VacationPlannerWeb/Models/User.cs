﻿using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace VacationPlannerWeb.Models
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public int? TeamId { get; set; }
        [NotMapped]
        public Team Team { get; set; }
        public int? DepartmentId { get; set; }
        [NotMapped]
        public Department Department { get; set; }
        [NotMapped]
        public ICollection<Role> Roles { get; set; }
        public bool IsHidden { get; set; }
        public string ManagerUserId { get; set; }
        [NotMapped]
        public User ManagerUser { get; set; }
    }
}
