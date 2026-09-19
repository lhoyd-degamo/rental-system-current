using CRUD.Data;
using CRUD.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace CRUD.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }
        private bool IsAdmin() => HttpContext.Session.GetString("AdminUser") != null;

        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            var categories = _context.Categories
                .OrderBy(c => c.CatName)
                .ToList();

            ViewBag.ItemCounts = categories.ToDictionary(
                c => c.CatID,
                c => _context.Items.Count(i => i.CatID == c.CatID));

            return View(categories);
        }
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: Category/Edit/5
        public IActionResult Edit(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            var category = _context.Categories.Find(id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category category)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            if (ModelState.IsValid)
            {
                _context.Categories.Update(category);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: Category/Delete/5
        public IActionResult Delete(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            var category = _context.Categories.Find(id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            var category = _context.Categories.Find(id);

            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Category/Details/5
        public IActionResult Details(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            var category = _context.Categories.FirstOrDefault(c => c.CatID == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }
    }
}