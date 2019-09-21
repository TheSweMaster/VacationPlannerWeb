using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VacationPlannerWeb.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace VacationPlannerWeb.Services
{
    public class RolesService
    {
        private readonly AppDbContext _context;

        public RolesService(AppDbContext context)
        {
            _context = context;
        }

        public virtual async Task<IList<string>> GetRolesShorteningsAsync(string userId)
        {
            var rolesShortenings = _context.Roles.Join(_context.UserRoles.Where(x => x.UserId == userId),
                role => role.Id,
                userRole => userRole.RoleId,
                (role, userRole) => role.Shortening);

            return await rolesShortenings.ToListAsync();
        }
    }
}
