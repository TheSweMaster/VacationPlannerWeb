using VacationPlannerWeb.Models;
using System.Collections.Generic;

namespace VacationPlannerWeb.ViewModels
{
    public class AccountViewModel
    {
        public IEnumerable<User> Users { get; set; }
        public Dictionary<string, List<User>> UserRoleDictionary { get; set; }
        public bool ShowHidden { get; set; }

    }
}
