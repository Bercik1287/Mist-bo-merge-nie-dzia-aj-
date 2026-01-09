using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mist.Data;
using mist.Models;
using System.Text.Json.Serialization;

namespace mist.Controllers
{
    public class QuickTagCreateModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    [Authorize(Roles = "Admin")]
    public class TagsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TagsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tags
        public async Task<IActionResult> Index()
        {
            var tags = await _context.Tags
                .Include(t => t.GameTags)
                .OrderBy(t => t.Name)
                .ToListAsync();
            return View(tags);
        }

        // GET: Tags/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tags/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tag tag)
        {
            if (ModelState.IsValid)
            {
                // Sprawdź czy tag o tej nazwie już istnieje
                if (await _context.Tags.AnyAsync(t => t.Name.ToLower() == tag.Name.ToLower()))
                {
                    ModelState.AddModelError("Name", "Tag o tej nazwie już istnieje.");
                    return View(tag);
                }

                _context.Add(tag);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tag '{tag.Name}' został utworzony pomyślnie!";
                return RedirectToAction(nameof(Index));
            }
            return View(tag);
        }

        // GET: Tags/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }

        // POST: Tags/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tag tag)
        {
            if (id != tag.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Sprawdź czy tag o tej nazwie już istnieje (inny niż edytowany)
                if (await _context.Tags.AnyAsync(t => t.Name.ToLower() == tag.Name.ToLower() && t.Id != tag.Id))
                {
                    ModelState.AddModelError("Name", "Tag o tej nazwie już istnieje.");
                    return View(tag);
                }

                try
                {
                    _context.Update(tag);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Tag '{tag.Name}' został zaktualizowany!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TagExists(tag.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tag);
        }

        // GET: Tags/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags
                .Include(t => t.GameTags)
                    .ThenInclude(gt => gt.Game)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (tag == null)
            {
                return NotFound();
            }

            return View(tag);
        }

        // POST: Tags/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag != null)
            {
                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tag '{tag.Name}' został usunięty!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Tags/GetAllTags - API do pobierania tagów (do użycia w formularzach)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTags()
        {
            var tags = await _context.Tags
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();
            return Json(tags);
        }

        // POST: Tags/QuickCreate - API do szybkiego tworzenia tagów
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> QuickCreate([FromBody] QuickTagCreateModel model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Name))
                {
                    return Json(new { success = false, message = "Nazwa tagu jest wymagana." });
                }

                var tagName = model.Name.Trim();
                
                // Sprawdź czy tag o tej nazwie już istnieje
                var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower());
                if (existingTag != null)
                {
                    return Json(new { success = false, message = "Tag o tej nazwie już istnieje.", existingId = existingTag.Id });
                }

                var tag = new Tag { Name = tagName };
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();

                return Json(new { success = true, id = tag.Id, name = tag.Name });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Błąd serwera: " + ex.Message });
            }
        }

        private bool TagExists(int id)
        {
            return _context.Tags.Any(e => e.Id == id);
        }
    }
}
