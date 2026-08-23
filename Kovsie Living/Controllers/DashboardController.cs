using Kovsie_Living.Data;
using Kovsie_Living.Data.Repositories;
using Kovsie_Living.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kovsie_Living.Controllers
{
    public class DashboardController : Controller
    { 
        private readonly IDashboardRepository _repository;
        public DashboardController(IDashboardRepository repository)
        { 
            _repository = repository;
        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                Users = _repository.GetAllCustomers(),
                Officers = _repository.GetAllOfficers(),
            };
            return View(model);
        }
        public IActionResult Customers()
        {
            var customers = _repository.GetAllCustomers();
            return View(customers);
        }
        public IActionResult Officers()
        {
            return View();
        }
    }
}
