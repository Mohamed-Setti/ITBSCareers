using IBSTCareers.Models.Carriere;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
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
        public ActionResult Create(int userId)
        {
            var experiences = _context.Experiences
                .Where(e => e.UserId == userId)
                .ToList();

            ViewBag.Experiences = experiences;

            return View(new Experience { UserId = userId });
        }

        // POST: ExperienceController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Experience exp)
        {
            if (ModelState.IsValid)
            {
                _context.Experiences.Add(exp);
                _context.SaveChanges();

                // stay on same page to add more
                return RedirectToAction("Create", new { userId = exp.UserId });
            }

            return View(exp);
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
