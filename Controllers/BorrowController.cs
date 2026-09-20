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


        // =========================================================
        // SESSION
        // =========================================================

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("AdminUser") != null;
        }


        private bool IsCustomer()
        {
            return HttpContext.Session.GetInt32("CustomerID") != null;
        }


        // =========================================================
        // INDEX
        // =========================================================

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var borrows = await _context.Borrows

                .Include(b => b.Customer)

                .Include(b => b.Item)
                    .ThenInclude(i => i.Category)

                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                        .ThenInclude(i => i.Category)

                .OrderByDescending(b => b.BorrowID)

                .ToListAsync();


            return View(borrows);
        }


        // =========================================================
        // CREATE PHYSICAL RENTAL - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            await LoadAvailableItems();


            return View(new Borrow
            {
                BorrowDate =
                    DateTime.Today,

                ReturnDate =
                    DateTime.Today.AddDays(1),

                Status =
                    "Borrowed"
            });
        }


        // =========================================================
        // CREATE PHYSICAL RENTAL - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Borrow borrow)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            // These are assigned by controller.
            ModelState.Remove("CustomerID");
            ModelState.Remove("ItemID");
            ModelState.Remove("Customer");
            ModelState.Remove("Item");


            // -----------------------------------------------------
            // CUSTOMER VALIDATION
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                borrow.NewCustomerName))
            {
                ModelState.AddModelError(
                    "NewCustomerName",
                    "Customer name is required.");
            }


            if (string.IsNullOrWhiteSpace(
                borrow.NewCustomerEmail))
            {
                ModelState.AddModelError(
                    "NewCustomerEmail",
                    "Customer email is required.");
            }


            if (string.IsNullOrWhiteSpace(
                borrow.NewCustomerPhone))
            {
                ModelState.AddModelError(
                    "NewCustomerPhone",
                    "Phone number is required.");
            }


            if (string.IsNullOrWhiteSpace(
                borrow.NewCustomerAddress))
            {
                ModelState.AddModelError(
                    "NewCustomerAddress",
                    "Address is required.");
            }


            if (string.IsNullOrWhiteSpace(
                borrow.NewCustomerIDType))
            {
                ModelState.AddModelError(
                    "NewCustomerIDType",
                    "ID type is required.");
            }


            if (string.IsNullOrWhiteSpace(
                borrow.NewCustomerIDNumber))
            {
                ModelState.AddModelError(
                    "NewCustomerIDNumber",
                    "ID number is required.");
            }


            // -----------------------------------------------------
            // ITEMS
            // -----------------------------------------------------

            if (borrow.SelectedItemIDs == null ||
                borrow.SelectedItemIDs.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one item.");
            }


            // -----------------------------------------------------
            // DATES
            // -----------------------------------------------------

            if (borrow.BorrowDate.Date >
                DateTime.Today)
            {
                ModelState.AddModelError(
                    "BorrowDate",
                    "Borrow date cannot be a future date.");
            }


            if (borrow.ReturnDate.Date <
                borrow.BorrowDate.Date)
            {
                ModelState.AddModelError(
                    "ReturnDate",
                    "Return date cannot be earlier than borrow date.");
            }


            if (!ModelState.IsValid)
            {
                await LoadAvailableItems();

                return View(borrow);
            }


            // -----------------------------------------------------
            // GET ITEMS
            // -----------------------------------------------------

            var selectedIDs =
                borrow.SelectedItemIDs
                    .Distinct()
                    .ToList();


            var selectedItems =
                await _context.Items
                    .Include(i => i.Category)
                    .Where(i =>
                        selectedIDs.Contains(i.ItemID))
                    .ToListAsync();


            if (selectedItems.Count !=
                selectedIDs.Count)
            {
                ModelState.AddModelError(
                    "",
                    "One or more selected items could not be found.");

                await LoadAvailableItems();

                return View(borrow);
            }


            // -----------------------------------------------------
            // CHECK STOCK
            // -----------------------------------------------------

            foreach (var item in selectedItems)
            {
                if (item.Quantity <= 0 ||
                    item.Status != "Available")
                {
                    ModelState.AddModelError(
                        "",
                        $"{item.ItemName} is unavailable.");
                }
            }


            if (!ModelState.IsValid)
            {
                await LoadAvailableItems();

                return View(borrow);
            }


            // -----------------------------------------------------
            // FIND CUSTOMER
            // -----------------------------------------------------

            var customer =
                await _context.Customers
                    .FirstOrDefaultAsync(c =>
                        c.Email ==
                        borrow.NewCustomerEmail);


            // -----------------------------------------------------
            // CREATE CUSTOMER
            // -----------------------------------------------------

            if (customer == null)
            {
                customer = new Customer
                {
                    CustomerName =
                        borrow.NewCustomerName,

                    Email =
                        borrow.NewCustomerEmail,

                    PhoneNumber =
                        borrow.NewCustomerPhone,

                    Address =
                        borrow.NewCustomerAddress,

                    IDType =
                        borrow.NewCustomerIDType,

                    IDNumber =
                        borrow.NewCustomerIDNumber
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


            // -----------------------------------------------------
            // CREATE PHYSICAL RENTAL
            // -----------------------------------------------------

            borrow.CustomerID =
                customer.CustomerID;

            borrow.ItemID =
                selectedItems.First().ItemID;

            borrow.Quantity =
                1;

            borrow.Status =
                "Borrowed";


            _context.Borrows.Add(borrow);

            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // BORROW ITEMS + REDUCE STOCK
            // -----------------------------------------------------

            foreach (var item in selectedItems)
            {
                _context.BorrowItems.Add(
                    new BorrowItem
                    {
                        BorrowID =
                            borrow.BorrowID,

                        ItemID =
                            item.ItemID
                    });


                item.Quantity -= 1;


                item.Status =
                    item.Quantity > 0
                        ? "Available"
                        : "Borrowed";
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Physical rental #{borrow.BorrowID} created successfully.";


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // BOOK - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Book()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            await LoadAvailableItems();


            return View(new Borrow
            {
                BorrowDate =
                    DateTime.Today,

                ReturnDate =
                    DateTime.Today.AddDays(1),

                Status =
                    "Booked"
            });
        }


        // =========================================================
        // BOOK - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(
            Borrow borrow)
        {
            if (!IsAdmin())
            {
                TempData["Error"] =
                    "Your admin session was not detected.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }


            // IMPORTANT:
            // CustomerID and ItemID are assigned later
            // inside this controller.

            ModelState.Remove("CustomerID");
            ModelState.Remove("ItemID");
            ModelState.Remove("Customer");
            ModelState.Remove("Item");


            // =====================================================
            // CUSTOMER VALIDATION
            // =====================================================

            if (string.IsNullOrWhiteSpace(
                borrow.NewCustomerName))
            {
                ModelState.AddModelError(
                    "NewCustomerName",
                    "Customer name is required.");
            }


            if (string.IsNullOrWhiteSpace(
                borrow.NewCustomerEmail))
            {
                ModelState.AddModelError(
                    "NewCustomerEmail",
                    "Customer email is required.");
            }


            if (string.IsNullOrWhiteSpace(
                borrow.NewCustomerPhone))
            {
                ModelState.AddModelError(
                    "NewCustomerPhone",
                    "Phone number is required.");
            }


            if (string.IsNullOrWhiteSpace(
                borrow.NewCustomerAddress))
            {
                ModelState.AddModelError(
                    "NewCustomerAddress",
                    "Address is required.");
            }


            if (string.IsNullOrWhiteSpace(
                borrow.NewCustomerIDType))
            {
                ModelState.AddModelError(
                    "NewCustomerIDType",
                    "ID type is required.");
            }


            if (string.IsNullOrWhiteSpace(
                borrow.NewCustomerIDNumber))
            {
                ModelState.AddModelError(
                    "NewCustomerIDNumber",
                    "ID number is required.");
            }


            // =====================================================
            // CHECK ITEMS
            // =====================================================

            if (borrow.SelectedItemIDs == null ||
                borrow.SelectedItemIDs.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one booking item.");
            }


            // =====================================================
            // DATES
            // =====================================================

            if (borrow.BorrowDate.Date <
                DateTime.Today)
            {
                ModelState.AddModelError(
                    "BorrowDate",
                    "Borrow date cannot be in the past.");
            }


            if (borrow.ReturnDate.Date <
                borrow.BorrowDate.Date)
            {
                ModelState.AddModelError(
                    "ReturnDate",
                    "Return date cannot be earlier than borrow date.");
            }


            // =====================================================
            // IF INVALID
            // =====================================================

            if (!ModelState.IsValid)
            {
                await LoadAvailableItems();

                return View(borrow);
            }


            // =====================================================
            // GET SELECTED ITEMS
            // =====================================================

            var selectedIDs =
                borrow.SelectedItemIDs
                    .Distinct()
                    .ToList();


            var selectedItems =
                await _context.Items
                    .Include(i => i.Category)
                    .Where(i =>
                        selectedIDs.Contains(
                            i.ItemID))
                    .ToListAsync();


            if (selectedItems.Count !=
                selectedIDs.Count)
            {
                ModelState.AddModelError(
                    "",
                    "One or more selected items could not be found.");

                await LoadAvailableItems();

                return View(borrow);
            }


            // =====================================================
            // CHECK AVAILABILITY
            // =====================================================

            foreach (var item in selectedItems)
            {
                if (item.Quantity <= 0 ||
                    item.Status != "Available")
                {
                    ModelState.AddModelError(
                        "",
                        $"{item.ItemName} is currently unavailable.");
                }
            }


            if (!ModelState.IsValid)
            {
                await LoadAvailableItems();

                return View(borrow);
            }


            // =====================================================
            // FIND CUSTOMER
            // =====================================================

            var customer =
                await _context.Customers
                    .FirstOrDefaultAsync(c =>
                        c.Email ==
                        borrow.NewCustomerEmail);


            // =====================================================
            // CREATE CUSTOMER IF NEW
            // =====================================================

            if (customer == null)
            {
                customer = new Customer
                {
                    CustomerName =
                        borrow.NewCustomerName,

                    Email =
                        borrow.NewCustomerEmail,

                    PhoneNumber =
                        borrow.NewCustomerPhone,

                    Address =
                        borrow.NewCustomerAddress,

                    IDType =
                        borrow.NewCustomerIDType,

                    IDNumber =
                        borrow.NewCustomerIDNumber
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


            // =====================================================
            // CREATE BOOKING
            // =====================================================

            borrow.CustomerID =
                customer.CustomerID;


            // Required ItemID gets the first selected item.
            borrow.ItemID =
                selectedItems.First().ItemID;


            borrow.Quantity =
                1;


            // IMPORTANT:
            // Your system uses BOOKED.
            borrow.Status =
                "Booked";


            _context.Borrows.Add(
                borrow);


            await _context.SaveChangesAsync();


            // =====================================================
            // CREATE BORROW ITEMS
            // =====================================================

            foreach (var item in selectedItems)
            {
                _context.BorrowItems.Add(
                    new BorrowItem
                    {
                        BorrowID =
                            borrow.BorrowID,

                        ItemID =
                            item.ItemID
                    });
            }


            // IMPORTANT:
            // DO NOT REDUCE STOCK FOR A BOOKING.
            //
            // Booking = Booked
            // Physical rental = Borrowed


            await _context.SaveChangesAsync();


            // =====================================================
            // SUCCESS
            // =====================================================

            TempData["Success"] =
                $"Booking #{borrow.BorrowID} was saved successfully.";


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            int? id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            if (id == null)
                return NotFound();


            var borrow =
                await _context.Borrows
                    .Include(b => b.Customer)
                    .Include(b => b.BorrowItems)
                        .ThenInclude(bi => bi.Item)
                    .FirstOrDefaultAsync(b =>
                        b.BorrowID == id);


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


            borrow.SelectedItemIDs =
                borrow.BorrowItems
                    .Select(bi => bi.ItemID)
                    .ToList();


            await LoadItemsForEdit(
                borrow.SelectedItemIDs);


            return View(borrow);
        }


        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Borrow borrow)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            if (id != borrow.BorrowID)
                return NotFound();


            ModelState.Remove("CustomerID");
            ModelState.Remove("ItemID");
            ModelState.Remove("Customer");
            ModelState.Remove("Item");


            var existingBorrow =
                await _context.Borrows
                    .Include(b => b.Customer)
                    .Include(b => b.BorrowItems)
                    .FirstOrDefaultAsync(b =>
                        b.BorrowID == id);


            if (existingBorrow == null)
                return NotFound();


            if (borrow.SelectedItemIDs == null ||
                borrow.SelectedItemIDs.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one item.");
            }


            if (borrow.ReturnDate.Date <
                borrow.BorrowDate.Date)
            {
                ModelState.AddModelError(
                    "ReturnDate",
                    "Return date cannot be earlier than borrow date.");
            }


            if (!ModelState.IsValid)
            {
                await LoadItemsForEdit(
                    borrow.SelectedItemIDs ??
                    new List<int>());

                return View(borrow);
            }


            var selectedIDs =
                borrow.SelectedItemIDs
                    .Distinct()
                    .ToList();


            var newItems =
                await _context.Items
                    .Where(i =>
                        selectedIDs.Contains(i.ItemID))
                    .ToListAsync();


            if (newItems.Count !=
                selectedIDs.Count)
            {
                ModelState.AddModelError(
                    "",
                    "One or more selected items could not be found.");

                await LoadItemsForEdit(
                    selectedIDs);

                return View(borrow);
            }


            // -----------------------------------------------------
            // RESTORE STOCK IF OLD RECORD WAS BORROWED
            // -----------------------------------------------------

            if (existingBorrow.Status ==
                "Borrowed")
            {
                var oldItemIDs =
                    existingBorrow.BorrowItems
                        .Select(bi => bi.ItemID)
                        .ToList();


                var oldItems =
                    await _context.Items
                        .Where(i =>
                            oldItemIDs.Contains(i.ItemID))
                        .ToListAsync();


                foreach (var item in oldItems)
                {
                    item.Quantity += 1;
                    item.Status = "Available";
                }
            }


            // -----------------------------------------------------
            // UPDATE CUSTOMER
            // -----------------------------------------------------

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


            // -----------------------------------------------------
            // UPDATE BORROW
            // -----------------------------------------------------

            existingBorrow.BorrowDate =
                borrow.BorrowDate;

            existingBorrow.ReturnDate =
                borrow.ReturnDate;

            existingBorrow.Status =
                borrow.Status;

            existingBorrow.Quantity =
                1;

            existingBorrow.ItemID =
                newItems.First().ItemID;


            // -----------------------------------------------------
            // REMOVE OLD BORROW ITEMS
            // -----------------------------------------------------

            _context.BorrowItems.RemoveRange(
                existingBorrow.BorrowItems);


            // -----------------------------------------------------
            // ADD NEW BORROW ITEMS
            // -----------------------------------------------------

            foreach (var item in newItems)
            {
                _context.BorrowItems.Add(
                    new BorrowItem
                    {
                        BorrowID =
                            existingBorrow.BorrowID,

                        ItemID =
                            item.ItemID
                    });


                // Only physical rental consumes stock.
                if (borrow.Status ==
                    "Borrowed")
                {
                    item.Quantity -= 1;

                    item.Status =
                        item.Quantity > 0
                            ? "Available"
                            : "Borrowed";
                }
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Booking or rental updated successfully.";


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // RETURN - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ReturnBorrow(
            int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var borrow =
                await _context.Borrows
                    .Include(b => b.Customer)
                    .Include(b => b.BorrowItems)
                        .ThenInclude(bi => bi.Item)
                            .ThenInclude(i => i.Category)
                    .FirstOrDefaultAsync(b =>
                        b.BorrowID == id);


            if (borrow == null)
                return NotFound();


            if (borrow.Status !=
                "Borrowed")
            {
                TempData["Info"] =
                    "Only borrowed rentals can be returned.";

                return RedirectToAction(
                    nameof(Index));
            }


            return View(borrow);
        }


        // =========================================================
        // RETURN - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBorrow(
            int id,
            DateTime actualReturnDate)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var borrow =
                await _context.Borrows
                    .Include(b => b.BorrowItems)
                    .FirstOrDefaultAsync(b =>
                        b.BorrowID == id);


            if (borrow == null)
                return NotFound();


            if (borrow.Status !=
                "Borrowed")
            {
                TempData["Error"] =
                    "Only borrowed rentals can be returned.";

                return RedirectToAction(
                    nameof(Index));
            }


            if (actualReturnDate.Date <
                borrow.BorrowDate.Date)
            {
                TempData["Error"] =
                    "Actual return date cannot be earlier than borrow date.";

                return RedirectToAction(
                    nameof(ReturnBorrow),
                    new
                    {
                        id
                    });
            }


            if (actualReturnDate.Date >
                DateTime.Today)
            {
                TempData["Error"] =
                    "Actual return date cannot be in the future.";

                return RedirectToAction(
                    nameof(ReturnBorrow),
                    new
                    {
                        id
                    });
            }


            int daysLate = 0;


            if (actualReturnDate.Date >
                borrow.ReturnDate.Date)
            {
                daysLate =
                    (
                        actualReturnDate.Date -
                        borrow.ReturnDate.Date
                    ).Days;
            }


            decimal penaltyPerDay =
                50m;


            decimal penaltyAmount =
                daysLate *
                penaltyPerDay;


            // -----------------------------------------------------
            // UPDATE STATUS
            // -----------------------------------------------------

            borrow.Status =
                "Returned";


            // -----------------------------------------------------
            // RESTORE ITEMS
            // -----------------------------------------------------

            var itemIDs =
                borrow.BorrowItems
                    .Select(bi => bi.ItemID)
                    .Distinct()
                    .ToList();


            var items =
                await _context.Items
                    .Where(i =>
                        itemIDs.Contains(i.ItemID))
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


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // DELETE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(
            int? id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            if (id == null)
                return NotFound();


            var borrow =
                await _context.Borrows
                    .Include(b => b.Customer)
                    .Include(b => b.BorrowItems)
                        .ThenInclude(bi => bi.Item)
                    .FirstOrDefaultAsync(b =>
                        b.BorrowID == id);


            if (borrow == null)
                return NotFound();


            return View(borrow);
        }


        // =========================================================
        // DELETE - POST
        // =========================================================

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var borrow =
                await _context.Borrows
                    .Include(b => b.BorrowItems)
                    .FirstOrDefaultAsync(b =>
                        b.BorrowID == id);


            if (borrow == null)
            {
                return RedirectToAction(
                    nameof(Index));
            }


            // Restore stock only when deleting
            // an actual physical rental.
            if (borrow.Status ==
                "Borrowed")
            {
                var itemIDs =
                    borrow.BorrowItems
                        .Select(bi => bi.ItemID)
                        .ToList();


                var items =
                    await _context.Items
                        .Where(i =>
                            itemIDs.Contains(i.ItemID))
                        .ToListAsync();


                foreach (var item in items)
                {
                    item.Quantity += 1;
                    item.Status = "Available";
                }
            }


            _context.BorrowItems.RemoveRange(
                borrow.BorrowItems);


            _context.Borrows.Remove(
                borrow);


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Booking or rental deleted successfully.";


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // LOAD AVAILABLE ITEMS
        // =========================================================

        private async Task LoadAvailableItems()
        {
            ViewBag.Items =
                await _context.Items
                    .Include(i => i.Category)
                    .Where(i =>
                        i.Status == "Available" &&
                        i.Quantity > 0)
                    .OrderBy(i => i.ItemName)
                    .ToListAsync();
        }


        // =========================================================
        // LOAD ITEMS FOR EDIT
        // =========================================================

        private async Task LoadItemsForEdit(
            List<int> selectedItemIDs)
        {
            ViewBag.Items =
                await _context.Items
                    .Include(i => i.Category)
                    .Where(i =>
                        (
                            i.Status == "Available" &&
                            i.Quantity > 0
                        )
                        ||
                        selectedItemIDs.Contains(i.ItemID))
                    .OrderBy(i => i.ItemName)
                    .ToListAsync();
        }
    }
}