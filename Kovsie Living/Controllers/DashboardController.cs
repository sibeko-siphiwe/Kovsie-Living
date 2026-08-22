using Microsoft.AspNetCore.Mvc;

namespace Kovsie_Living.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Users()
        {
            return View();
        }
        public IActionResult Officers()
        {
            return View();
        }
    }
}
