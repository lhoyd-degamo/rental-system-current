using CRUD.Data;
using CRUD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Controllers
{
    public class BorrowController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BorrowController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin() =>
            HttpContext.Session.GetString("AdminUser") != null;

        private bool IsCustomer() =>
            HttpContext.Session.GetInt32("CustomerID") != null;

        // ADMIN: booking/rental list
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            var borrows = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                        .ThenInclude(i => i.Category)
                .OrderByDescending(b => b.BorrowID)
                .ToListAsync();

            return View(borrows);
        }

        // ADMIN: available items for physical walk-in rental
        public async Task<IActionResult> ItemBorrow()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            var items = await _context.Items
                .Include(i => i.Category)
                .Where(i => i.Status == "Available" && i.Quantity > 0)
                .OrderBy(i => i.ItemName)
                .ToListAsync();

            return View(items);
        }

        // ADMIN: create physical rental
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            await LoadAvailableItems();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Borrow borrow)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            ModelState.Remove("ItemID");
            ModelState.Remove("Item");
            ModelState.Remove("Customer");

            if (borrow.SelectedItemIDs == null ||
                borrow.SelectedItemIDs.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please add at least one item.");
            }

            if (string.IsNullOrWhiteSpace(borrow.NewCustomerName))
                ModelState.AddModelError(
                    "NewCustomerName",
                    "Customer name is required.");

            if (string.IsNullOrWhiteSpace(borrow.NewCustomerEmail))
                ModelState.AddModelError(
                    "NewCustomerEmail",
                    "Customer email is required.");

            if (string.IsNullOrWhiteSpace(borrow.NewCustomerPhone))
                ModelState.AddModelError(
                    "NewCustomerPhone",
                    "Customer phone number is required.");

            if (string.IsNullOrWhiteSpace(borrow.NewCustomerAddress))
                ModelState.AddModelError(
                    "NewCustomerAddress",
                    "Customer address is required.");

            if (string.IsNullOrWhiteSpace(borrow.NewCustomerIDType))
                ModelState.AddModelError(
                    "NewCustomerIDType",
                    "ID type is required.");

            if (string.IsNullOrWhiteSpace(borrow.NewCustomerIDNumber))
                ModelState.AddModelError(
                    "NewCustomerIDNumber",
                    "ID number is required.");

            if (borrow.BorrowDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    "BorrowDate",
                    "Borrow date cannot be a future date.");
            }

            if (borrow.ReturnDate.Date < borrow.BorrowDate.Date)
            {
                ModelState.AddModelError(
                    "ReturnDate",
                    "Return date cannot be earlier than the borrow date.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAvailableItems();
                return View(borrow);
            }

            var selectedIDs = (borrow.SelectedItemIDs ??
                               new List<int>())
                .Distinct()
                .ToList();

            var selectedItems = await _context.Items
                .Where(i => selectedIDs.Contains(i.ItemID))
                .ToListAsync();

            if (selectedItems.Count != selectedIDs.Count)
            {
                ModelState.AddModelError(
                    "",
                    "One or more selected items could not be found.");

                await LoadAvailableItems();
                return View(borrow);
            }

            if (selectedItems.Any(i =>
                i.Quantity <= 0 ||
                i.Status != "Available"))
            {
                ModelState.AddModelError(
                    "",
                    "One or more selected items are no longer available.");

                await LoadAvailableItems();
                return View(borrow);
            }

            // Find existing customer using email
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.Email == borrow.NewCustomerEmail);

            if (customer == null)
            {
                customer = new Customer
                {
                    CustomerName = borrow.NewCustomerName,
                    Email = borrow.NewCustomerEmail,
                    PhoneNumber = borrow.NewCustomerPhone,
                    Address = borrow.NewCustomerAddress,
                    IDType = borrow.NewCustomerIDType,
                    IDNumber = borrow.NewCustomerIDNumber
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }
            else
            {
                customer.CustomerName =
                    borrow.NewCustomerName;

                customer.PhoneNumber =
                    borrow.NewCustomerPhone;

                customer.Address =
                    borrow.NewCustomerAddress;

                customer.IDType =
                    borrow.NewCustomerIDType;

                customer.IDNumber =
                    borrow.NewCustomerIDNumber;

                await _context.SaveChangesAsync();
            }

            borrow.CustomerID = customer.CustomerID;
            borrow.Status = "Borrowed";
            borrow.ItemID = selectedItems.First().ItemID;
            borrow.Quantity = 1;

            _context.Borrows.Add(borrow);
            await _context.SaveChangesAsync();

            foreach (var item in selectedItems)
            {
                _context.BorrowItems.Add(new BorrowItem
                {
                    BorrowID = borrow.BorrowID,
                    ItemID = item.ItemID
                });

                item.Quantity -= 1;

                item.Status = item.Quantity > 0
                    ? "Available"
                    : "Borrowed";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Create",
                "Payment",
                new { borrowId = borrow.BorrowID });
        }

        // CUSTOMER: booking page for one selected item
        [HttpGet]
        public async Task<IActionResult> Book(int itemId)
        {
            if (!IsCustomer())
                return RedirectToAction("Customer", "Home");

            var item = await _context.Items
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.ItemID == itemId);

            if (item == null)
                return NotFound();

            if (item.Quantity <= 0 ||
                item.Status != "Available")
            {
                TempData["Error"] =
                    "This item is currently unavailable.";

                return RedirectToAction(
                    "CustomerDashboard",
                    "Home");
            }

            ViewBag.Item = item;

            return View(new Borrow
            {
                ItemID = item.ItemID,
                BorrowDate = DateTime.Today,
                ReturnDate = DateTime.Today.AddDays(1)
            });
        }

        // CUSTOMER: save online booking only.
        // Stock is NOT reduced here.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(Borrow borrow)
        {
            if (!IsCustomer())
                return RedirectToAction("Customer", "Home");

            var customerID =
                HttpContext.Session.GetInt32("CustomerID");

            if (customerID == null)
                return RedirectToAction("Customer", "Home");

            ModelState.Remove("Item");
            ModelState.Remove("Customer");

            if (borrow.BorrowDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "BorrowDate",
                    "Borrow date cannot be in the past.");
            }

            if (borrow.ReturnDate.Date < borrow.BorrowDate.Date)
            {
                ModelState.AddModelError(
                    "ReturnDate",
                    "Return date cannot be earlier than the borrow date.");
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.ItemID == borrow.ItemID);

            if (item == null)
                return NotFound();

            if (item.Quantity <= 0 ||
                item.Status != "Available")
            {
                ModelState.AddModelError(
                    "",
                    "This item is currently unavailable.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Item = item;
                return View(borrow);
            }

            borrow.CustomerID = customerID.Value;
            borrow.Status = "Booked";
            borrow.Quantity = 1;
            borrow.ItemID = item.ItemID;

            _context.Borrows.Add(borrow);
            await _context.SaveChangesAsync();

            _context.BorrowItems.Add(new BorrowItem
            {
                BorrowID = borrow.BorrowID,
                ItemID = item.ItemID
            });

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Booking #{borrow.BorrowID} was submitted successfully.";

            return RedirectToAction(
                "CustomerDashboard",
                "Home");
        }

        // ADMIN: edit booking/rental
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            if (id == null)
                return NotFound();

            var borrow = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item!)
                .FirstOrDefaultAsync(b => b.BorrowID == id);

            if (borrow == null)
                return NotFound();

            if (borrow.Customer != null)
            {
                borrow.NewCustomerName =
                    borrow.Customer.CustomerName;

                borrow.NewCustomerEmail =
                    borrow.Customer.Email;

                borrow.NewCustomerPhone =
                    borrow.Customer.PhoneNumber;

                borrow.NewCustomerAddress =
                    borrow.Customer.Address;

                borrow.NewCustomerIDType =
                    borrow.Customer.IDType;

                borrow.NewCustomerIDNumber =
                    borrow.Customer.IDNumber;
            }

            borrow.SelectedItemIDs = borrow.BorrowItems
                .Select(bi => bi.ItemID)
                .ToList();

            await LoadItemsForEdit(
                borrow.SelectedItemIDs);

            return View(borrow);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Borrow borrow)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            if (id != borrow.BorrowID)
                return NotFound();

            ModelState.Remove("ItemID");
            ModelState.Remove("Item");
            ModelState.Remove("Customer");

            if (borrow.SelectedItemIDs == null ||
                borrow.SelectedItemIDs.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one item.");
            }

            if (string.IsNullOrWhiteSpace(borrow.NewCustomerName))
            {
                ModelState.AddModelError(
                    "NewCustomerName",
                    "Customer name is required.");
            }

            if (string.IsNullOrWhiteSpace(borrow.NewCustomerEmail))
            {
                ModelState.AddModelError(
                    "NewCustomerEmail",
                    "Customer email is required.");
            }

            if (string.IsNullOrWhiteSpace(borrow.NewCustomerPhone))
            {
                ModelState.AddModelError(
                    "NewCustomerPhone",
                    "Customer phone number is required.");
            }

            if (string.IsNullOrWhiteSpace(borrow.NewCustomerAddress))
            {
                ModelState.AddModelError(
                    "NewCustomerAddress",
                    "Customer address is required.");
            }

            if (string.IsNullOrWhiteSpace(borrow.NewCustomerIDType))
            {
                ModelState.AddModelError(
                    "NewCustomerIDType",
                    "ID type is required.");
            }

            if (string.IsNullOrWhiteSpace(borrow.NewCustomerIDNumber))
            {
                ModelState.AddModelError(
                    "NewCustomerIDNumber",
                    "ID number is required.");
            }

            if (borrow.BorrowDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    "BorrowDate",
                    "Borrow date cannot be a future date.");
            }

            if (borrow.ReturnDate.Date < borrow.BorrowDate.Date)
            {
                ModelState.AddModelError(
                    "ReturnDate",
                    "Return date cannot be earlier than the borrow date.");
            }

            var existingBorrow = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                .FirstOrDefaultAsync(b => b.BorrowID == id);

            if (existingBorrow == null)
                return NotFound();

            var newIDs = (borrow.SelectedItemIDs ??
                          new List<int>())
                .Distinct()
                .ToList();

            var newItems = await _context.Items
                .Where(i => newIDs.Contains(i.ItemID))
                .ToListAsync();

            var oldIDs = existingBorrow.BorrowItems
                .Select(bi => bi.ItemID)
                .Distinct()
                .ToList();

            var oldItems = await _context.Items
                .Where(i => oldIDs.Contains(i.ItemID))
                .ToListAsync();

            if (!ModelState.IsValid ||
                newItems.Count != newIDs.Count)
            {
                if (newItems.Count != newIDs.Count)
                {
                    ModelState.AddModelError(
                        "",
                        "One or more selected items could not be found.");
                }

                await LoadItemsForEdit(newIDs);
                return View(borrow);
            }

            bool wasBorrowed =
                existingBorrow.Status == "Borrowed";

            bool willBeBorrowed =
                borrow.Status == "Borrowed";

            if (wasBorrowed)
            {
                foreach (var item in oldItems)
                {
                    item.Quantity += 1;
                    item.Status = "Available";
                }
            }

            if (willBeBorrowed)
            {
                foreach (var item in newItems)
                {
                    if (item.Quantity <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            $"{item.ItemName} is out of stock.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    await LoadItemsForEdit(newIDs);
                    return View(borrow);
                }
            }

            if (existingBorrow.Customer != null)
            {
                existingBorrow.Customer.CustomerName =
                    borrow.NewCustomerName;

                existingBorrow.Customer.Email =
                    borrow.NewCustomerEmail;

                existingBorrow.Customer.PhoneNumber =
                    borrow.NewCustomerPhone;

                existingBorrow.Customer.Address =
                    borrow.NewCustomerAddress;

                existingBorrow.Customer.IDType =
                    borrow.NewCustomerIDType;

                existingBorrow.Customer.IDNumber =
                    borrow.NewCustomerIDNumber;
            }

            existingBorrow.BorrowDate =
                borrow.BorrowDate;

            existingBorrow.ReturnDate =
                borrow.ReturnDate;

            existingBorrow.Status =
                borrow.Status;

            existingBorrow.Quantity = 1;

            _context.BorrowItems.RemoveRange(
                existingBorrow.BorrowItems);

            if (newItems.Any())
            {
                existingBorrow.ItemID =
                    newItems.First().ItemID;
            }

            foreach (var item in newItems)
            {
                _context.BorrowItems.Add(new BorrowItem
                {
                    BorrowID = existingBorrow.BorrowID,
                    ItemID = item.ItemID
                });

                if (willBeBorrowed)
                {
                    item.Quantity -= 1;

                    item.Status = item.Quantity > 0
                        ? "Available"
                        : "Borrowed";
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ADMIN: open return inspection
        [HttpGet]
        public async Task<IActionResult> ReturnBorrow(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            var borrow = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                        .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(b => b.BorrowID == id);

            if (borrow == null)
                return NotFound();

            if (borrow.Status != "Borrowed")
            {
                TempData["Info"] =
                    "Only borrowed items can be returned.";

                return RedirectToAction(nameof(Index));
            }

            return View(borrow);
        }

        // ADMIN: save return and calculate penalty
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBorrow(
            int id,
            DateTime actualReturnDate)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            var borrow = await _context.Borrows
                .Include(b => b.BorrowItems)
                .FirstOrDefaultAsync(b => b.BorrowID == id);

            if (borrow == null)
                return NotFound();

            if (borrow.Status == "Returned")
            {
                TempData["Info"] =
                    "This booking has already been returned.";

                return RedirectToAction(nameof(Index));
            }

            if (borrow.Status != "Borrowed")
            {
                TempData["Error"] =
                    "Only borrowed rentals can be returned.";

                return RedirectToAction(nameof(Index));
            }

            if (actualReturnDate.Date <
                borrow.BorrowDate.Date)
            {
                TempData["Error"] =
                    "Actual return date cannot be earlier than the borrow date.";

                return RedirectToAction(
                    nameof(ReturnBorrow),
                    new { id });
            }

            if (actualReturnDate.Date >
                DateTime.Today)
            {
                TempData["Error"] =
                    "Actual return date cannot be a future date.";

                return RedirectToAction(
                    nameof(ReturnBorrow),
                    new { id });
            }

            // Calculate late days
            int daysLate = 0;

            if (actualReturnDate.Date >
                borrow.ReturnDate.Date)
            {
                daysLate =
                    (actualReturnDate.Date -
                     borrow.ReturnDate.Date).Days;
            }

            // ₱50 penalty per late day
            decimal penaltyPerDay = 50m;

            decimal penaltyAmount =
                daysLate * penaltyPerDay;

            // Prevent duplicate penalties
            var existingPenalty = await _context.Penalties
                .FirstOrDefaultAsync(
                    p => p.BorrowID == borrow.BorrowID);

            if (existingPenalty == null)
            {
                var penalty = new Penalty
                {
                    BorrowID = borrow.BorrowID,
                    DaysLate = daysLate,
                    PenaltyPerDay = penaltyPerDay,
                    PenaltyAmount = penaltyAmount
                };

                _context.Penalties.Add(penalty);
            }
            else
            {
                existingPenalty.DaysLate = daysLate;
                existingPenalty.PenaltyPerDay = penaltyPerDay;
                existingPenalty.PenaltyAmount = penaltyAmount;
            }

            borrow.Status = "Returned";

            // Return item stock
            var itemIDs = borrow.BorrowItems
                .Select(bi => bi.ItemID)
                .Distinct()
                .ToList();

            var items = await _context.Items
                .Where(i => itemIDs.Contains(i.ItemID))
                .ToListAsync();

            foreach (var item in items)
            {
                item.Quantity += 1;
                item.Status = "Available";
            }

            await _context.SaveChangesAsync();

            if (penaltyAmount > 0)
            {
                TempData["Success"] =
                    $"Return completed. Late penalty: ₱{penaltyAmount:N2}.";
            }
            else
            {
                TempData["Success"] =
                    "Return completed successfully. No penalty.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ADMIN: delete booking/rental
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            if (id == null)
                return NotFound();

            var borrow = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                .FirstOrDefaultAsync(b => b.BorrowID == id);

            if (borrow == null)
                return NotFound();

            return View(borrow);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            var borrow = await _context.Borrows
                .Include(b => b.BorrowItems)
                .FirstOrDefaultAsync(b => b.BorrowID == id);

            if (borrow != null)
            {
                if (borrow.Status == "Borrowed")
                {
                    var itemIDs = borrow.BorrowItems
                        .Select(bi => bi.ItemID)
                        .ToList();

                    var items = await _context.Items
                        .Where(i => itemIDs.Contains(i.ItemID))
                        .ToListAsync();

                    foreach (var item in items)
                    {
                        item.Quantity += 1;
                        item.Status = "Available";
                    }
                }

                var payments = await _context.Payments
                    .Where(p => p.BorrowID == id)
                    .ToListAsync();

                _context.Payments.RemoveRange(payments);

                _context.BorrowItems.RemoveRange(
                    borrow.BorrowItems);

                _context.Borrows.Remove(borrow);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadAvailableItems()
        {
            ViewBag.Items = await _context.Items
                .Include(i => i.Category)
                .Where(i =>
                    i.Status == "Available" &&
                    i.Quantity > 0)
                .OrderBy(i => i.ItemName)
                .ToListAsync();
        }

        private async Task LoadItemsForEdit(
            List<int> selectedItemIDs)
        {
            ViewBag.Items = await _context.Items
                .Include(i => i.Category)
                .Where(i =>
                    (i.Status == "Available" &&
                     i.Quantity > 0) ||
                    selectedItemIDs.Contains(i.ItemID))
                .OrderBy(i => i.ItemName)
                .ToListAsync();
        }
    }
}