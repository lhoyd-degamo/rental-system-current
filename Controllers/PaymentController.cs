using CRUD.Data;
using CRUD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payment/Create?borrowId=5
        [HttpGet]
        public async Task<IActionResult> Create(int borrowId)
        {
            var borrow = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                        .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(b => b.BorrowID == borrowId);

            if (borrow == null)
            {
                return NotFound();
            }

            // Check if already paid
            var alreadyPaid = await _context.Payments
                .AnyAsync(p => p.BorrowID == borrowId);

            if (alreadyPaid)
            {
                TempData["Info"] = "This borrow has already been paid.";
                return RedirectToAction("Index", "Borrow");
            }

            // Calculate total amount
            ViewBag.Amount = borrow.BorrowItems
                .Sum(bi => bi.Item?.Amount ?? 0);

            return View(borrow);
        }

        // POST: Payment/ConfirmPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int borrowId)
        {
            var borrow = await _context.Borrows
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                .FirstOrDefaultAsync(b => b.BorrowID == borrowId);

            if (borrow == null)
            {
                return NotFound();
            }

            // Check if already paid
            var alreadyPaid = await _context.Payments
                .AnyAsync(p => p.BorrowID == borrowId);

            if (!alreadyPaid)
            {
                // Calculate total amount
                var amount = borrow.BorrowItems
                    .Sum(bi => bi.Item?.Amount ?? 0);

                var payment = new Payment
                {
                    BorrowID = borrowId,
                    PaymentMethod = "Cash",
                    AmountPaid = amount,
                    PaymentStatus = "Paid",
                    PaymentDate = DateTime.Now
                };

                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Payment recorded successfully.";

            return RedirectToAction("Index", "Borrow");
        }
    }
}