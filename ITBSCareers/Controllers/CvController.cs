using ITBSCareers.Models.Carriere;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    public class CvController : Controller
    {
        private readonly CarriereDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CvController(CarriereDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // POST: /Cv/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile cvFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "User");

            if (cvFile == null || cvFile.Length == 0)
            {
                TempData["CvError"] = "Please select a PDF file to upload.";
                return RedirectToAction("Profile", "User");
            }

            var ext = Path.GetExtension(cvFile.FileName).ToLowerInvariant();
            if (ext != ".pdf")
            {
                TempData["CvError"] = "Only PDF files are accepted.";
                return RedirectToAction("Profile", "User");
            }

            if (cvFile.Length > 5 * 1024 * 1024)
            {
                TempData["CvError"] = "File size must not exceed 5 MB.";
                return RedirectToAction("Profile", "User");
            }

            // Build storage path: wwwroot/uploads/cvs/{userId}/
            var userFolder = Path.Combine(_env.WebRootPath, "uploads", "cvs", userId.ToString());
            Directory.CreateDirectory(userFolder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(userFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await cvFile.CopyToAsync(stream);
            }

            // Relative URL stored in DB (served as static file)
            var relativePath = $"/uploads/cvs/{userId}/{fileName}";

            var cv = new Cv
            {
                UserId = userId.Value,
                FilePath = relativePath,
                UploadedAt = DateTime.Now
            };

            _context.Cvs.Add(cv);
            await _context.SaveChangesAsync();

            TempData["CvSuccess"] = "CV uploaded successfully.";
            return RedirectToAction("Profile", "User");
        }

        // POST: /Cv/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "User");

            var cv = await _context.Cvs.FirstOrDefaultAsync(c => c.Cvid == id && c.UserId == userId.Value);
            if (cv == null) return NotFound();

            // Delete physical file
            var fullPath = Path.Combine(_env.WebRootPath, cv.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _context.Cvs.Remove(cv);
            await _context.SaveChangesAsync();

            TempData["CvSuccess"] = "CV deleted.";
            return RedirectToAction("Profile", "User");
        }
    }
}
