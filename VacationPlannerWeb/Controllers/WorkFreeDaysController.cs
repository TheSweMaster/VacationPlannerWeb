using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using VacationPlannerWeb.DataAccess;
using VacationPlannerWeb.JsonModels;
using VacationPlannerWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace VacationPlannerWeb.Controllers
{
    [Authorize]
    public class WorkFreeDaysController : Controller
    {
        private static readonly HttpClient _client = new HttpClient();
        private readonly AppDbContext _context;

        public WorkFreeDaysController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.WorkFreeDays.OrderBy(w => w.Date).ToListAsync());
        }

        [Authorize]
        public async Task<IActionResult> ListAll()
        {
            return View(await _context.WorkFreeDays.OrderBy(w => w.Date).ToListAsync());
        }

        [Authorize(Roles = "Admin,Manager")]
        public IActionResult ImportSwedishWorkFreeDaysYearSelector()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public async Task<IActionResult> ImportSwedishWorkFreeDays(int year)
        {
            year = (year < DateTime.Now.AddYears(-1).Year || year > DateTime.Now.AddYears(2).Year) ? DateTime.Now.Year : year;

            var url = "https://api.dryg.net/dagar/v2.1/" + year;
            var responds = await _client.GetStringAsync(url);
            var svenskaDagar = SvenskaDagar.FromJson(responds);

            var notworkdays = svenskaDagar.Dagar.Where(d => d.Arbetsfri_dag == "Ja" && d.Veckodag != "Söndag" && d.Veckodag != "Lördag").ToList();

            var workFreeDays = new List<WorkFreeDay>();
            foreach (var dag in notworkdays)
            {
                workFreeDays.Add(
                    new WorkFreeDay
                    {
                        Date = DateTime.Parse(dag.Datum),
                        Name = dag.Helgdag,
                        Custom = false,
                    }
                );
            }
            return View(workFreeDays);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost, ActionName("ImportSwedishWorkFreeDays")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportSwedishWorkFreeDaysConfirmed(List<WorkFreeDay> workFreeDaysList)
        {
            if (!ModelState.IsValid)
            {
                return View(workFreeDaysList);
            }

            foreach (var day in workFreeDaysList)
            {
                if (!_context.WorkFreeDays.Any(w => w.Date == day.Date))
                {
                    _context.WorkFreeDays.Add(day);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workFreeDay = await _context.WorkFreeDays
                .SingleOrDefaultAsync(m => m.Id == id);
            if (workFreeDay == null)
            {
                return NotFound();
            }

            return View(workFreeDay);
        }

        [Authorize]
        public async Task<IActionResult> DetailsByDate(string date)
        {
            var workFreeDay = await _context.WorkFreeDays.FirstOrDefaultAsync(m => m.Date == DateTime.Parse(date));

            if (workFreeDay == null)
            {
                return NotFound();
            }

            return View(workFreeDay);
        }

        // GET: WorkFreeDaysTest/Create
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: WorkFreeDaysTest/Create
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Date,Custom")] WorkFreeDay workFreeDay)
        {
            if (ModelState.IsValid)
            {
                workFreeDay.Custom = true;
                _context.Add(workFreeDay);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(workFreeDay);
        }

        [Authorize(Roles = "Admin,Manager")]
        // GET: WorkFreeDaysTest/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workFreeDay = await _context.WorkFreeDays.SingleOrDefaultAsync(m => m.Id == id);
            if (workFreeDay == null)
            {
                return NotFound();
            }
            return View(workFreeDay);
        }

        // POST: WorkFreeDaysTest/Edit/5
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Date,Custom")] WorkFreeDay workFreeDay)
        {
            if (id != workFreeDay.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(workFreeDay);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkFreeDayExists(workFreeDay.Id))
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
            return View(workFreeDay);
        }

        // GET: WorkFreeDaysTest/Delete/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workFreeDay = await _context.WorkFreeDays
                .SingleOrDefaultAsync(m => m.Id == id);
            if (workFreeDay == null)
            {
                return NotFound();
            }

            return View(workFreeDay);
        }

        // POST: WorkFreeDaysTest/Delete/5
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var workFreeDay = await _context.WorkFreeDays.SingleOrDefaultAsync(m => m.Id == id);
            _context.WorkFreeDays.Remove(workFreeDay);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WorkFreeDayExists(int id)
        {
            return _context.WorkFreeDays.Any(e => e.Id == id);
        }

    }
}