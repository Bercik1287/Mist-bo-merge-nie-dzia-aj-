using Microsoft.EntityFrameworkCore;
using mist.Data;
using mist.Models;

namespace mist.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;

        public WishlistService(ApplicationDbContext context, ICartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        public async Task<(bool Success, string Message)> AddToWishlistAsync(int userId, int gameId)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null || !game.IsActive)
            {
                return (false, "Gra nie została znaleziona lub nie jest dostępna");
            }

            // Sprawdź czy użytkownik już posiada grę
            if (await _cartService.HasUserPurchasedGameAsync(userId, gameId))
            {
                return (false, "Już posiadasz tę grę");
            }

            // Sprawdź czy już jest w wishliście
            if (await IsInWishlistAsync(userId, gameId))
            {
                return (false, "Gra jest już na wishliście");
            }

            var wishlistItem = new WishlistItem
            {
                UserId = userId,
                GameId = gameId
            };

            _context.WishlistItems.Add(wishlistItem);
            await _context.SaveChangesAsync();

            return (true, "Dodano do wishlisty");
        }

        public async Task<(bool Success, string Message)> RemoveFromWishlistAsync(int userId, int gameId)
        {
            var wishlistItem = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.GameId == gameId);

            if (wishlistItem == null)
            {
                return (false, "Gra nie jest na wishliście");
            }

            _context.WishlistItems.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            return (true, "Usunięto z wishlisty");
        }

        public async Task<List<WishlistItem>> GetUserWishlistAsync(int userId)
        {
            return await _context.WishlistItems
                .Include(w => w.Game)
                    .ThenInclude(g => g.GameTags)
                        .ThenInclude(gt => gt.Tag)
                .Include(w => w.Game)
                    .ThenInclude(g => g.Promotions)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();
        }

        public async Task<bool> IsInWishlistAsync(int userId, int gameId)
        {
            return await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.GameId == gameId);
        }

        public async Task<(bool Success, string Message)> RemoveFromWishlistIfOwnedAsync(int userId, int gameId)
        {
            if (await _cartService.HasUserPurchasedGameAsync(userId, gameId))
            {
                return await RemoveFromWishlistAsync(userId, gameId);
            }

            return (true, "Gra nie była na wishliście");
        }
    }
}