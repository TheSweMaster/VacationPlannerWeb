using Microsoft.AspNetCore.Authorization;

namespace VacationPlannerWeb
{
    public class IsInRoleRequirement : IAuthorizationRequirement
    {
        public string Roles { get; private set; }

        public IsInRoleRequirement(string roles)
        {
            Roles = roles;
        }
    }
}