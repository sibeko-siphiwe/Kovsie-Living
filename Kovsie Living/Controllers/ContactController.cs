using Kovsie_Living.Data;
using Kovsie_Living.Models;
using Kovsie_Living.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kovsie_Living.Controllers
{
    public class ContactController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContactController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ContactError"] = "Please fix the errors below and try again.";
                // Return the model so validation messages could be shown if you
                // redirect back — but since we redirect to Home, use TempData.
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var contactMessage = new ContactMessage
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Subject = model.Subject.Trim(),
                Message = model.Message.Trim(),
                SubmittedAt = DateTime.UtcNow
            };

            // Link to logged-in user if applicable
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    contactMessage.UserId = user.Id;
                }
            }

            _context.ContactMessages.Add(contactMessage);
            await _context.SaveChangesAsync();

            TempData["ContactSuccess"] = "Thank you! Your message has been sent successfully. We'll get back to you soon.";
            return RedirectToAction("Index", "Home", new { area = "", anchor = "contact" });
        }
    }
}