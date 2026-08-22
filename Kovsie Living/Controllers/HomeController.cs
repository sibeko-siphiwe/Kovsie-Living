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
    }
}
