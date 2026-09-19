using CRUD.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace crud.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin() =>
            HttpContext.Session.GetString("AdminUser") != null;

        // ADMIN: REPORT PAGE
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            // =========================
            // INVENTORY SUMMARY
            // =========================

            var totalItems = await _context.Items
                .SumAsync(i => i.Quantity);

            var availableItems = await _context.Items
                .Where(i => i.Status == "Available")
                .SumAsync(i => i.Quantity);

            var borrowedItems = await _context.Items
                .Where(i => i.Status == "Borrowed")
                .SumAsync(i => i.Quantity);

            var outOfStockItems = await _context.Items
                .CountAsync(i => i.Quantity <= 0);

            // =========================
            // TRANSACTION SUMMARY
            // =========================

            var totalBookings = await _context.Borrows
                .CountAsync();

            var bookedTransactions = await _context.Borrows
                .CountAsync(b => b.Status == "Booked");

            var borrowedTransactions = await _context.Borrows
                .CountAsync(b => b.Status == "Borrowed");

            var returnedTransactions = await _context.Borrows
                .CountAsync(b => b.Status == "Returned");

            // =========================
            // PAYMENT SUMMARY
            // =========================

            var totalPayments = await _context.Payments
                .SumAsync(p => (decimal?)p.AmountPaid) ?? 0m;

            var totalPaidTransactions = await _context.Payments
                .CountAsync(p => p.PaymentStatus == "Paid");

            // =========================
            // PENALTY SUMMARY
            // =========================

            var totalPenalty = await _context.Penalties
                .SumAsync(p => (decimal?)p.PenaltyAmount) ?? 0m;

            var totalLateReturns = await _context.Penalties
                .CountAsync(p => p.DaysLate > 0);

            // =========================
            // CATEGORY REPORT
            // =========================

            var categoryReport = await _context.Categories
                .Select(c => new
                {
                    CategoryName = c.CatName,

                    TotalItems = _context.Items
                        .Where(i => i.CatID == c.CatID)
                        .Sum(i => (int?)i.Quantity) ?? 0,

                    AvailableItems = _context.Items
                        .Where(i =>
                            i.CatID == c.CatID &&
                            i.Status == "Available")
                        .Sum(i => (int?)i.Quantity) ?? 0,

                    BorrowedItems = _context.Items
                        .Where(i =>
                            i.CatID == c.CatID &&
                            i.Status == "Borrowed")
                        .Sum(i => (int?)i.Quantity) ?? 0
                })
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            // =========================
            // SEND DATA TO VIEW
            // =========================

            ViewBag.TotalItems = totalItems;
            ViewBag.AvailableItems = availableItems;
            ViewBag.BorrowedItems = borrowedItems;
            ViewBag.OutOfStockItems = outOfStockItems;

            ViewBag.TotalBookings = totalBookings;
            ViewBag.BookedTransactions = bookedTransactions;
            ViewBag.BorrowedTransactions = borrowedTransactions;
            ViewBag.ReturnedTransactions = returnedTransactions;

            ViewBag.TotalPayments = totalPayments;
            ViewBag.TotalPaidTransactions = totalPaidTransactions;

            ViewBag.TotalPenalty = totalPenalty;
            ViewBag.TotalLateReturns = totalLateReturns;

            ViewBag.CategoryReport = categoryReport;

            return View();
        }
    }
}