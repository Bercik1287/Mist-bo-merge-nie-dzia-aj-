using Microsoft.EntityFrameworkCore;
using mist.Data;
using mist.Models;

namespace mist.Services
{
    public class PurchaseService : IPurchaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;

        public PurchaseService(ApplicationDbContext context, ICartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        public async Task<(bool Success, string Message, List<Purchase> Purchases)> ProcessCheckoutAsync(int userId)
        {
            var cart = await _cartService.GetOrCreateCartAsync(userId);
        
            if (!cart.CartItems.Any())
            {
                return (false, "Koszyk jest pusty", new List<Purchase>());
            }
        
            var purchases = new List<Purchase>();
        
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var cartItem in cart.CartItems)
                {
                    // Sprawdź czy użytkownik już nie posiada tej gry
                    if (await _cartService.HasUserPurchasedGameAsync(userId, cartItem.GameId))
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Już posiadasz grę: {cartItem.Game.Title}", null);
                    }
        
                    // Sprawdź czy gra jest nadal dostępna
                    var game = await _context.Games.FindAsync(cartItem.GameId);
                    if (game == null || !game.IsActive)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Gra {cartItem.Game.Title} nie jest już dostępna", null);
                    }
        
                    var purchase = new Purchase
                    {
                        UserId = userId,
                        GameId = cartItem.GameId,
                        PricePaid = cartItem.Price,
                        PurchaseDate = DateTime.Now
                    };
        
                    _context.Purchases.Add(purchase);
                    purchases.Add(purchase);
                }
        
                // Wyczyść koszyk
                _context.CartItems.RemoveRange(cart.CartItems);
        
                // Usuń z wishlisty wszystkie gry, które zostały właśnie kupione
                var wishlistItemsToRemove = await _context.WishlistItems
                    .Where(w => w.UserId == userId && 
                           purchases.Select(p => p.GameId).Contains(w.GameId))
                    .ToListAsync();
        
                _context.WishlistItems.RemoveRange(wishlistItemsToRemove);
        
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
        
                return (true, $"Zakupiono pomyślnie {purchases.Count} gier!", purchases);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Błąd podczas przetwarzania zakupu: {ex.Message}", null);
            }
        }
        public async Task<List<Purchase>> GetUserPurchasesAsync(int userId)
        {
            return await _context.Purchases
                .Include(p => p.Game)
                    .ThenInclude(g => g.GameTags)
                        .ThenInclude(gt => gt.Tag)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();
        }

        public async Task<List<Game>> GetUserOwnedGamesAsync(int userId)
        {
            return await _context.Purchases
                .Include(p => p.Game)
                    .ThenInclude(g => g.GameTags)
                        .ThenInclude(gt => gt.Tag)
                .Where(p => p.UserId == userId)
                .Select(p => p.Game)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }
    }
}