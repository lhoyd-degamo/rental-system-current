using CRUD.Data;
using CRUD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace CRUD.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin() =>
            HttpContext.Session.GetString("AdminUser") != null;

        // ADMIN PAYMENT LIST
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            var payments = await _context.Payments
                .Include(p => p.Borrow)
                    .ThenInclude(b => b.Customer)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }

        // ADMIN PAYMENT FORM
        [HttpGet]
        public async Task<IActionResult> Create(int borrowId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            var borrow = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                        .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(b => b.BorrowID == borrowId);

            if (borrow == null)
                return NotFound();

            var alreadyPaid = await _context.Payments.AnyAsync(p => p.BorrowID == borrowId);

            if (alreadyPaid)
            {
                TempData["Info"] = "This booking has already been paid.";
                return RedirectToAction(nameof(Receipt), new { borrowId });
            }

            ViewBag.Amount = borrow.BorrowItems.Sum(bi => bi.Item?.Amount ?? 0);
            return View(borrow);
        }

        // RECORD PHYSICAL PAYMENT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int borrowId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            var borrow = await _context.Borrows
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                .FirstOrDefaultAsync(b => b.BorrowID == borrowId);

            if (borrow == null)
                return NotFound();

            var alreadyPaid = await _context.Payments.AnyAsync(p => p.BorrowID == borrowId);

            if (!alreadyPaid)
            {
                var amount = borrow.BorrowItems.Sum(bi => bi.Item?.Amount ?? 0);

                // Online bookings do not reduce stock.
                // Reduce stock when the customer physically pays and the rental starts.
                if (borrow.Status == "Booked")
                {
                    foreach (var borrowItem in borrow.BorrowItems)
                    {
                        if (borrowItem.Item != null)
                        {
                            if (borrowItem.Item.Quantity <= 0)
                            {
                                TempData["Error"] = $"{borrowItem.Item.ItemName} is no longer available.";
                                return RedirectToAction(nameof(Index));
                            }

                            borrowItem.Item.Quantity -= 1;
                            borrowItem.Item.Status = borrowItem.Item.Quantity > 0 ? "Available" : "Borrowed";
                        }
                    }
                }

                _context.Payments.Add(new Payment
                {
                    BorrowID = borrowId,
                    PaymentMethod = "Cash",
                    AmountPaid = amount,
                    PaymentStatus = "Paid",
                    PaymentDate = DateTime.Now
                });

                // The physical rental starts after payment confirmation.
                borrow.Status = "Borrowed";

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Receipt), new { borrowId });
        }

        // PAYMENT RECEIPT
        [HttpGet]
        public async Task<IActionResult> Receipt(int borrowId)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            var borrow = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                        .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(b => b.BorrowID == borrowId);

            if (borrow == null)
                return NotFound();

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BorrowID == borrowId);

            if (payment == null)
                return RedirectToAction(nameof(Create), new { borrowId });

            ViewBag.Payment = payment;
            ViewBag.Amount = borrow.BorrowItems.Sum(bi => bi.Item?.Amount ?? 0);

            return View(borrow);
        }

        // ORDER INFORMATION PAGE USED BY QR CODE
        [HttpGet]
        public async Task<IActionResult> OrderQR(int id)
        {
            var borrow = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                        .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(b => b.BorrowID == id);

            if (borrow == null)
                return NotFound();

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BorrowID == id);

            ViewBag.Payment = payment;
            return View(borrow);
        }

        // QR IMAGE
        [HttpGet]
        public async Task<IActionResult> GenerateQR(int id)
        {
            var borrow = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                        .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(b => b.BorrowID == id);

            if (borrow == null)
                return NotFound();

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BorrowID == id);

            var lines = new List<string>
            {
                "PEARLS & MORE BRIDAL FASHION",
                $"Booking ID: #{borrow.BorrowID}",
                $"Customer: {borrow.Customer?.CustomerName}",
                $"Email: {borrow.Customer?.Email}",
                $"Phone: {borrow.Customer?.PhoneNumber}",
                $"Borrow Date: {borrow.BorrowDate:MMMM dd, yyyy}",
                $"Return Date: {borrow.ReturnDate:MMMM dd, yyyy}",
                $"Status: {borrow.Status}"
            };

            foreach (var bi in borrow.BorrowItems)
            {
                if (bi.Item != null)
                {
                    lines.Add($"Item: {bi.Item.ItemName}");
                    lines.Add($"Category: {bi.Item.Category?.CatName}");
                    lines.Add($"Size: {bi.Item.Size}");
                    lines.Add($"Price: PHP {bi.Item.Amount:N2}");
                }
            }

            var total = borrow.BorrowItems.Sum(bi => bi.Item?.Amount ?? 0);
            lines.Add($"Total: PHP {total:N2}");
            lines.Add($"Payment: {payment?.PaymentStatus ?? "Not yet paid"}");

            if (borrow.Status == "Returned")
            {
                var penalty = await _context.Penalties
                    .FirstOrDefaultAsync(p => p.BorrowID == borrow.BorrowID);

                if (penalty != null)
                {
                    lines.Add($"Late Days: {penalty.DaysLate}");
                    lines.Add($"Penalty: PHP {penalty.PenaltyAmount:N2}");
                }
            }

            var orderText = string.Join("\n", lines);

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(orderText, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(data);
            var bytes = qrCode.GetGraphic(10);

            return File(bytes, "image/png");
        }
    }
}
