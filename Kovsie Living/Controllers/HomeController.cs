using System.Diagnostics; 
using Microsoft.AspNetCore.Mvc;

namespace Kovsie_Living.Controllers
{
    public class HomeController : Controller
    { 

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            ViewData["RequestId"] = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }
    }
}
