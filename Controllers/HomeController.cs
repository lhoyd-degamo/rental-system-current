using System.Text.Json;
using CRUD.Data;
using CRUD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // HOME
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.CatName)
                .ToListAsync();

            var availableItems = await _context.Items
                .Include(i => i.Category)
                .Where(i =>
                    i.Quantity > 0 &&
                    i.Status == "Available")
                .OrderBy(i => i.CatID)
                .ThenBy(i => i.ItemName)
                .ToListAsync();

            ViewBag.AvailableItems = availableItems;

            return View(categories);
        }


        // =========================================================
        // CATEGORY
        // =========================================================

        public async Task<IActionResult> Category(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(
                    c => c.CatID == id);

            if (category == null)
            {
                return NotFound();
            }

            var items = await _context.Items
                .Include(i => i.Category)
                .Where(i => i.CatID == id)
                .OrderBy(i => i.ItemName)
                .ToListAsync();

            ViewBag.CategoryName = category.CatName;
            ViewBag.CategoryID = category.CatID;

            return View(items);
        }


        // =========================================================
        // ITEM DETAILS
        // =========================================================

        public async Task<IActionResult> ItemDetails(int id)
        {
            var item = await _context.Items
                .Include(i => i.Category)
                .FirstOrDefaultAsync(
                    i => i.ItemID == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }


        // =========================================================
        // CUSTOMER GET
        // =========================================================

        [HttpGet]
        public IActionResult Customer()
        {
            var customerID =
                HttpContext.Session.GetInt32("CustomerID");

            if (customerID != null)
            {
                return RedirectToAction(
                    nameof(CustomerDashboard));
            }

            return View(new Customer());
        }


        // =========================================================
        // CUSTOMER POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Customer(
            Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            var existing = await _context.Customers
                .FirstOrDefaultAsync(
                    c => c.Email == customer.Email);

            if (existing == null)
            {
                _context.Customers.Add(customer);

                await _context.SaveChangesAsync();
            }
            else
            {
                existing.CustomerName =
                    customer.CustomerName;

                existing.PhoneNumber =
                    customer.PhoneNumber;

                existing.Address =
                    customer.Address;

                existing.IDType =
                    customer.IDType;

                existing.IDNumber =
                    customer.IDNumber;

                await _context.SaveChangesAsync();

                customer = existing;
            }

            HttpContext.Session.SetInt32(
                "CustomerID",
                customer.CustomerID);

            return RedirectToAction(
                nameof(CustomerDashboard));
        }


        // =========================================================
        // CUSTOMER DASHBOARD
        // =========================================================

        public async Task<IActionResult> CustomerDashboard()
        {
            var customerID =
                HttpContext.Session.GetInt32("CustomerID");

            if (customerID == null)
            {
                return RedirectToAction(
                    nameof(Customer));
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(
                    c => c.CustomerID == customerID.Value);

            if (customer == null)
            {
                HttpContext.Session.Remove("CustomerID");
                HttpContext.Session.Remove("BookingCart");

                return RedirectToAction(
                    nameof(Customer));
            }

            // =====================================================
            // CUSTOMER BOOKINGS
            // =====================================================

            var bookings = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                        .ThenInclude(i => i.Category)
                .Where(
                    b => b.CustomerID == customerID.Value)
                .OrderByDescending(
                    b => b.BorrowID)
                .ToListAsync();


            // =====================================================
            // AVAILABLE ITEMS
            // =====================================================

            var availableItems = await _context.Items
                .Include(i => i.Category)
                .Where(i =>
                    i.Quantity > 0 &&
                    i.Status == "Available")
                .OrderBy(i => i.CatID)
                .ThenBy(i => i.ItemName)
                .ToListAsync();


            // =====================================================
            // CART COUNT
            // =====================================================

            var cart = GetCart();


            ViewBag.Customer =
                customer;

            ViewBag.AvailableItems =
                availableItems;

            ViewBag.CartCount =
                cart.Count;


            return View(bookings);
        }


        // =========================================================
        // ADD ITEM TO BOOKING CART
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> AddToCart(int id)
        {
            var customerID =
                HttpContext.Session.GetInt32("CustomerID");

            // Customer must be logged in
            if (customerID == null)
            {
                return RedirectToAction(
                    nameof(Customer));
            }


            // Find item
            var item = await _context.Items
                .FirstOrDefaultAsync(
                    i => i.ItemID == id);


            if (item == null)
            {
                return NotFound();
            }


            // Check availability
            if (item.Quantity <= 0 ||
                item.Status != "Available")
            {
                TempData["CartError"] =
                    "This item is currently unavailable.";

                return RedirectToAction(
                    nameof(CustomerDashboard));
            }


            // Get current cart
            var cart = GetCart();


            // Don't allow duplicate item
            if (cart.Contains(id))
            {
                TempData["CartError"] =
                    "This item is already in your booking cart.";

                return RedirectToAction(
                    nameof(CustomerDashboard));
            }


            // Add item to cart
            cart.Add(id);


            // Save cart
            SaveCart(cart);


            TempData["CartSuccess"] =
                $"{item.ItemName} was added to your booking cart.";


            // Stay on dashboard so customer can continue
            // adding more items
            return RedirectToAction(
                nameof(CustomerDashboard));
        }


        // =========================================================
        // BOOKING CART
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> BookingCart()
        {
            var customerID =
                HttpContext.Session.GetInt32("CustomerID");

            if (customerID == null)
            {
                return RedirectToAction(
                    nameof(Customer));
            }


            var cart = GetCart();


            if (!cart.Any())
            {
                return View(new List<Item>());
            }


            var items = await _context.Items
                .Include(i => i.Category)
                .Where(i =>
                    cart.Contains(i.ItemID))
                .OrderBy(i => i.ItemName)
                .ToListAsync();


            return View(items);
        }


        // =========================================================
        // REMOVE ONE ITEM FROM CART
        // =========================================================

        [HttpGet]
        public IActionResult RemoveFromCart(int id)
        {
            var customerID =
                HttpContext.Session.GetInt32("CustomerID");

            if (customerID == null)
            {
                return RedirectToAction(
                    nameof(Customer));
            }


            var cart = GetCart();


            cart.Remove(id);


            SaveCart(cart);


            return RedirectToAction(
                nameof(BookingCart));
        }


        // =========================================================
        // CLEAR CART
        // =========================================================

        [HttpGet]
        public IActionResult ClearCart()
        {
            var customerID =
                HttpContext.Session.GetInt32("CustomerID");

            if (customerID == null)
            {
                return RedirectToAction(
                    nameof(Customer));
            }


            HttpContext.Session.Remove(
                "BookingCart");


            return RedirectToAction(
                nameof(BookingCart));
        }


        // =========================================================
        // CONFIRM BOOKING
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(
            DateTime borrowDate,
            DateTime returnDate)
        {
            var customerID =
                HttpContext.Session.GetInt32("CustomerID");


            if (customerID == null)
            {
                return RedirectToAction(
                    nameof(Customer));
            }


            // =====================================================
            // VALIDATE DATES
            // =====================================================

            if (borrowDate.Date < DateTime.Today)
            {
                TempData["CartError"] =
                    "Borrow date cannot be before today.";

                return RedirectToAction(
                    nameof(BookingCart));
            }


            if (returnDate.Date <= borrowDate.Date)
            {
                TempData["CartError"] =
                    "Return date must be after the borrow date.";

                return RedirectToAction(
                    nameof(BookingCart));
            }


            // =====================================================
            // GET CART
            // =====================================================

            var cart = GetCart();


            if (!cart.Any())
            {
                TempData["CartError"] =
                    "Your booking cart is empty.";

                return RedirectToAction(
                    nameof(BookingCart));
            }


            // =====================================================
            // GET CUSTOMER
            // =====================================================

            var customer = await _context.Customers
                .FirstOrDefaultAsync(
                    c => c.CustomerID ==
                         customerID.Value);


            if (customer == null)
            {
                HttpContext.Session.Remove(
                    "CustomerID");

                HttpContext.Session.Remove(
                    "BookingCart");

                return RedirectToAction(
                    nameof(Customer));
            }


            // =====================================================
            // GET ITEMS FROM CART
            // =====================================================

            var items = await _context.Items
                .Where(i =>
                    cart.Contains(i.ItemID))
                .ToListAsync();


            if (!items.Any())
            {
                TempData["CartError"] =
                    "The items in your cart could not be found.";

                HttpContext.Session.Remove(
                    "BookingCart");

                return RedirectToAction(
                    nameof(BookingCart));
            }


            // =====================================================
            // CHECK AVAILABILITY AGAIN
            // =====================================================

            foreach (var item in items)
            {
                if (item.Quantity <= 0 ||
                    item.Status != "Available")
                {
                    TempData["CartError"] =
                        $"The item '{item.ItemName}' " +
                        "is no longer available.";

                    return RedirectToAction(
                        nameof(BookingCart));
                }
            }


            // =====================================================
            // CREATE ONE BORROW
            // =====================================================

            var borrow = new Borrow
            {
                CustomerID =
                    customer.CustomerID,

                BorrowDate =
                    borrowDate.Date,

                ReturnDate =
                    returnDate.Date,

                Quantity =
                    items.Count,

                Status =
                    "Booked",

                ItemID =
                    items.First().ItemID
            };


            _context.Borrows.Add(
                borrow);


            await _context.SaveChangesAsync();


            // =====================================================
            // CREATE MULTIPLE BORROW ITEMS
            // =====================================================

            foreach (var item in items)
            {
                var borrowItem =
                    new BorrowItem
                    {
                        BorrowID =
                            borrow.BorrowID,

                        ItemID =
                            item.ItemID
                    };


                _context.BorrowItems.Add(
                    borrowItem);
            }


            await _context.SaveChangesAsync();


            // =====================================================
            // CLEAR CART
            // =====================================================

            HttpContext.Session.Remove(
                "BookingCart");


            // =====================================================
            // SUCCESS MESSAGE
            // =====================================================

            TempData["BookingSuccess"] =
                $"Booking #{borrow.BorrowID} " +
                "was successfully created.";


            return RedirectToAction(
                nameof(CustomerDashboard));
        }


        // =========================================================
        // CUSTOMER LOGOUT
        // =========================================================

        public IActionResult CustomerLogout()
        {
            HttpContext.Session.Remove(
                "CustomerID");

            HttpContext.Session.Remove(
                "BookingCart");

            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // GET CART
        // =========================================================

        private List<int> GetCart()
        {
            var cartJson =
                HttpContext.Session.GetString(
                    "BookingCart");


            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<int>();
            }


            try
            {
                return JsonSerializer.Deserialize<List<int>>(
                           cartJson)
                       ?? new List<int>();
            }
            catch
            {
                HttpContext.Session.Remove(
                    "BookingCart");

                return new List<int>();
            }
        }


        // =========================================================
        // SAVE CART
        // =========================================================

        private void SaveCart(List<int> cart)
        {
            HttpContext.Session.SetString(
                "BookingCart",
                JsonSerializer.Serialize(cart));
        }
    }
}