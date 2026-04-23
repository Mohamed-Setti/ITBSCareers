using ITBSCareers.Models.Carriere;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    [Authorize]
    public class ExperienceController : Controller
    {
        CarriereDbContext _context;
        public ExperienceController(CarriereDbContext context)
        {
            _context = context;
        }

        // GET: ExperienceController
        public ActionResult Index()
        {
            return View();
        }

        // GET: ExperienceController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ExperienceController/Create
        public ActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var experiences = _context.Experiences
                .Where(e => e.UserId == userId.Value)
                .OrderByDescending(e => e.StartDate)
                .ToList();

            ViewBag.Experiences = experiences;

            return View(new Experience { UserId = userId.Value });
        }

        // POST: ExperienceController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Experience exp, string action)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            exp.UserId = userId.Value;
            exp.Title = exp.Title?.Trim();
            exp.Company = exp.Company?.Trim();
            exp.Description = exp.Description?.Trim();

            if (string.IsNullOrWhiteSpace(exp.Title))
            {
                ModelState.AddModelError(nameof(Experience.Title), "Title is required.");
            }

            if (string.IsNullOrWhiteSpace(exp.Company))
            {
                ModelState.AddModelError(nameof(Experience.Company), "Company is required.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Experiences = _context.Experiences
                    .Where(e => e.UserId == userId.Value)
                    .OrderByDescending(e => e.StartDate)
                    .ToList();

                return View(exp);
            }

            _context.Experiences.Add(exp);
            _context.SaveChanges();

            if (action == "dashboard")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return RedirectToAction(nameof(Create));
        }

        // GET: ExperienceController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ExperienceController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ExperienceController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ExperienceController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
