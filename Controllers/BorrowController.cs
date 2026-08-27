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

        // GET: Borrow/Index
        public async Task<IActionResult> Index()
        {
            var borrows = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                    .ThenInclude(i => i.Category)
                .ToListAsync();

            return View(borrows);
        }

        // GET: Borrow/ItemBorrow
        public async Task<IActionResult> ItemBorrow()
        {
            var items = await _context.Items
                .Include(i => i.Category)
                .Where(i =>
                    i.Status == "Available" &&
                    i.Quantity > 0)
                .ToListAsync();

            return View(items);
        }

        // GET: Borrow/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadAvailableItems();

            return View();
        }

        // POST: Borrow/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Borrow borrow)
        {
            // Remove validation for old single-item fields
            ModelState.Remove("ItemID");
            ModelState.Remove("Item");
            ModelState.Remove("Customer");

            // At least one item
            if (borrow.SelectedItemIDs == null ||
                borrow.SelectedItemIDs.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please add at least one item."
                );
            }

            // Borrow date validation
            if (borrow.BorrowDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    "BorrowDate",
                    "Borrow date cannot be a future date."
                );
            }

            // Return date validation
            if (borrow.ReturnDate.Date < borrow.BorrowDate.Date)
            {
                ModelState.AddModelError(
                    "ReturnDate",
                    "Return date cannot be earlier than the borrow date."
                );
            }

            if (!ModelState.IsValid)
            {
                await LoadAvailableItems();

                return View(borrow);
            }

            // Find customer
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.Email == borrow.NewCustomerEmail);

            // Create customer
            if (customer == null)
            {
                customer = new Customer
                {
                    CustomerName = borrow.NewCustomerName,
                    Email = borrow.NewCustomerEmail,
                    PhoneNumber = borrow.NewCustomerPhone,
                    Address = borrow.NewCustomerAddress
                };

                _context.Customers.Add(customer);

                await _context.SaveChangesAsync();
            }
            else
            {
                // Update existing customer
                customer.CustomerName =
                    borrow.NewCustomerName;

                customer.PhoneNumber =
                    borrow.NewCustomerPhone;

                customer.Address =
                    borrow.NewCustomerAddress;

                await _context.SaveChangesAsync();
            }

            borrow.CustomerID =
                customer.CustomerID;

            borrow.Status = "Borrowed";

            borrow.Quantity = 1;

            // Remove duplicate item IDs
            var selectedItemIDs =
                borrow.SelectedItemIDs
                    .Distinct()
                    .ToList();

            // Get selected items
            var selectedItems =
                await _context.Items
                    .Where(i =>
                        selectedItemIDs.Contains(i.ItemID))
                    .ToListAsync();

            // Check items
            if (selectedItems.Count != selectedItemIDs.Count)
            {
                ModelState.AddModelError(
                    "",
                    "One or more selected items could not be found."
                );

                await LoadAvailableItems();

                return View(borrow);
            }

            // Check stock
            foreach (var item in selectedItems)
            {
                if (item.Quantity <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        $"{item.ItemName} is out of stock."
                    );

                    await LoadAvailableItems();

                    return View(borrow);
                }
            }

            // First item kept for old ItemID
            borrow.ItemID =
                selectedItems.First().ItemID;

            // Save borrow
            _context.Borrows.Add(borrow);

            await _context.SaveChangesAsync();

            // Add multiple items
            foreach (var item in selectedItems)
            {
                var borrowItem = new BorrowItem
                {
                    BorrowID = borrow.BorrowID,
                    ItemID = item.ItemID
                };

                _context.BorrowItems.Add(borrowItem);

                // Reduce stock
                item.Quantity -= 1;

                if (item.Quantity <= 0)
                {
                    item.Status = "Borrowed";
                }
                else
                {
                    item.Status = "Available";
                }
            }

            await _context.SaveChangesAsync();

            // Go to payment page
            return RedirectToAction(
                "Create",
                "Payment",
                new
                {
                    borrowId = borrow.BorrowID
                }
            );
        }

        // GET: Borrow/Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrow = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                .FirstOrDefaultAsync(b =>
                    b.BorrowID == id);

            if (borrow == null)
            {
                return NotFound();
            }

            // Load customer information
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
            }

            // Load currently selected items
            borrow.SelectedItemIDs =
                borrow.BorrowItems
                    .Select(bi => bi.ItemID)
                    .ToList();

            // Load items for Edit
            await LoadItemsForEdit(borrow.SelectedItemIDs);

            return View(borrow);
        }

        // POST: Borrow/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Borrow borrow)
        {
            if (id != borrow.BorrowID)
            {
                return NotFound();
            }

            // Remove validation for old navigation properties
            ModelState.Remove("ItemID");
            ModelState.Remove("Item");
            ModelState.Remove("Customer");

            // At least one item
            if (borrow.SelectedItemIDs == null ||
                borrow.SelectedItemIDs.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one item."
                );
            }

            // Borrow date validation
            if (borrow.BorrowDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    "BorrowDate",
                    "Borrow date cannot be a future date."
                );
            }

            // Return date validation
            if (borrow.ReturnDate.Date < borrow.BorrowDate.Date)
            {
                ModelState.AddModelError(
                    "ReturnDate",
                    "Return date cannot be earlier than the borrow date."
                );
            }

            // Get existing borrow first
            var existingBorrow =
                await _context.Borrows
                    .Include(b => b.Customer)
                    .Include(b => b.BorrowItems)
                    .FirstOrDefaultAsync(b =>
                        b.BorrowID == id);

            if (existingBorrow == null)
            {
                return NotFound();
            }

            // If validation failed, reload items and return view
            if (!ModelState.IsValid)
            {
                await LoadItemsForEdit(
                    borrow.SelectedItemIDs ?? new List<int>()
                );

                return View(borrow);
            }

            // Remove duplicate item IDs
            var newItemIDs =
                (borrow.SelectedItemIDs ?? new List<int>())
                    .Distinct()
                    .ToList();

            // Get new items
            var newItems =
                await _context.Items
                    .Where(i =>
                        newItemIDs.Contains(i.ItemID))
                    .ToListAsync();

            // Check if all items exist
            if (newItems.Count != newItemIDs.Count)
            {
                ModelState.AddModelError(
                    "",
                    "One or more selected items could not be found."
                );

                await LoadItemsForEdit(newItemIDs);

                return View(borrow);
            }

            // Get old item IDs
            var oldItemIDs =
                existingBorrow.BorrowItems
                    .Select(bi => bi.ItemID)
                    .Distinct()
                    .ToList();

            // Get old items
            var oldItems =
                await _context.Items
                    .Where(i =>
                        oldItemIDs.Contains(i.ItemID))
                    .ToListAsync();

            /*
             * STOCK LOGIC
             *
             * Borrowed -> Borrowed
             * Restore old items, then deduct new items.
             *
             * Borrowed -> Returned
             * Restore old items only.
             *
             * Returned -> Borrowed
             * Deduct new items only.
             *
             * Returned -> Returned
             * No stock change.
             */

            bool wasBorrowed =
                existingBorrow.Status == "Borrowed";

            bool willBeBorrowed =
                borrow.Status == "Borrowed";

            // Restore old stock if the previous record was Borrowed
            if (wasBorrowed)
            {
                foreach (var item in oldItems)
                {
                    item.Quantity += 1;
                    item.Status = "Available";
                }
            }

            // If changing to Borrowed, check stock
            if (willBeBorrowed)
            {
                foreach (var item in newItems)
                {
                    // If this item was already borrowed by this same record
                    // and was restored above, it is now available.
                    if (item.Quantity <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            $"{item.ItemName} is out of stock."
                        );
                    }
                }

                if (!ModelState.IsValid)
                {
                    await LoadItemsForEdit(newItemIDs);

                    return View(borrow);
                }
            }

            // Update customer
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
            }

            // Update borrow information
            existingBorrow.IDType =
                borrow.IDType;

            existingBorrow.IDNumber =
                borrow.IDNumber;

            existingBorrow.BorrowDate =
                borrow.BorrowDate;

            existingBorrow.ReturnDate =
                borrow.ReturnDate;

            existingBorrow.Status =
                borrow.Status;

            existingBorrow.Quantity = 1;

            // Remove old BorrowItems
            _context.BorrowItems.RemoveRange(
                existingBorrow.BorrowItems
            );

            // If there are selected items,
            // keep the first item in the old ItemID field
            if (newItems.Any())
            {
                existingBorrow.ItemID =
                    newItems.First().ItemID;
            }

            // Add new BorrowItems
            foreach (var item in newItems)
            {
                _context.BorrowItems.Add(
                    new BorrowItem
                    {
                        BorrowID =
                            existingBorrow.BorrowID,

                        ItemID =
                            item.ItemID
                    }
                );

                // Deduct stock only if status is Borrowed
                if (willBeBorrowed)
                {
                    item.Quantity -= 1;

                    if (item.Quantity <= 0)
                    {
                        item.Status = "Borrowed";
                    }
                    else
                    {
                        item.Status = "Available";
                    }
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Return Borrow
        [HttpGet]
        public async Task<IActionResult> ReturnBorrow(int id)
        {
            var borrow = await _context.Borrows
                .Include(b => b.BorrowItems)
                .FirstOrDefaultAsync(b =>
                    b.BorrowID == id);

            if (borrow == null)
            {
                return NotFound();
            }

            if (borrow.Status != "Returned")
            {
                borrow.Status = "Returned";

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

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Borrow/Delete
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrow = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                .FirstOrDefaultAsync(b =>
                    b.BorrowID == id);

            if (borrow == null)
            {
                return NotFound();
            }

            return View(borrow);
        }

        // POST: Borrow/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var borrow = await _context.Borrows
                .Include(b => b.BorrowItems)
                .FirstOrDefaultAsync(b =>
                    b.BorrowID == id);

            if (borrow != null)
            {
                // Restore stock only if borrow is still active
                if (borrow.Status != "Returned")
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

                // Remove payments
                var payments =
                    await _context.Payments
                        .Where(p =>
                            p.BorrowID == id)
                        .ToListAsync();

                _context.Payments.RemoveRange(
                    payments
                );

                // Remove BorrowItems
                _context.BorrowItems.RemoveRange(
                    borrow.BorrowItems
                );

                // Remove Borrow
                _context.Borrows.Remove(borrow);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Load available items
        // Used by Create.cshtml
        private async Task LoadAvailableItems()
        {
            ViewBag.Items =
                await _context.Items
                    .Include(i => i.Category)
                    .Where(i =>
                        i.Status == "Available" &&
                        i.Quantity > 0)
                    .ToListAsync();
        }

        // Load items for Edit
        // Includes available items AND currently selected items
        private async Task LoadItemsForEdit(
            List<int> selectedItemIDs)
        {
            var items =
                await _context.Items
                    .Include(i => i.Category)
                    .Where(i =>
                        (i.Status == "Available" &&
                         i.Quantity > 0)
                        ||
                        selectedItemIDs.Contains(i.ItemID))
                    .OrderBy(i => i.ItemName)
                    .ToListAsync();

            ViewBag.Items = items;
        }
    }
}