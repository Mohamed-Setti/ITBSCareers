using IBSTCareers.Models;
using IBSTCareers.Models.Carriere;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    public class UserController : Controller
    {

        CarriereDbContext _context;

        public UserController(CarriereDbContext context)
        {
            _context = context;
        }
        // GET: UserController
        public ActionResult Index()
        {
            return View();
        }

        // GET: UserController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: UserController/Create
        public ActionResult Create()
        {
            var roles = _context.Roles.ToList();
            ViewBag.Roles = new SelectList(roles, "RoleId", "Name");

            return View();
        }

        // POST: UserController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(User user, int RoleId)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    user.CreatedAt = DateTime.Now;
                    _context.Users.Add(user);
                    _context.SaveChanges();

                    // Assign the selected role to the user
                    var userRole = new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = RoleId
                    };
                    _context.UserRoles.Add(userRole);
                    _context.SaveChanges();

                    return RedirectToAction(nameof(SelectSkillsInterests), new { id = user.UserId });
                }

                // If validation fails, reload the dropdown
                ViewBag.Roles = new SelectList(_context.Roles.ToList(), "RoleId", "Name");
                return View(user);
            }
            catch
            {
                ViewBag.Roles = new SelectList(_context.Roles.ToList(), "RoleId", "Name");
                return View(user);
            }
        }

        // GET: UserController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UserController/Edit/5
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

        // GET: UserController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UserController/Delete/5
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
        // GET: UserController/SelectSkillsInterests/5
        public ActionResult SelectSkillsInterests(int id)
        {
            var user = _context.Users
                .Include(u => u.Skills)
                .Include(u => u.Interests)
                .FirstOrDefault(u => u.UserId == id);

            if (user == null) return NotFound();

            var userSkillIds = user.Skills.Select(us => us.SkillId).ToList();
            var userInterestIds = user.Interests.Select(ui => ui.InterestId).ToList();

            var vm = new SelectSkillsInterestsViewModel
            {
                UserId = id,

                Skills = _context.Skills.Select(s => new CheckboxItem
                {
                    Id = s.SkillId,
                    Name = s.Name,
                    IsSelected = userSkillIds.Contains(s.SkillId)
                }).ToList(),

                Interests = _context.Interests.Select(i => new CheckboxItem
                {
                    Id = i.InterestId,
                    Name = i.Name,
                    IsSelected = userInterestIds.Contains(i.InterestId)
                }).ToList()
            };

            return View(vm);
        }

        // POST: UserController/SelectSkillsInterests
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SelectSkillsInterests(SelectSkillsInterestsViewModel vm)
        {
            var user = _context.Users
                .Include(u => u.Skills)
                .Include(u => u.Interests)
                .FirstOrDefault(u => u.UserId == vm.UserId);

            if (user == null) return NotFound();

            // Update skills
            user.Skills.Clear();
            var selectedSkillIds = vm.Skills
                .Where(x => x.IsSelected)
                .Select(x => x.Id)
                .ToList();

            var selectedSkills = _context.Skills
                .Where(s => selectedSkillIds.Contains(s.SkillId))
                .ToList();

            
            
            foreach (var skill in selectedSkills)
                user.Skills.Add(skill);

            // Update interests
            user.Interests.Clear();
            var selectedInterestIds = vm.Interests
                .Where(x => x.IsSelected)
                .Select(x => x.Id)
                .ToList();

            var selectedInterests = _context.Interests
                .Where(i => selectedInterestIds.Contains(i.InterestId))
                .ToList();

            foreach (var interest in selectedInterests)
                user.Interests.Add(interest);

            _context.SaveChanges();
            return RedirectToAction("Create", "Experience", new { userId = vm.UserId });
        }
    }
}
