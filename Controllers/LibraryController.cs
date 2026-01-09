using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mist.Data;
using mist.Services;
using mist.ViewModels;
using System.Security.Claims;

namespace mist.Controllers
{
    [Authorize]
    public class LibraryController : Controller
    {
        private readonly IPurchaseService _purchaseService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public LibraryController(IPurchaseService purchaseService, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _purchaseService = purchaseService;
            _context = context;
            _environment = environment;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // GET: Library/Download/5
        public async Task<IActionResult> Download(int id)
        {
            var userId = GetUserId();
            
            // Sprawdź czy użytkownik posiada grę
            var hasPurchased = await _context.Purchases.AnyAsync(p => p.UserId == userId && p.GameId == id);
            if (!hasPurchased)
            {
                return Forbid();
            }
            
            var game = await _context.Games.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }
            
            if (string.IsNullOrEmpty(game.DownloadUrl))
            {
                TempData["ErrorMessage"] = "Gra nie jest obecnie dostępna do pobrania.";
                return RedirectToAction(nameof(Index));
            }
            
            // Pobierz ścieżkę do pliku
            var filePath = game.DownloadUrl.TrimStart('/');
            var fullPath = Path.Combine(_environment.WebRootPath, filePath);
            
            if (!System.IO.File.Exists(fullPath))
            {
                TempData["ErrorMessage"] = "Gra nie jest obecnie dostępna do pobrania.";
                return RedirectToAction(nameof(Index));
            }
            
            // Pobierz rozszerzenie pliku
            var extension = Path.GetExtension(fullPath);
            var contentType = extension.ToLower() switch
            {
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                ".exe" => "application/x-msdownload",
                ".msi" => "application/x-msi",
                _ => "application/octet-stream"
            };
            
            // Nazwa pliku do pobrania
            var downloadFileName = $"{game.Title.Replace(" ", "_")}{extension}";
            
            return PhysicalFile(fullPath, contentType, downloadFileName);
        }

        // GET: Library
        public async Task<IActionResult> Index(LibrarySearchViewModel searchModel)
        {
            var userId = GetUserId();
            var gamesQuery = _context.Games
                .Include(g => g.GameTags)
                    .ThenInclude(gt => gt.Tag)
                .Where(g => g.Purchases.Any(p => p.UserId == userId));
            
            var games = await gamesQuery.ToListAsync();

            // Filtrowanie po wyszukiwanej frazie
            if (!string.IsNullOrWhiteSpace(searchModel.SearchTerm))
            {
                var searchTerm = searchModel.SearchTerm.ToLower();
                games = games.Where(g => 
                    g.Title.ToLower().Contains(searchTerm) ||
                    g.Developer.ToLower().Contains(searchTerm)
                ).ToList();
            }

            // Filtrowanie po tagach
            if (searchModel.TagIds != null && searchModel.TagIds.Any())
            {
                games = games.Where(g => 
                    searchModel.TagIds.All(tagId => g.GameTags.Any(gt => gt.TagId == tagId))
                ).ToList();
            }

            // Sortowanie
            games = searchModel.SortBy switch
            {
                "name" => games.OrderBy(g => g.Title).ToList(),
                "tag" => games.OrderBy(g => g.GameTags.FirstOrDefault()?.Tag?.Name ?? "").ThenBy(g => g.Title).ToList(),
                _ => games.OrderByDescending(g => g.CreatedAt).ToList() // recent (default)
            };

            // Pobierz dostępne tagi dla filtrów (tylko te które są w grach użytkownika)
            var allTagIds = games.SelectMany(g => g.GameTags.Select(gt => gt.TagId)).Distinct();
            ViewBag.Tags = await _context.Tags
                .Where(t => allTagIds.Contains(t.Id))
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.SearchModel = searchModel;
            ViewBag.TotalGames = await gamesQuery.CountAsync();
            ViewBag.FilteredGames = games.Count;

            return View(games);
        }

        // GET: Library/Purchases
        public async Task<IActionResult> Purchases()
        {
            var userId = GetUserId();
            var purchases = await _purchaseService.GetUserPurchasesAsync(userId);
            return View(purchases);
        }
    }
}