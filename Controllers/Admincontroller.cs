using CRUD.Data;
using crud.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<UserModel> _passwordHasher;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<UserModel>();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("AdminUser") != null)
                return RedirectToAction(nameof(Dashboard));

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var admin = await _context.Users.FirstOrDefaultAsync(u =>
                u.UserName == username && u.UserRole == "Admin");

            if (admin == null)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            var result = _passwordHasher.VerifyHashedPassword(
                admin, admin.Password, password);

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            HttpContext.Session.SetString("AdminUser", admin.UserName);
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Dashboard()
        {
            if (HttpContext.Session.GetString("AdminUser") == null)
                return RedirectToAction(nameof(Login));

            ViewBag.ItemCount = await _context.Items.CountAsync();
            ViewBag.CategoryCount = await _context.Categories.CountAsync();
            ViewBag.CustomerCount = await _context.Customers.CountAsync();
            ViewBag.BorrowCount = await _context.Borrows.CountAsync();
            ViewBag.PaymentCount = await _context.Payments.CountAsync();
            ViewBag.BookedCount = await _context.Borrows.CountAsync(b => b.Status == "Booked");
            ViewBag.BorrowedCount = await _context.Borrows.CountAsync(b => b.Status == "Borrowed");
            ViewBag.ReturnedCount = await _context.Borrows.CountAsync(b => b.Status == "Returned");
            ViewBag.LowStockCount = await _context.Items.CountAsync(i => i.Quantity > 0 && i.Quantity <= 2);
            ViewBag.CategoryInventory = await _context.Categories
                .OrderBy(c => c.CatName)
                .Select(c => new
                {
                    c.CatName,
                    Count = _context.Items.Count(i => i.CatID == c.CatID)
                })
                .ToListAsync();
            ViewBag.AdminName = HttpContext.Session.GetString("AdminUser");

            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("AdminUser") == null)
                return RedirectToAction(nameof(Login));

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string currentPassword,
            string newPassword,
            string confirmPassword)
        {
            if (HttpContext.Session.GetString("AdminUser") == null)
                return RedirectToAction(nameof(Login));

            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Please fill in all fields.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New passwords do not match.";
                return View();
            }

            if (newPassword.Length < 6)
            {
                ViewBag.Error = "New password must be at least 6 characters.";
                return View();
            }

            var username = HttpContext.Session.GetString("AdminUser")!;
            var admin = await _context.Users.FirstOrDefaultAsync(u =>
                u.UserName == username && u.UserRole == "Admin");

            if (admin == null)
                return RedirectToAction(nameof(Login));

            var result = _passwordHasher.VerifyHashedPassword(
                admin, admin.Password, currentPassword);

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Current password is incorrect.";
                return View();
            }

            admin.Password = _passwordHasher.HashPassword(admin, newPassword);
            await _context.SaveChangesAsync();

            HttpContext.Session.Clear();
            TempData["Success"] = "Password changed successfully. Please login again.";
            return RedirectToAction(nameof(Login));
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
