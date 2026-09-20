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
        private readonly IWebHostEnvironment _environment;

        public ItemController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // =====================================================
        // ADMIN CHECK
        // =====================================================

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("AdminUser") != null;
        }


        // =====================================================
        // INDEX
        // =====================================================

        [HttpGet]
        public IActionResult Index(
            string? searchString,
            int? categoryId)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var items = _context.Items
                .Include(i => i.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                items = items.Where(i =>
                    i.ItemName.Contains(searchString) ||
                    i.Description.Contains(searchString));
            }

            if (categoryId.HasValue)
            {
                items = items.Where(i =>
                    i.CatID == categoryId.Value);
            }

            ViewBag.Categories = new SelectList(
                _context.Categories.ToList(),
                "CatID",
                "CatName",
                categoryId);

            return View(items.ToList());
        }


        // =====================================================
        // CREATE GET
        // =====================================================

        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            LoadCategories();

            return View();
        }


        // =====================================================
        // CREATE POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Item item,
            IFormFile? ImageFile)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            // ItemCode is generated automatically.
            ModelState.Remove(nameof(Item.ItemCode));


            // =================================================
            // IMAGE VALIDATION
            // =================================================

            if (ImageFile != null &&
                ImageFile.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

                var extension =
                    Path.GetExtension(
                        ImageFile.FileName)
                        .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");
                }

                if (ImageFile.Length >
                    5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Image must not exceed 5 MB.");
                }
            }


            if (!ModelState.IsValid)
            {
                LoadCategories(item.CatID);

                return View(item);
            }


            try
            {
                item.Status = "Available";
                item.ImagePath = "";


                // =================================================
                // SAVE ITEM FIRST
                // =================================================

                _context.Items.Add(item);

                await _context.SaveChangesAsync();


                // =================================================
                // GENERATE ITEM CODE
                // =================================================

                item.ItemCode =
                    GenerateItemCode(
                        item.CatID,
                        item.ItemID);


                // =================================================
                // SAVE IMAGE
                // =================================================

                if (ImageFile != null &&
                    ImageFile.Length > 0)
                {
                    item.ImagePath =
                        await SaveImageAsync(
                            ImageFile);
                }


                // =================================================
                // SAVE CODE + IMAGE PATH
                // =================================================

                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "Item added successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "========== ITEM CREATE ERROR ==========");

                Console.WriteLine(
                    ex.ToString());

                Console.WriteLine(
                    "========================================");


                ModelState.AddModelError(
                    "",
                    "The item could not be saved.");

                LoadCategories(item.CatID);

                return View(item);
            }
        }


        // =====================================================
        // SAVE IMAGE
        // =====================================================

        private async Task<string> SaveImageAsync(
            IFormFile imageFile)
        {
            var webRootPath =
                _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(
                webRootPath))
            {
                webRootPath =
                    Path.Combine(
                        _environment.ContentRootPath,
                        "wwwroot");
            }


            var folderPath =
                Path.Combine(
                    webRootPath,
                    "Images",
                    "Items");


            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(
                    folderPath);
            }


            var extension =
                Path.GetExtension(
                    imageFile.FileName)
                    .ToLowerInvariant();


            var fileName =
                Guid.NewGuid()
                    .ToString("N")
                + extension;


            var filePath =
                Path.Combine(
                    folderPath,
                    fileName);


            using var stream =
                new FileStream(
                    filePath,
                    FileMode.Create);


            await imageFile.CopyToAsync(
                stream);


            return "/Images/Items/" +
                   fileName;
        }


        // =====================================================
        // GENERATE ITEM CODE
        // =====================================================

        private string GenerateItemCode(
            int catId,
            int itemId)
        {
            var catName =
                _context.Categories
                    .Where(c =>
                        c.CatID == catId)
                    .Select(c =>
                        c.CatName)
                    .FirstOrDefault()
                ?? "";


            var prefix =
                GetCategoryPrefix(
                    catName);


            var existingCodes =
                _context.Items
                    .Where(i =>
                        i.CatID == catId &&
                        i.ItemID != itemId &&
                        i.ItemCode != null &&
                        i.ItemCode.StartsWith(
                            prefix + "-"))
                    .Select(i =>
                        i.ItemCode)
                    .ToList();


            var nextSequence = 1;


            if (existingCodes.Count > 0)
            {
                var maxSequence =
                    existingCodes
                        .Select(code =>
                        {
                            var parts =
                                code.Split('-');

                            if (parts.Length == 2 &&
                                int.TryParse(
                                    parts[1],
                                    out int number))
                            {
                                return number;
                            }

                            return 0;
                        })
                        .DefaultIfEmpty(0)
                        .Max();


                nextSequence =
                    maxSequence + 1;
            }


            return $"{prefix}-{nextSequence:D3}";
        }


        // =====================================================
        // CATEGORY PREFIX
        // =====================================================

        private static readonly
            Dictionary<string, string>
            KnownCategoryPrefixes = new()
            {
                { "tuxedo", "TUX" },
                { "suit", "SUI" },
                { "gown", "GOW" },
                { "accessory", "ACC" },
                { "accessories", "ACC" },
                { "barong", "BAR" },
                { "dress", "DRS" }
            };


        private static string GetCategoryPrefix(
            string catName)
        {
            var normalized =
                (catName ?? "")
                    .Trim()
                    .ToLowerInvariant();


            if (KnownCategoryPrefixes.TryGetValue(
                normalized,
                out var knownPrefix))
            {
                return knownPrefix;
            }


            var letters =
                new string(
                    normalized
                        .Where(char.IsLetter)
                        .ToArray())
                    .ToUpperInvariant();


            if (string.IsNullOrWhiteSpace(
                letters))
            {
                return "ITM";
            }


            return letters.Length >= 3
                ? letters.Substring(0, 3)
                : letters.PadRight(
                    3,
                    'X');
        }


        // =====================================================
        // EDIT GET
        // =====================================================

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var item =
                _context.Items
                    .Include(i =>
                        i.Category)
                    .FirstOrDefault(i =>
                        i.ItemID == id);


            if (item == null)
            {
                return NotFound();
            }


            LoadCategories(
                item.CatID);


            return View(item);
        }


        // =====================================================
        // EDIT POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Item item,
            IFormFile? ImageFile)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var existingItem =
                await _context.Items
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i =>
                        i.ItemID == item.ItemID);


            if (existingItem == null)
            {
                return NotFound();
            }


            // Keep original item code.
            item.ItemCode =
                existingItem.ItemCode;


            ModelState.Remove(
                nameof(Item.ItemCode));


            // =================================================
            // VALIDATE NEW IMAGE
            // =================================================

            if (ImageFile != null &&
                ImageFile.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };


                var extension =
                    Path.GetExtension(
                        ImageFile.FileName)
                        .ToLowerInvariant();


                if (!allowedExtensions.Contains(
                    extension))
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");
                }


                if (ImageFile.Length >
                    5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Image must not exceed 5 MB.");
                }
            }


            if (!ModelState.IsValid)
            {
                LoadCategories(
                    item.CatID);

                return View(item);
            }


            try
            {
                // Keep existing image.
                item.ImagePath =
                    existingItem.ImagePath;


                // Replace image if a new one was selected.
                if (ImageFile != null &&
                    ImageFile.Length > 0)
                {
                    item.ImagePath =
                        await SaveImageAsync(
                            ImageFile);
                }


                _context.Items.Update(item);

                await _context.SaveChangesAsync();


                // Delete old image only after
                // successful database update.
                if (ImageFile != null &&
                    ImageFile.Length > 0 &&
                    !string.IsNullOrWhiteSpace(
                        existingItem.ImagePath))
                {
                    DeleteImage(
                        existingItem.ImagePath);
                }


                TempData["Success"] =
                    "Item updated successfully.";


                return RedirectToAction(
                    nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "========== ITEM EDIT ERROR ==========");

                Console.WriteLine(
                    ex.ToString());

                Console.WriteLine(
                    "======================================");


                ModelState.AddModelError(
                    "",
                    "The item could not be updated.");


                LoadCategories(
                    item.CatID);


                return View(item);
            }
        }


        // =====================================================
        // DELETE GET
        // =====================================================

        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var item =
                _context.Items
                    .Include(i =>
                        i.Category)
                    .FirstOrDefault(i =>
                        i.ItemID == id);


            if (item == null)
            {
                return NotFound();
            }


            return View(item);
        }


        // =====================================================
        // DELETE POST
        // =====================================================

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


            // =================================================
            // FIND ITEM
            // =================================================

            var item =
                await _context.Items
                    .FirstOrDefaultAsync(i =>
                        i.ItemID == id);


            if (item == null)
            {
                TempData["Error"] =
                    "Item not found.";

                return RedirectToAction(
                    nameof(Index));
            }


            var imagePath =
                item.ImagePath;


            // =================================================
            // START TRANSACTION
            // =================================================

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();


            try
            {
                // =================================================
                // FIND BORROWITEM RECORDS
                // =================================================

                var borrowItems =
                    await _context.BorrowItems
                        .Where(bi =>
                            bi.ItemID == id)
                        .ToListAsync();


                // Get the Borrow IDs that use this item.
                var borrowIds =
                    borrowItems
                        .Select(bi =>
                            bi.BorrowID)
                        .Distinct()
                        .ToList();


                // =================================================
                // PROCESS EACH BORROW
                // =================================================

                foreach (var borrowId in borrowIds)
                {
                    var borrow =
                        await _context.Borrows
                            .Include(b =>
                                b.BorrowItems)
                            .FirstOrDefaultAsync(
                                b =>
                                    b.BorrowID ==
                                    borrowId);


                    if (borrow == null)
                    {
                        continue;
                    }


                    // Find all items belonging to
                    // this particular borrow.
                    var remainingBorrowItems =
                        borrow.BorrowItems
                            .Where(bi =>
                                bi.ItemID != id)
                            .ToList();


                    // =================================================
                    // CASE 1:
                    // THIS BORROW ONLY HAS THIS ITEM
                    // =================================================

                    if (remainingBorrowItems.Count == 0)
                    {
                        // Delete payments first because
                        // Payments reference Borrow.
                        var payments =
                            await _context.Payments
                                .Where(p =>
                                    p.BorrowID ==
                                    borrow.BorrowID)
                                .ToListAsync();


                        if (payments.Any())
                        {
                            _context.Payments
                                .RemoveRange(
                                    payments);
                        }


                        // Delete penalties because
                        // Penalties reference Borrow.
                        var penalties =
                            await _context.Penalties
                                .Where(p =>
                                    p.BorrowID ==
                                    borrow.BorrowID)
                                .ToListAsync();


                        if (penalties.Any())
                        {
                            _context.Penalties
                                .RemoveRange(
                                    penalties);
                        }


                        // Delete BorrowItems.
                        _context.BorrowItems
                            .RemoveRange(
                                borrow.BorrowItems);


                        // Finally delete Borrow.
                        _context.Borrows
                            .Remove(borrow);
                    }


                    // =================================================
                    // CASE 2:
                    // THIS BORROW HAS OTHER ITEMS
                    // =================================================

                    else
                    {
                        // Remove only the BorrowItem
                        // belonging to the item being deleted.
                        var itemLinksToDelete =
                            borrow.BorrowItems
                                .Where(bi =>
                                    bi.ItemID == id)
                                .ToList();


                        if (itemLinksToDelete.Any())
                        {
                            _context.BorrowItems
                                .RemoveRange(
                                    itemLinksToDelete);
                        }


                        // Borrow.ItemID is also a foreign key
                        // to Items, so if it points to the
                        // item being deleted, move it to one
                        // of the remaining items.
                        if (borrow.ItemID == id)
                        {
                            var replacementItemID =
                                remainingBorrowItems
                                    .Select(bi =>
                                        bi.ItemID)
                                    .FirstOrDefault();


                            if (replacementItemID != 0)
                            {
                                borrow.ItemID =
                                    replacementItemID;
                            }
                        }
                    }
                }


                // =================================================
                // SAVE BORROW-RELATED CHANGES
                // =================================================

                await _context.SaveChangesAsync();


                // =================================================
                // DELETE ITEM
                // =================================================

                _context.Items.Remove(item);

                await _context.SaveChangesAsync();


                // =================================================
                // COMMIT TRANSACTION
                // =================================================

                await transaction.CommitAsync();


                // =================================================
                // DELETE IMAGE FILE
                // =================================================

                DeleteImage(
                    imagePath);


                TempData["Success"] =
                    "Item deleted successfully.";


                return RedirectToAction(
                    nameof(Index));
            }
            catch (Exception ex)
            {
                // =================================================
                // ROLLBACK
                // =================================================

                await transaction.RollbackAsync();


                Console.WriteLine(
                    "========== ITEM DELETE ERROR ==========");

                Console.WriteLine(
                    ex.ToString());

                Console.WriteLine(
                    "========================================");


                TempData["Error"] =
                    "The item could not be deleted. " +
                    "Check the application output for details.";


                return RedirectToAction(
                    nameof(Index));
            }
        }


        // =====================================================
        // DELETE IMAGE
        // =====================================================

        private void DeleteImage(
            string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(
                imagePath))
            {
                return;
            }


            // Only delete files belonging
            // to our item-image folder.
            if (!imagePath.StartsWith(
                "/Images/Items/",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }


            var fileName =
                Path.GetFileName(
                    imagePath);


            if (string.IsNullOrWhiteSpace(
                fileName))
            {
                return;
            }


            var webRootPath =
                _environment.WebRootPath;


            if (string.IsNullOrWhiteSpace(
                webRootPath))
            {
                webRootPath =
                    Path.Combine(
                        _environment.ContentRootPath,
                        "wwwroot");
            }


            var filePath =
                Path.Combine(
                    webRootPath,
                    "Images",
                    "Items",
                    fileName);


            if (System.IO.File.Exists(
                filePath))
            {
                try
                {
                    System.IO.File.Delete(
                        filePath);
                }
                catch
                {
                    // Do not crash the application
                    // if the physical image cannot
                    // be removed.
                }
            }
        }


        // =====================================================
        // DETAILS
        // =====================================================

        [HttpGet]
        public IActionResult Details(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var item =
                _context.Items
                    .Include(i =>
                        i.Category)
                    .FirstOrDefault(i =>
                        i.ItemID == id);


            if (item == null)
            {
                return NotFound();
            }


            return View(item);
        }


        // =====================================================
        // SUITS
        // =====================================================

        public IActionResult Suits()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var suits =
                _context.Items
                    .Include(i =>
                        i.Category)
                    .Where(i =>
                        i.Category != null &&
                        i.Category.CatName == "Suit")
                    .ToList();


            return View(suits);
        }


        // =====================================================
        // GOWNS
        // =====================================================

        public IActionResult Gowns()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var gowns =
                _context.Items
                    .Include(i =>
                        i.Category)
                    .Where(i =>
                        i.Category != null &&
                        i.Category.CatName == "Gown")
                    .ToList();


            return View(gowns);
        }


        // =====================================================
        // LOAD CATEGORIES
        // =====================================================

        private void LoadCategories(
            int? selectedCategory = null)
        {
            ViewBag.Categories =
                new SelectList(
                    _context.Categories.ToList(),
                    "CatID",
                    "CatName",
                    selectedCategory);
        }
    }
}   