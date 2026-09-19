using CRUD.Data;
using CRUD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Controllers
{
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // ADMIN CHECK
        // ============================================================

        private bool IsAdmin()
        {
            return !string.IsNullOrEmpty(
                HttpContext.Session.GetString("AdminUser")
            );
        }


        // ============================================================
        // INDEX
        // ============================================================

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            var reservations = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.ReservationItems)
                    .ThenInclude(ri => ri.Item)
                .OrderByDescending(r => r.ReservationID)
                .ToListAsync();

            return View(reservations);
        }


        // ============================================================
        // CREATE - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            var reservation = new Reservation
            {
                ReservationDate = DateTime.Now,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                Quantity = 1,
                Status = "Pending"
            };

            await LoadAvailableItems();

            return View(reservation);
        }


        // ============================================================
        // CREATE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Reservation reservation)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");

            ModelState.Remove(nameof(reservation.Customer));
            ModelState.Remove(nameof(reservation.ReservationItems));

            // --------------------------------------------------------
            // CHECK SELECTED ITEMS
            // --------------------------------------------------------

            if (reservation.SelectedItemIDs == null ||
                !reservation.SelectedItemIDs.Any())
            {
                ModelState.AddModelError(
                    "SelectedItemIDs",
                    "Please select at least one item."
                );
            }


            // --------------------------------------------------------
            // CHECK DATES
            // --------------------------------------------------------

            if (reservation.StartDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "StartDate",
                    "Reservation start date cannot be in the past."
                );
            }

            if (reservation.EndDate.Date <
                reservation.StartDate.Date)
            {
                ModelState.AddModelError(
                    "EndDate",
                    "End date cannot be before the start date."
                );
            }


            if (!ModelState.IsValid)
            {
                await LoadAvailableItems(
                    reservation.SelectedItemIDs
                );

                return View(reservation);
            }


            // ========================================================
            // FIND CUSTOMER
            // ========================================================

            Customer? customer = null;

            if (!string.IsNullOrWhiteSpace(
                reservation.NewCustomerEmail))
            {
                customer = await _context.Customers
                    .FirstOrDefaultAsync(c =>
                        c.Email ==
                        reservation.NewCustomerEmail);
            }


            // ========================================================
            // CREATE CUSTOMER
            // ========================================================

            if (customer == null)
            {
                if (string.IsNullOrWhiteSpace(
                        reservation.NewCustomerName) ||
                    string.IsNullOrWhiteSpace(
                        reservation.NewCustomerEmail) ||
                    string.IsNullOrWhiteSpace(
                        reservation.NewCustomerPhone) ||
                    string.IsNullOrWhiteSpace(
                        reservation.NewCustomerAddress))
                {
                    ModelState.AddModelError(
                        "",
                        "Please complete all customer information."
                    );

                    await LoadAvailableItems(
                        reservation.SelectedItemIDs
                    );

                    return View(reservation);
                }


                customer = new Customer
                {
                    CustomerName =
                        reservation.NewCustomerName,

                    Email =
                        reservation.NewCustomerEmail,

                    PhoneNumber =
                        reservation.NewCustomerPhone,

                    Address =
                        reservation.NewCustomerAddress
                };

                _context.Customers.Add(customer);

                await _context.SaveChangesAsync();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(
                    reservation.NewCustomerName))
                {
                    customer.CustomerName =
                        reservation.NewCustomerName;
                }

                if (!string.IsNullOrWhiteSpace(
                    reservation.NewCustomerPhone))
                {
                    customer.PhoneNumber =
                        reservation.NewCustomerPhone;
                }

                if (!string.IsNullOrWhiteSpace(
                    reservation.NewCustomerAddress))
                {
                    customer.Address =
                        reservation.NewCustomerAddress;
                }

                await _context.SaveChangesAsync();
            }


            // ========================================================
            // GET SELECTED ITEMS
            // ========================================================

            var selectedIDs =
                reservation.SelectedItemIDs
                    .Distinct()
                    .ToList();


            var selectedItems =
                await _context.Items
                    .Include(i => i.Category)
                    .Where(i =>
                        selectedIDs.Contains(i.ItemID))
                    .ToListAsync();


            if (selectedItems.Count != selectedIDs.Count)
            {
                ModelState.AddModelError(
                    "",
                    "One or more selected items could not be found."
                );

                await LoadAvailableItems(
                    reservation.SelectedItemIDs
                );

                return View(reservation);
            }


            // ========================================================
            // CHECK STOCK
            // ========================================================

            var unavailableItems =
                selectedItems
                    .Where(i => i.Quantity <= 0)
                    .ToList();


            if (unavailableItems.Any())
            {
                var names =
                    string.Join(
                        ", ",
                        unavailableItems.Select(
                            i => i.ItemName)
                    );

                ModelState.AddModelError(
                    "",
                    $"The following items are out of stock: {names}"
                );

                await LoadAvailableItems(
                    reservation.SelectedItemIDs
                );

                return View(reservation);
            }


            // ========================================================
            // CREATE RESERVATION
            // ========================================================

            reservation.CustomerID =
                customer.CustomerID;

            reservation.Customer = null;

            reservation.ReservationItems =
                new List<ReservationItems>();


            foreach (var item in selectedItems)
            {
                reservation.ReservationItems.Add(
                    new ReservationItems
                    {
                        ItemID = item.ItemID
                    }
                );
            }


            reservation.Quantity =
                selectedItems.Count;

            reservation.Status =
                "Pending";

            reservation.ReservationDate =
                DateTime.Now;


            _context.Reservations.Add(
                reservation
            );

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Reservation created successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ============================================================
        // EDIT - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");


            var reservation =
                await _context.Reservations
                    .Include(r => r.Customer)
                    .Include(r => r.ReservationItems)
                        .ThenInclude(ri => ri.Item)
                    .FirstOrDefaultAsync(r =>
                        r.ReservationID == id);


            if (reservation == null)
                return NotFound();


            reservation.SelectedItemIDs =
                reservation.ReservationItems
                    .Select(ri => ri.ItemID)
                    .ToList();


            if (reservation.Customer != null)
            {
                reservation.NewCustomerName =
                    reservation.Customer.CustomerName;

                reservation.NewCustomerEmail =
                    reservation.Customer.Email;

                reservation.NewCustomerPhone =
                    reservation.Customer.PhoneNumber;

                reservation.NewCustomerAddress =
                    reservation.Customer.Address;
            }


            await LoadItemsForEdit(
                reservation.SelectedItemIDs
            );


            return View(reservation);
        }


        // ============================================================
        // EDIT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Reservation reservation)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");


            if (id != reservation.ReservationID)
                return NotFound();


            var existingReservation =
                await _context.Reservations
                    .Include(r => r.Customer)
                    .Include(r => r.ReservationItems)
                    .FirstOrDefaultAsync(r =>
                        r.ReservationID == id);


            if (existingReservation == null)
                return NotFound();


            ModelState.Remove(
                nameof(reservation.Customer)
            );

            ModelState.Remove(
                nameof(reservation.ReservationItems)
            );


            if (reservation.SelectedItemIDs == null ||
                !reservation.SelectedItemIDs.Any())
            {
                ModelState.AddModelError(
                    "SelectedItemIDs",
                    "Please select at least one item."
                );
            }


            if (reservation.EndDate.Date <
                reservation.StartDate.Date)
            {
                ModelState.AddModelError(
                    "EndDate",
                    "End date cannot be before the start date."
                );
            }


            if (!ModelState.IsValid)
            {
                reservation.Customer =
                    existingReservation.Customer;

                await LoadItemsForEdit(
                    reservation.SelectedItemIDs
                );

                return View(reservation);
            }


            // ========================================================
            // UPDATE CUSTOMER
            // ========================================================

            if (existingReservation.Customer != null)
            {
                if (!string.IsNullOrWhiteSpace(
                    reservation.NewCustomerName))
                {
                    existingReservation.Customer.CustomerName =
                        reservation.NewCustomerName;
                }

                if (!string.IsNullOrWhiteSpace(
                    reservation.NewCustomerEmail))
                {
                    existingReservation.Customer.Email =
                        reservation.NewCustomerEmail;
                }

                if (!string.IsNullOrWhiteSpace(
                    reservation.NewCustomerPhone))
                {
                    existingReservation.Customer.PhoneNumber =
                        reservation.NewCustomerPhone;
                }

                if (!string.IsNullOrWhiteSpace(
                    reservation.NewCustomerAddress))
                {
                    existingReservation.Customer.Address =
                        reservation.NewCustomerAddress;
                }
            }


            // ========================================================
            // UPDATE RESERVATION
            // ========================================================

            existingReservation.IDType =
                reservation.IDType;

            existingReservation.IDNumber =
                reservation.IDNumber;

            existingReservation.StartDate =
                reservation.StartDate;

            existingReservation.EndDate =
                reservation.EndDate;

            existingReservation.Notes =
                reservation.Notes;


            // ========================================================
            // UPDATE ITEMS
            // ========================================================

            var selectedIDs =
                reservation.SelectedItemIDs
                    .Distinct()
                    .ToList();


            var selectedItems =
                await _context.Items
                    .Where(i =>
                        selectedIDs.Contains(i.ItemID))
                    .ToListAsync();


            if (selectedItems.Count != selectedIDs.Count)
            {
                ModelState.AddModelError(
                    "",
                    "One or more selected items could not be found."
                );

                await LoadItemsForEdit(
                    reservation.SelectedItemIDs
                );

                return View(reservation);
            }


            _context.ReservationItems.RemoveRange(
                existingReservation.ReservationItems
            );


            foreach (var itemID in selectedIDs)
            {
                _context.ReservationItems.Add(
                    new ReservationItems
                    {
                        ReservationID =
                            existingReservation.ReservationID,

                        ItemID =
                            itemID
                    }
                );
            }


            existingReservation.Quantity =
                selectedIDs.Count;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Reservation updated successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ============================================================
        // BORROW RESERVATION
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Borrow(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");


            var reservation =
                await _context.Reservations
                    .Include(r => r.Customer)
                    .Include(r => r.ReservationItems)
                        .ThenInclude(ri => ri.Item)
                    .FirstOrDefaultAsync(r =>
                        r.ReservationID == id);


            if (reservation == null)
                return NotFound();


            if (reservation.Status == "Borrowed")
            {
                TempData["Error"] =
                    "This reservation has already been borrowed.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            if (reservation.Status == "Cancelled")
            {
                TempData["Error"] =
                    "A cancelled reservation cannot be borrowed.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            if (reservation.ReservationItems == null ||
                !reservation.ReservationItems.Any())
            {
                TempData["Error"] =
                    "This reservation has no items.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            var itemIDs =
                reservation.ReservationItems
                    .Select(ri => ri.ItemID)
                    .Distinct()
                    .ToList();


            var items =
                await _context.Items
                    .Where(i =>
                        itemIDs.Contains(i.ItemID))
                    .ToListAsync();


            if (items.Count != itemIDs.Count)
            {
                TempData["Error"] =
                    "One or more reserved items could not be found.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ========================================================
            // CHECK QUANTITY ONLY
            // ========================================================

            var unavailableItems =
                items
                    .Where(i => i.Quantity <= 0)
                    .ToList();


            if (unavailableItems.Any())
            {
                var names =
                    string.Join(
                        ", ",
                        unavailableItems.Select(
                            i => i.ItemName)
                    );

                TempData["Error"] =
                    $"The following items are unavailable: {names}";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            if (reservation.Customer == null)
            {
                TempData["Error"] =
                    "This reservation has no valid customer.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // ========================================================
            // CREATE BORROW
            // ========================================================

            var borrow = new Borrow
            {
                CustomerID =
                    reservation.CustomerID,

                ItemID =
                    itemIDs.First(),

                IDType =
                    reservation.IDType ?? "",

                IDNumber =
                    reservation.IDNumber ?? "",

                BorrowDate =
                    DateTime.Today,

                ReturnDate =
                    reservation.EndDate.Date >= DateTime.Today
                        ? reservation.EndDate.Date
                        : DateTime.Today.AddDays(1),

                Quantity =
                    itemIDs.Count,

                Status =
                    "Borrowed"
            };


            _context.Borrows.Add(
                borrow
            );


            await _context.SaveChangesAsync();


            // ========================================================
            // ADD BORROW ITEMS + REDUCE STOCK
            // ========================================================

            foreach (var item in items)
            {
                _context.BorrowItems.Add(
                    new BorrowItem
                    {
                        BorrowID =
                            borrow.BorrowID,

                        ItemID =
                            item.ItemID
                    }
                );


                item.Quantity -= 1;


                if (item.Quantity > 0)
                {
                    item.Status =
                        "Available";
                }
                else
                {
                    item.Status =
                        "Borrowed";
                }
            }


            // ========================================================
            // UPDATE RESERVATION
            // ========================================================

            reservation.Status =
                "Borrowed";

            reservation.Quantity =
                itemIDs.Count;


            await _context.SaveChangesAsync();


            // ========================================================
            // PAYMENT
            // ========================================================

            return RedirectToAction(
                "Create",
                "Payment",
                new
                {
                    borrowId =
                        borrow.BorrowID
                }
            );
        }


        // ============================================================
        // DELETE - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");


            var reservation =
                await _context.Reservations
                    .Include(r => r.Customer)
                    .Include(r => r.ReservationItems)
                        .ThenInclude(ri => ri.Item)
                    .FirstOrDefaultAsync(r =>
                        r.ReservationID == id);


            if (reservation == null)
                return NotFound();


            return View(reservation);
        }


        // ============================================================
        // DELETE - POST
        // ============================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Home");


            var reservation =
                await _context.Reservations
                    .Include(r => r.ReservationItems)
                    .FirstOrDefaultAsync(r =>
                        r.ReservationID == id);


            if (reservation == null)
                return NotFound();


            _context.ReservationItems.RemoveRange(
                reservation.ReservationItems
            );


            _context.Reservations.Remove(
                reservation
            );


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Reservation deleted successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ============================================================
        // LOAD AVAILABLE ITEMS
        // ============================================================

        private async Task LoadAvailableItems(
            List<int>? selectedIDs = null)
        {
            selectedIDs ??=
                new List<int>();


            // IMPORTANT:
            // Only Quantity determines if an item
            // is available for reservation.

            var items =
                await _context.Items
                    .Include(i => i.Category)
                    .Where(i =>
                        i.Quantity > 0)
                    .OrderBy(i =>
                        i.ItemName)
                    .ToListAsync();


            ViewBag.AvailableItems =
                items;

            ViewBag.SelectedItemIDs =
                selectedIDs;
        }


        // ============================================================
        // LOAD ITEMS FOR EDIT
        // ============================================================

        private async Task LoadItemsForEdit(
            List<int>? selectedIDs = null)
        {
            selectedIDs ??=
                new List<int>();


            var items =
                await _context.Items
                    .Include(i => i.Category)
                    .Where(i =>
                        i.Quantity > 0 ||
                        selectedIDs.Contains(i.ItemID))
                    .OrderBy(i =>
                        i.ItemName)
                    .ToListAsync();


            ViewBag.AvailableItems =
                items;

            ViewBag.SelectedItemIDs =
                selectedIDs;
        }
    }
}