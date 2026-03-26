using IBSTCareers.Models.Carriere;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    public class UserRoleController : Controller
    {
        CarriereDbContext _context;

        public UserRoleController(CarriereDbContext context)
        {
            _context = context;
        }
        // GET: UserRoleController
        public ActionResult Index()
        {
            return View();
        }

        // GET: UserRoleController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: UserRoleController/Create
        public ActionResult Create()
        {
            var roles = _context.Roles.ToList();

            // Crée SelectList pour le dropdown
           // ViewBag.Roles = new SelectList(roles, "RoleID", "Name");

            return View();
        }

        // POST: UserRoleController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // Récupérer la valeur sélectionnée depuis le form
                //int roleID = int.Parse(collection["RoleID"]);

                //Console.WriteLine($"Role sélectionné : {roleID}");

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                //var roles = _context.Roles.ToList();
                //ViewBag.Roles = new SelectList(roles, "RoleID", "Name");
                return View();
            }
        }

        // GET: UserRoleController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UserRoleController/Edit/5
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

        // GET: UserRoleController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UserRoleController/Delete/5
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
