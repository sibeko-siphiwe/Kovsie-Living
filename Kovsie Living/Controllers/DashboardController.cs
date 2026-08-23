using Kovsie_Living.Data;
using Kovsie_Living.Data.Repositories;
using Kovsie_Living.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
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
            var users = _repository.GetAllCustomers().ToList();
            var officers = _repository.GetAllOfficers().ToList();
            var tariffs = _repository.GetActiveTariffs().ToList();

            // Calculate user status counts for the chart
            var activeUsers = users.Count(u => u.IsActive);
            var inactiveUsers = users.Count(u => !u.IsActive);

            // Calculate approval status counts for the chart
            var approvedUsers = users.Count(u =>
                (u.Student != null && u.Student.IsApproved) ||
                (u.Staff != null && u.Staff.IsApproved)
            );
            var pendingUsers = users.Count(u =>
                (u.Student != null && !u.Student.IsApproved) ||
                (u.Staff != null && !u.Staff.IsApproved)
            );

            var model = new DashboardViewModel
            {
                Users = users,
                Officers = officers,
                Tariffs = tariffs,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                ApprovedUsers = approvedUsers,
                PendingUsers = pendingUsers
            };

            return View(model);
        }

        public IActionResult Customers(string searchTerm, string userTypeFilter, string statusFilter, int page = 1)
        { 
            var customers = _repository.GetAllCustomers().AsQueryable();
             
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower().Trim();
                customers = customers.Where(u =>
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(searchTerm)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchTerm))
                );
            }
             
            if (!string.IsNullOrEmpty(userTypeFilter))
            {
                if (userTypeFilter == "Student")
                {
                    customers = customers.Where(u => u.Student != null);
                }
                else if (userTypeFilter == "Staff")
                {
                    customers = customers.Where(u => u.Staff != null);
                }
            }
             
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "Active")
                {
                    customers = customers.Where(u => u.IsActive &&
                        ((u.Student != null && u.Student.IsApproved) ||
                         (u.Staff != null && u.Staff.IsApproved)));
                }
                else if (statusFilter == "Inactive")
                {
                    customers = customers.Where(u => !u.IsActive);
                }
                else if (statusFilter == "Pending")
                {
                    customers = customers.Where(u => u.IsActive &&
                        ((u.Student != null && !u.Student.IsApproved) ||
                         (u.Staff != null && !u.Staff.IsApproved)));
                }
            }
             
            int pageSize = 10;
            int totalCount = customers.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedCustomers = customers
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
             
            ViewBag.SearchTerm = searchTerm;
            ViewBag.UserTypeFilter = userTypeFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;

            return View(pagedCustomers);
        }

        public IActionResult Officers(string searchTerm, string statusFilter)
        {
            var officers = _repository.GetAllOfficers().AsQueryable();
             
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower().Trim();
                officers = officers.Where(o =>
                    (o.FirstName != null && o.FirstName.ToLower().Contains(searchTerm)) ||
                    (o.LastName != null && o.LastName.ToLower().Contains(searchTerm)) ||
                    (o.UserName != null && o.UserName.ToLower().Contains(searchTerm)) ||
                    (o.Email != null && o.Email.ToLower().Contains(searchTerm)) ||
                    (o.FinancialOfficer != null && o.FinancialOfficer.EmployeeNumber.ToLower().Contains(searchTerm))
                );
            }
             
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "Active")
                {
                    officers = officers.Where(o => o.IsActive);
                }
                else if (statusFilter == "Inactive")
                {
                    officers = officers.Where(o => !o.IsActive);
                }
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusFilter = statusFilter;

            return View(officers.ToList());
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Tariffs()
        {
            var tariffs = _repository.GetActiveTariffs();
            return View(tariffs);
        }
    }
}
