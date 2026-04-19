using ITBSCareers.Models.Carriere;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ITBSCareers.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly CarriereDbContext _context;

        public AdminController(CarriereDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!await IsAlumniRequestsTableAvailableAsync())
            {
                ViewBag.Warning = "La table AlumniRequests est absente. Crée-la pour valider les demandes alumni.";
                return View(new List<AlumniRequest>());
            }

            var pendingRequests = await _context.AlumniRequests
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return View(pendingRequests);
        }

        private async Task<bool> IsAlumniRequestsTableAvailableAsync()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT TOP (1) 1 FROM [dbo].[AlumniRequests]");
                return true;
            }
            catch (SqlException ex) when (ex.Message.Contains("Invalid object name 'AlumniRequests'"))
            {
                return false;
            }
        }
    }
}
