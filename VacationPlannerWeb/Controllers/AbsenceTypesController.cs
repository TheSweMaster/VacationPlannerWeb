using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationPlannerWeb.DataAccess;
using VacationPlannerWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace VacationPlannerWeb.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AbsenceTypesController : Controller
    {
        private readonly AppDbContext _context;

        public AbsenceTypesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AbsenceTypes
        public async Task<IActionResult> Index()
        {
            return View(await _context.AbsenceTypes.ToListAsync());
        }

        // GET: AbsenceTypes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var absenceType = await _context.AbsenceTypes
                .SingleOrDefaultAsync(m => m.Id == id);
            if (absenceType == null)
            {
                return NotFound();
            }

            return View(absenceType);
        }

        // GET: AbsenceTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AbsenceTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] AbsenceType absenceType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(absenceType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(absenceType);
        }

        // GET: AbsenceTypes/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var absenceType = await _context.AbsenceTypes.SingleOrDefaultAsync(m => m.Id == id);
            if (absenceType == null)
            {
                return NotFound();
            }
            return View(absenceType);
        }

        // POST: AbsenceTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] AbsenceType absenceType)
        {
            if (id != absenceType.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(absenceType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AbsenceTypeExists(absenceType.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(absenceType);
        }

        // GET: AbsenceTypes/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var absenceType = await _context.AbsenceTypes
                .SingleOrDefaultAsync(m => m.Id == id);
            if (absenceType == null)
            {
                return NotFound();
            }

            return View(absenceType);
        }

        // POST: AbsenceTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var absenceType = await _context.AbsenceTypes.SingleOrDefaultAsync(m => m.Id == id);
            _context.AbsenceTypes.Remove(absenceType);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AbsenceTypeExists(int id)
        {
            return _context.AbsenceTypes.Any(e => e.Id == id);
        }
    }
}
