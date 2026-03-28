using IBSTCareers.Models;
using ITBSCareers.Models.Carriere;
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
            return RedirectToAction("Login", "User");
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
                    HttpContext.Session.SetInt32("UserId", user.UserId);

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

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {

            var user = _context.Users
                .FirstOrDefault(u => u.Email == email && u.PasswordHash == password);

            if (user != null)
            {
                
                HttpContext.Session.SetInt32("UserId", user.UserId);

                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Invalid email or password";
            return View();
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
            // Load user with join tables
            var user = _context.Users
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.UserInterests)
                    .ThenInclude(ui => ui.Interest)
                .FirstOrDefault(u => u.UserId == id);

            if (user == null) return NotFound();

            // Get IDs of already selected skills/interests
            var userSkillIds = user.UserSkills.Select(us => us.SkillId).ToList();
            var userInterestIds = user.UserInterests.Select(ui => ui.InterestId).ToList();

            // Build view model
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
            Console.WriteLine($"---------------------SelectSkillsInterests : Received VM for user {vm.UserId}");

            // Load the user without multiple Includes (load join tables separately)
            var user = _context.Users.FirstOrDefault(u => u.UserId == vm.UserId);
            if (user == null) return NotFound();

            // --- Update skills ---
            var existingSkills = _context.UserSkills.Where(us => us.UserId == user.UserId).ToList();
            _context.UserSkills.RemoveRange(existingSkills);

            var selectedSkillIds = vm.Skills
                .Where(x => x.IsSelected)
                .Select(x => x.Id)
                .ToList();
            Console.WriteLine($"Adding skill to user {user.UserId}");
            foreach (var skillId in selectedSkillIds)
            {
                Console.WriteLine($"Adding skill {skillId} to user {user.UserId}");
                _context.UserSkills.Add(new UserSkill
                {
                    UserId = user.UserId,
                    SkillId = skillId
                });
            }

            // --- Update interests ---
            var existingInterests = _context.UserInterests.Where(ui => ui.UserId == user.UserId).ToList();
            _context.UserInterests.RemoveRange(existingInterests);

            var selectedInterestIds = vm.Interests
                .Where(x => x.IsSelected)
                .Select(x => x.Id)
                .ToList();

            foreach (var interestId in selectedInterestIds)
            {
                Console.WriteLine($"Adding interest {interestId} to user {user.UserId}");
                _context.UserInterests.Add(new UserInterest
                {
                    UserId = user.UserId,
                    InterestId = interestId
                });
            }

            _context.SaveChanges();

            return RedirectToAction("Create", "Experience", new { userId = user.UserId });
        }
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "User");

            var user = _context.Users
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.UserInterests)
                    .ThenInclude(ui => ui.Interest)
                .Include(u => u.Experiences)
                .FirstOrDefault(u => u.UserId == userId.Value);

            if (user == null) return NotFound();

            // Debug printing
            Console.WriteLine($"User: {user.FullName} ({user.Email})");
            Console.WriteLine("Skills:");
            foreach (var us in user.UserSkills)
                Console.WriteLine($" - {us.Skill.Name}");

            Console.WriteLine("Interests:");
            foreach (var ui in user.UserInterests)
                Console.WriteLine($" - {ui.Interest.Name}");

            Console.WriteLine("Experiences:");
            foreach (var exp in user.Experiences)
                Console.WriteLine($" - {exp.Title} at {exp.Company} ({exp.StartDate?.ToShortDateString()} - {exp.EndDate?.ToShortDateString()})\n   Description: {exp.Description}");

            return View(user);
        }
    }
}
