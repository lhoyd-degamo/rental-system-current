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

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.CatName)
                .ToListAsync();

            return View(categories);
        }

        public async Task<IActionResult> Category(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CatID == id);

            if (category == null)
                return NotFound();

            var items = await _context.Items
                .Include(i => i.Category)
                .Where(i => i.CatID == id)
                .OrderBy(i => i.ItemName)
                .ToListAsync();

            ViewBag.CategoryName = category.CatName;
            ViewBag.CategoryID = category.CatID;

            return View(items);
        }

        public async Task<IActionResult> ItemDetails(int id)
        {
            var item = await _context.Items
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.ItemID == id);

            if (item == null)
                return NotFound();

            return View(item);
        }

        [HttpGet]
        public IActionResult Customer()
        {
            var customerID = HttpContext.Session.GetInt32("CustomerID");

            if (customerID != null)
                return RedirectToAction(nameof(CustomerDashboard));

            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Customer(Customer customer)
        {
            if (!ModelState.IsValid)
                return View(customer);

            var existing = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == customer.Email);

            if (existing == null)
            {
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }
            else
            {
                existing.CustomerName = customer.CustomerName;
                existing.PhoneNumber = customer.PhoneNumber;
                existing.Address = customer.Address;
                await _context.SaveChangesAsync();
                customer = existing;
            }

            HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
            return RedirectToAction(nameof(CustomerDashboard));
        }

        public async Task<IActionResult> CustomerDashboard()
        {
            var customerID = HttpContext.Session.GetInt32("CustomerID");

            if (customerID == null)
                return RedirectToAction(nameof(Customer));

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerID == customerID.Value);

            if (customer == null)
            {
                HttpContext.Session.Remove("CustomerID");
                return RedirectToAction(nameof(Customer));
            }

            var bookings = await _context.Borrows
                .Include(b => b.Customer)
                .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Item)
                        .ThenInclude(i => i.Category)
                .Where(b => b.CustomerID == customerID.Value)
                .OrderByDescending(b => b.BorrowID)
                .ToListAsync();

            // Show all items so the customer can see both availability and price.
            var items = await _context.Items
                .Include(i => i.Category)
                .OrderBy(i => i.CatID)
                .ThenBy(i => i.ItemName)
                .ToListAsync();

            ViewBag.Customer = customer;
            ViewBag.AvailableItems = items;

            return View(bookings);
        }

        public IActionResult CustomerLogout()
        {
            HttpContext.Session.Remove("CustomerID");
            return RedirectToAction(nameof(Index));
        }
    }
}
