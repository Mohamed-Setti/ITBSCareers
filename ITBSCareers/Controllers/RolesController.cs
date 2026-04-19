using ITBSCareers.Models.Carriere;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RolesController : Controller
    {
        private readonly CarriereDbContext _context;

        public RolesController(CarriereDbContext context)
        {
            _context = context;
        }

        // GET: RolesController
        public async Task<ActionResult> Index()
        {
            var roles = await _context.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View(roles);
        }

        // GET: RolesController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == id);
            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        // GET: RolesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RolesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Role role)
        {
            if (!ModelState.IsValid)
            {
                return View(role);
            }

            role.Name = role.Name.Trim();

            var exists = await _context.Roles.AnyAsync(r => r.Name == role.Name);
            if (exists)
            {
                ModelState.AddModelError(nameof(Role.Name), "This role already exists.");
                return View(role);
            }

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: RolesController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        // POST: RolesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, Role role)
        {
            if (id != role.RoleId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(role);
            }

            role.Name = role.Name.Trim();
            var duplicate = await _context.Roles.AnyAsync(r => r.RoleId != id && r.Name == role.Name);
            if (duplicate)
            {
                ModelState.AddModelError(nameof(Role.Name), "Another role already uses this name.");
                return View(role);
            }

            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: RolesController/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == id);
            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        // POST: RolesController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var used = await _context.UserRoles.AnyAsync(ur => ur.RoleId == id);
            if (used)
            {
                ModelState.AddModelError(string.Empty, "Cannot delete a role that is already assigned to users.");
                return View("Delete", role);
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
