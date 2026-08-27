using CRUD.Data;
using CRUD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Controllers
{
    public class ItemController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ItemController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Item
        public IActionResult Index(string searchString, int? categoryId)
        {
            var items = _context.Items
        .Include(i => i.Category)
        .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                items = items.Where(i =>
                    i.ItemName.Contains(searchString) ||
                    i.Description.Contains(searchString));
            }


            if (categoryId != null)
            {
                items = items.Where(i => i.CatID == categoryId);
            }


            ViewBag.Categories = new SelectList(
                _context.Categories.ToList(),
                "CatID",
                "CatName",
                categoryId
            );

            return View(items.ToList());
        }


        // GET: Item/Create
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(
                _context.Categories.ToList(),
                "CatID",
                "CatName"
            );

            return View();
        }


        // POST: Item/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Item item)
        {
            // ItemCode is generated automatically, not submitted by the form
            ModelState.Remove(nameof(Item.ItemCode));

            if (ModelState.IsValid)
            {
                _context.Items.Add(item);
                _context.SaveChanges();

                // Now that the item has an ItemID, generate its code
                // (e.g. "TUX001" for a Tuxedo, "GWN014" for a Gown)
                item.ItemCode = GenerateItemCode(item.CatID, item.ItemID);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(
                _context.Categories.ToList(),
                "CatID",
                "CatName",
                item.CatID
            );

            return View(item);
        }

        // Builds a readable code like "tux-001" from the category name.
        // The numeric part is scoped to the category, so each category
        // (tuxedo, gown, accessories, ...) keeps its own running count.
        private string GenerateItemCode(int catId, int itemId)
        {
            var catName = _context.Categories
                .Where(c => c.CatID == catId)
                .Select(c => c.CatName)
                .FirstOrDefault() ?? "";

            var prefix = GetCategoryPrefix(catName);

            // Look at codes already used within this category to find the
            // next number in the sequence (e.g. tux-001, tux-002, tux-003...)
            var existingCodes = _context.Items
                .Where(i => i.CatID == catId
                    && i.ItemID != itemId
                    && i.ItemCode != null
                    && i.ItemCode.StartsWith(prefix + "-"))
                .Select(i => i.ItemCode)
                .ToList();

            var nextSeq = 1;
            if (existingCodes.Count > 0)
            {
                var maxSeq = existingCodes
                    .Select(code =>
                    {
                        var parts = code.Split('-');
                        return parts.Length == 2 && int.TryParse(parts[1], out var n) ? n : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                nextSeq = maxSeq + 1;
            }

            return $"{prefix}-{nextSeq:D3}";
        }

        // Maps a category name to its short uppercase code prefix.
        // e.g. "Tuxedo" -> "TUX", "Gown" -> "GOW", "Accessories" -> "ACC"
        private static readonly Dictionary<string, string> KnownCategoryPrefixes = new()
        {
            { "tuxedo", "TUX" },
            { "suit", "SUI" },
            { "gown", "GOW" },
            { "accessory", "ACC" },
            { "accessories", "ACC" },
            { "barong", "BAR" },
            { "dress", "DRS" },
        };

        private static string GetCategoryPrefix(string catName)
        {
            var normalized = (catName ?? "").Trim().ToLowerInvariant();

            if (KnownCategoryPrefixes.TryGetValue(normalized, out var known))
            {
                return known;
            }

            var letters = new string(normalized.Where(char.IsLetter).ToArray()).ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(letters))
            {
                return "ITM";
            }

            return letters.Length >= 3
                ? letters.Substring(0, 3)
                : letters.PadRight(3, 'X');
        }



        // GET: Item/Edit/5
        public IActionResult Edit(int id)
        {
            var item = _context.Items.Find(id);

            if (item == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(
                _context.Categories.ToList(),
                "CatID",
                "CatName",
                item.CatID
            );

            return View(item);
        }



        // POST: Item/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Item item)
        {
            // Keep the original item code even if it wasn't posted back
            if (string.IsNullOrWhiteSpace(item.ItemCode))
            {
                var existingCode = _context.Items
                    .AsNoTracking()
                    .Where(i => i.ItemID == item.ItemID)
                    .Select(i => i.ItemCode)
                    .FirstOrDefault();

                item.ItemCode = !string.IsNullOrWhiteSpace(existingCode)
                    ? existingCode
                    : GenerateItemCode(item.CatID, item.ItemID);
            }

            ModelState.Remove(nameof(Item.ItemCode));

            if (ModelState.IsValid)
            {
                _context.Items.Update(item);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(
                _context.Categories.ToList(),
                "CatID",
                "CatName",
                item.CatID
            );

            return View(item);
        }



        // GET: Item/Delete/5
        public IActionResult Delete(int id)
        {
            var item = _context.Items
                .Include(i => i.Category)
                .FirstOrDefault(i => i.ItemID == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }



        // POST: Item/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var item = _context.Items.Find(id);

            if (item != null)
            {
                _context.Items.Remove(item);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }



        // GET: Item/Details/5
        public IActionResult Details(int id)
        {
            var item = _context.Items
                .Include(i => i.Category)
                .FirstOrDefault(i => i.ItemID == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        public IActionResult Suits()
        {
            var suits = _context.Items
                .Where(i => i.Category.CatName == "Suit")
                .ToList();

            return View(suits);
        }

        public IActionResult Gowns()
        {
            var gowns = _context.Items
                .Where(i => i.Category.CatName == "Gown")
                .ToList();

            return View(gowns);
        }


    }


}
