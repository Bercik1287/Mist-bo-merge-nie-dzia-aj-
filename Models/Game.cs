using System.ComponentModel.DataAnnotations;

namespace mist.Models
{
    public class Game
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tytuł gry jest wymagany")]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(2000)]
        public string Description { get; set; }

        [Required]
        [Range(0.01, 999.99, ErrorMessage = "Cena musi być między 0.01 a 999.99")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; }
        public string? DownloadUrl { get; set; }
        public string Developer { get; set; }
        public string Publisher { get; set; }
        public DateTime ReleaseDate { get; set; }
        public bool IsActive { get; set; } = true;
        
        [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; }

        // Relacje
        public virtual ICollection<User> Owners { get; set; }
        public virtual ICollection<Purchase> Purchases { get; set; }
        public virtual ICollection<Promotion> Promotions { get; set; }
        public virtual ICollection<WishlistItem> WishlistItems { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        
        // Relacja wiele-do-wielu z Tag
        public virtual ICollection<GameTag> GameTags { get; set; } = new List<GameTag>();

        // Pomocnicza właściwość do pobierania tagów
        public IEnumerable<Tag> Tags => GameTags?.Select(gt => gt.Tag) ?? Enumerable.Empty<Tag>();

        // Pomocnicza właściwość do wyświetlania tagów jako string
        public string TagsDisplay => GameTags?.Any() == true 
            ? string.Join(", ", GameTags.Select(gt => gt.Tag?.Name).Where(n => n != null)) 
            : "Brak tagów";

        // Obliczenia - cena z uwzględnieniem aktywnej promocji
        public decimal GetCurrentPrice()
        {
            var activePromotion = Promotions?
                .FirstOrDefault(p => p.IsValidNow());

            if (activePromotion != null)
            {
                return activePromotion.CalculateDiscountedPrice(Price);
            }

            return Price;
        }

        public Promotion GetActivePromotion()
        {
            return Promotions?.FirstOrDefault(p => p.IsValidNow());
        }

        public bool HasActivePromotion()
        {
            return GetActivePromotion() != null;
        }

        public decimal GetAverageRating()
            {
                if (Reviews == null || !Reviews.Any())
                    return 0;
                return (decimal)Reviews.Average(r => r.Rating);
            }

        public int GetReviewCount()
            {
                return Reviews?.Count ?? 0;
            }
    }
}