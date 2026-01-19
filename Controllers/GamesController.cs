using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mist.Data;
using mist.Models;
using mist.ViewModels;
using mist.Services;
using Microsoft.AspNetCore.Authorization;

namespace mist.Controllers
{
    public class GamesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;

        public GamesController(ApplicationDbContext context, IFileUploadService fileUploadService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
        }

        // GET: Games
        public async Task<IActionResult> Index(GameSearchViewModel searchModel)
        {
            var query = _context.Games
                .Include(g => g.Promotions)
                .Include(g => g.GameTags)
                    .ThenInclude(gt => gt.Tag)
                .Where(g => g.IsActive)
                .AsQueryable();

            // Filtrowanie po wyszukiwanej frazie
            if (!string.IsNullOrWhiteSpace(searchModel.SearchTerm))
            {
                var searchTerm = searchModel.SearchTerm.ToLower();
                query = query.Where(g => 
                    g.Title.ToLower().Contains(searchTerm) ||
                    g.Description.ToLower().Contains(searchTerm) ||
                    g.Developer.ToLower().Contains(searchTerm) ||
                    g.Publisher.ToLower().Contains(searchTerm)
                );
            }

            // Filtrowanie po tagach (wiele tagów naraz - gra musi mieć WSZYSTKIE wybrane tagi)
            if (searchModel.TagIds != null && searchModel.TagIds.Any())
            {
                foreach (var tagId in searchModel.TagIds)
                {
                    query = query.Where(g => g.GameTags.Any(gt => gt.TagId == tagId));
                }
            }

            // Filtrowanie po deweloperze
            if (!string.IsNullOrWhiteSpace(searchModel.Developer))
            {
                query = query.Where(g => g.Developer == searchModel.Developer);
            }

            // Pobierz gry do listy, aby móc użyć metody GetCurrentPrice()
            var games = await query.ToListAsync();

            // Filtrowanie po cenie minimalnej (z uwzględnieniem promocji)
            if (searchModel.MinPrice.HasValue)
            {
                games = games.Where(g => g.GetCurrentPrice() >= searchModel.MinPrice.Value).ToList();
            }

            // Filtrowanie po cenie maksymalnej (z uwzględnieniem promocji)
            if (searchModel.MaxPrice.HasValue)
            {
                games = games.Where(g => g.GetCurrentPrice() <= searchModel.MaxPrice.Value).ToList();
            }

            // Sortowanie po cenie (z uwzględnieniem promocji)
            games = searchModel.SortBy switch
            {
                "price-asc" => games.OrderBy(g => g.GetCurrentPrice()).ToList(),
                "price-desc" => games.OrderByDescending(g => g.GetCurrentPrice()).ToList(),
                "name" => games.OrderBy(g => g.Title).ToList(),
                _ => games // już posortowane przez query (newest)
            };

            // Filtrowanie - tylko z promocjami (przed konwersją do listy)
            if (searchModel.OnlyWithPromotions)
            {
                var now = DateTime.Now;
                query = query.Where(g => g.Promotions.Any(p => 
                    p.IsActive && p.StartDate <= now && p.EndDate >= now
                ));
            }

            // Sortowanie (przed filtrami cenowymi)
            query = searchModel.SortBy switch
            {
                "name" => query.OrderBy(g => g.Title),
                _ => query.OrderByDescending(g => g.CreatedAt) // newest (default)
            };


            // Pobierz dostępne tagi i deweloperów dla filtrów
            ViewBag.Tags = await _context.Tags
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.Developers = await _context.Games
                .Where(g => g.IsActive)
                .Select(g => g.Developer)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            ViewBag.SearchModel = searchModel;

            return View(games);
        }

        // GET: Games/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await _context.Games
                .Include(g => g.Promotions)
                .Include(g => g.GameTags)
                    .ThenInclude(gt => gt.Tag)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        // GET: Games/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
            return View();
        }

        // POST: Games/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int[] selectedTagIds, IFormFile? imageFile, IFormFile? downloadFile)
        {
            // Ręczne pobieranie wartości z formularza bez model bindingu
            var title = Request.Form["Title"].ToString();
            var description = Request.Form["Description"].ToString();
            var developer = Request.Form["Developer"].ToString();
            var publisher = Request.Form["Publisher"].ToString();
            var isActiveStr = Request.Form["IsActive"].ToString();
            
            if (!decimal.TryParse(Request.Form["Price"], out var price))
            {
                ModelState.AddModelError("Price", "Nieprawidłowa cena");
            }
            
            if (!DateTime.TryParse(Request.Form["ReleaseDate"], out var releaseDate))
            {
                ModelState.AddModelError("ReleaseDate", "Nieprawidłowa data wydania");
            }
            
            var isActive = isActiveStr.Contains("true");
            
            // Walidacja
            if (string.IsNullOrWhiteSpace(title))
                ModelState.AddModelError("Title", "Tytuł gry jest wymagany");
            if (string.IsNullOrWhiteSpace(description))
                ModelState.AddModelError("Description", "Opis gry jest wymagany");
            if (string.IsNullOrWhiteSpace(developer))
                ModelState.AddModelError("Developer", "Developer jest wymagany");
            if (string.IsNullOrWhiteSpace(publisher))
                ModelState.AddModelError("Publisher", "Wydawca jest wymagany");
            if (price <= 0)
                ModelState.AddModelError("Price", "Cena musi być większa niż 0");
            
            if (ModelState.IsValid)
            {
                string? imageUrl = null;
                string? downloadUrl = null;
                
                // Upload image file
                if (imageFile != null && imageFile.Length > 0)
                {
                    var imageResult = await _fileUploadService.UploadGameImageAsync(imageFile);
                    if (imageResult.Success)
                    {
                        imageUrl = imageResult.FilePath;
                    }
                    else
                    {
                        ModelState.AddModelError("", imageResult.Message);
                        ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
                        return View(new Game { Title = title, Description = description, Price = price, Developer = developer, Publisher = publisher, ReleaseDate = releaseDate, IsActive = isActive });
                    }
                }
                
                // Upload download file
                if (downloadFile != null && downloadFile.Length > 0)
                {
                    var downloadResult = await _fileUploadService.UploadGameFileAsync(downloadFile);
                    if (downloadResult.Success)
                    {
                        downloadUrl = downloadResult.FilePath;
                    }
                    else
                    {
                        ModelState.AddModelError("", downloadResult.Message);
                        ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
                        return View(new Game { Title = title, Description = description, Price = price, Developer = developer, Publisher = publisher, ReleaseDate = releaseDate, IsActive = isActive });
                    }
                }
                
                // Wstaw grę używając surowego SQL, aby uniknąć problemów z EF Core
                var sql = @"
                    INSERT INTO Games (Title, Description, Price, Developer, Publisher, ReleaseDate, IsActive, CreatedAt, ImageUrl, DownloadUrl)
                    VALUES (@title, @description, @price, @developer, @publisher, @releaseDate, @isActive, @createdAt, @imageUrl, @downloadUrl);
                    SELECT last_insert_rowid();";
                
                var parameters = new[]
                {
                    new Microsoft.Data.Sqlite.SqliteParameter("@title", title),
                    new Microsoft.Data.Sqlite.SqliteParameter("@description", description),
                    new Microsoft.Data.Sqlite.SqliteParameter("@price", price),
                    new Microsoft.Data.Sqlite.SqliteParameter("@developer", developer),
                    new Microsoft.Data.Sqlite.SqliteParameter("@publisher", publisher),
                    new Microsoft.Data.Sqlite.SqliteParameter("@releaseDate", releaseDate),
                    new Microsoft.Data.Sqlite.SqliteParameter("@isActive", isActive),
                    new Microsoft.Data.Sqlite.SqliteParameter("@createdAt", DateTime.UtcNow),
                    new Microsoft.Data.Sqlite.SqliteParameter("@imageUrl", (object?)imageUrl ?? DBNull.Value),
                    new Microsoft.Data.Sqlite.SqliteParameter("@downloadUrl", (object?)downloadUrl ?? DBNull.Value)
                };
                
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddRange(parameters);
                
                await _context.Database.OpenConnectionAsync();
                var result = await command.ExecuteScalarAsync();
                var gameId = Convert.ToInt32(result);

                // Dodaj wybrane tagi
                if (selectedTagIds != null && selectedTagIds.Length > 0)
                {
                    foreach (var tagId in selectedTagIds)
                    {
                        _context.GameTags.Add(new GameTag { GameId = gameId, TagId = tagId });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Gra została dodana pomyślnie!";
                return RedirectToAction(nameof(Index));
            }
            
            ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
            return View(new Game { Title = title, Description = description, Price = price, Developer = developer, Publisher = publisher, ReleaseDate = releaseDate, IsActive = isActive });
        }

        // GET: Games/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await _context.Games
                .Include(g => g.GameTags)
                .FirstOrDefaultAsync(g => g.Id == id);
            if (game == null)
            {
                return NotFound();
            }
            
            ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
            ViewBag.SelectedTagIds = game.GameTags.Select(gt => gt.TagId).ToList();
            return View(game);
        }

        // POST: Games/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Game game, int[] selectedTagIds, IFormFile? imageFile, IFormFile? downloadFile, bool removeDownloadFile = false)
        {
            if (id != game.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Owners");
            ModelState.Remove("Purchases");
            ModelState.Remove("Promotions");
            ModelState.Remove("GameTags");
            ModelState.Remove("Tags");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("DownloadUrl");
            ModelState.Remove("WishlistItems");
            ModelState.Remove("Reviews");

            if (ModelState.IsValid)
            {
                try
                {
                    // Pobierz istniejącą grę z bazy
                    var existingGame = await _context.Games.FindAsync(id);
                    if (existingGame == null)
                    {
                        return NotFound();
                    }

                    // Zaktualizuj właściwości
                    existingGame.Title = game.Title;
                    existingGame.Description = game.Description;
                    existingGame.Price = game.Price;
                    existingGame.Developer = game.Developer;
                    existingGame.Publisher = game.Publisher;
                    existingGame.ReleaseDate = game.ReleaseDate;
                    existingGame.IsActive = game.IsActive;
                    
                    // Obsługa obrazu
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageResult = await _fileUploadService.UploadGameImageAsync(imageFile);
                        if (imageResult.Success)
                        {
                            // Usuń stary obraz jeśli istnieje
                            if (!string.IsNullOrEmpty(existingGame.ImageUrl))
                            {
                                await _fileUploadService.DeleteGameImageAsync(existingGame.ImageUrl);
                            }
                            existingGame.ImageUrl = imageResult.FilePath;
                        }
                        else
                        {
                            ModelState.AddModelError("", imageResult.Message);
                            ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
                            ViewBag.SelectedTagIds = selectedTagIds?.ToList() ?? new List<int>();
                            return View(game);
                        }
                    }
                    
                    // Obsługa pliku do pobrania
                    if (removeDownloadFile)
                    {
                        if (!string.IsNullOrEmpty(existingGame.DownloadUrl))
                        {
                            await _fileUploadService.DeleteGameFileAsync(existingGame.DownloadUrl);
                        }
                        existingGame.DownloadUrl = null;
                    }
                    else if (downloadFile != null && downloadFile.Length > 0)
                    {
                        var downloadResult = await _fileUploadService.UploadGameFileAsync(downloadFile);
                        if (downloadResult.Success)
                        {
                            // Usuń stary plik jeśli istnieje
                            if (!string.IsNullOrEmpty(existingGame.DownloadUrl))
                            {
                                await _fileUploadService.DeleteGameFileAsync(existingGame.DownloadUrl);
                            }
                            existingGame.DownloadUrl = downloadResult.FilePath;
                        }
                        else
                        {
                            ModelState.AddModelError("", downloadResult.Message);
                            ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
                            ViewBag.SelectedTagIds = selectedTagIds?.ToList() ?? new List<int>();
                            return View(game);
                        }
                    }
                    
                    // Aktualizuj tagi - usuń stare i dodaj nowe używając raw SQL
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM GameTags WHERE GameId = {0}", id);
                    
                    if (selectedTagIds != null && selectedTagIds.Length > 0)
                    {
                        foreach (var tagId in selectedTagIds)
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "INSERT INTO GameTags (GameId, TagId) VALUES ({0}, {1})", id, tagId);
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Gra została zaktualizowana!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GameExists(game.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            
            ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
            ViewBag.SelectedTagIds = selectedTagIds?.ToList() ?? new List<int>();
            return View(game);
        }

        // GET: Games/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await _context.Games
                .Include(g => g.GameTags)
                    .ThenInclude(gt => gt.Tag)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        // POST: Games/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game != null)
            {
                _context.Games.Remove(game);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Gra została usunięta!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool GameExists(int id)
        {
            return _context.Games.Any(e => e.Id == id);
        }
    }
}