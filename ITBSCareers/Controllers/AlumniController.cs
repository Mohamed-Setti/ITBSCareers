using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IBSTCareers.Controllers
{
    [Authorize(Policy = "VerifiedAlumni")]
    public class AlumniController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
