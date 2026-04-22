using ITBSCareers.Models.Carriere;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSTCareers.Controllers
{
    [Authorize]
    public class CvController : Controller
    {
        private readonly CarriereDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IAuthorizationService _authorizationService;

        public CvController(CarriereDbContext context, IWebHostEnvironment env, IAuthorizationService authorizationService)
        {
            _context = context;
            _env = env;
            _authorizationService = authorizationService;
        }

        // GET: /Cv/Open/5
        [HttpGet]
        public async Task<IActionResult> Open(int id)
        {
            var cv = await _context.Cvs.FirstOrDefaultAsync(c => c.Cvid == id);
            if (cv == null)
            {
                return NotFound();
            }

            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null && int.TryParse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value, out var claimUserId))
            {
                currentUserId = claimUserId;
            }

            var isOwner = currentUserId.HasValue && cv.UserId == currentUserId.Value;
            if (!isOwner)
            {
                var authResult = await _authorizationService.AuthorizeAsync(User, null, "VerifiedAlumni");
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }
            }

            if (Uri.TryCreate(cv.FilePath, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return Redirect(cv.FilePath);
            }

            var relativePath = cv.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, relativePath);

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound("CV file not found on server. Add the file under wwwroot or set an absolute URL in CVs.FilePath.");
            }

            var fileName = Path.GetFileName(fullPath);
            return PhysicalFile(fullPath, "application/pdf", fileName, enableRangeProcessing: true);
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

            var existingCvs = await _context.Cvs.Where(c => c.UserId == userId.Value).ToListAsync();
            foreach (var existingCv in existingCvs)
            {
                if (!string.IsNullOrWhiteSpace(existingCv.FilePath) &&
                    !Uri.TryCreate(existingCv.FilePath, UriKind.Absolute, out _))
                {
                    var existingFullPath = Path.Combine(_env.WebRootPath, existingCv.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(existingFullPath))
                    {
                        System.IO.File.Delete(existingFullPath);
                    }
                }
            }
            _context.Cvs.RemoveRange(existingCvs);
            await _context.SaveChangesAsync();

            // Build storage path: wwwroot/uploads/cvs/{userId}/
            var userFolder = Path.Combine(_env.WebRootPath, "uploads", "cvs", userId.ToString());
            Directory.CreateDirectory(userFolder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(userFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await cvFile.CopyToAsync(stream);
            }

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
