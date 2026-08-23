using Kovsie_Living.Data;
using Kovsie_Living.Models;
using Kovsie_Living.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kovsie_Living.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class TariffController : Controller
    {
        private readonly AppDbContext _context;

        public TariffController(AppDbContext context)
        {
            _context = context;
        } 

        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateTariffViewModel();
            return View(model);
        }
         
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTariffViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                { 
                    var exists = await _context.Tariffs
                        .AnyAsync(t => t.Name.ToLower() == model.Name.Trim().ToLower());

                    if (exists)
                    {
                        ModelState.AddModelError("", "A tariff with this name already exists.");
                        return View(model);
                    }

                    var tariff = new Tariff
                    {
                        Name = model.Name.Trim(),
                        Amount = model.Amount,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Tariffs.Add(tariff);
                    await _context.SaveChangesAsync();

                    TempData["message"] = $"Tariff '{tariff.Name}' created successfully!";
                    TempData["messageType"] = "success";
                    return RedirectToAction("Tariffs", "Dashboard");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }
            }
            return View(model);
        }
         
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var tariff = await _context.Tariffs.FindAsync(id);
            if (tariff == null)
            {
                TempData["message"] = "Tariff not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Tariffs", "Dashboard");
            }

            var model = new EditTariffViewModel
            {
                TariffId = tariff.TariffId,
                Name = tariff.Name,
                Amount = tariff.Amount
            };

            return View(model);
        }
         
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditTariffViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tariff = await _context.Tariffs.FindAsync(model.TariffId);
                    if (tariff == null)
                    {
                        TempData["message"] = "Tariff not found";
                        TempData["messageType"] = "danger";
                        return RedirectToAction("Tariffs", "Dashboard");
                    }

                    // Check if name is changed and already exists
                    if (tariff.Name.ToLower() != model.Name.Trim().ToLower())
                    {
                        var exists = await _context.Tariffs
                            .AnyAsync(t => t.Name.ToLower() == model.Name.Trim().ToLower() && t.TariffId != model.TariffId);

                        if (exists)
                        {
                            ModelState.AddModelError("", "A tariff with this name already exists.");
                            return View(model);
                        }
                    }

                    tariff.Name = model.Name.Trim();
                    tariff.Amount = model.Amount;
                    tariff.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    TempData["message"] = $"Tariff '{tariff.Name}' updated successfully!";
                    TempData["messageType"] = "success";
                    return RedirectToAction("Tariffs", "Dashboard");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }
            }
            return View(model);
        }
         
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tariff = await _context.Tariffs.FindAsync(id);
            if (tariff == null)
            {
                TempData["message"] = "Tariff not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Tariffs", "Dashboard");
            }

            _context.Tariffs.Remove(tariff);
            await _context.SaveChangesAsync();

            TempData["message"] = $"Tariff '{tariff.Name}' deleted successfully!";
            TempData["messageType"] = "success";
            return RedirectToAction("Tariffs", "Dashboard");
        }
    }
}
