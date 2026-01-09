using Microsoft.EntityFrameworkCore;
using mist.Models;

namespace mist.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<GameTag> GameTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Purchase relationships
            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.User)
                .WithMany(u => u.Purchases)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.Game)
                .WithMany(g => g.Purchases)
                .HasForeignKey(p => p.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cart relationships
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithOne()
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Game)
                .WithMany()
                .HasForeignKey(ci => ci.GameId)
                .OnDelete(DeleteBehavior.Restrict);

            // Promotion relationships
            modelBuilder.Entity<Promotion>()
                .HasOne(p => p.Game)
                .WithMany(g => g.Promotions)
                .HasForeignKey(p => p.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            // Wishlist relationships
            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.User)
                .WithMany(u => u.WishlistItems)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.Game)
                .WithMany(g => g.WishlistItems)
                .HasForeignKey(w => w.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unikalne: jeden użytkownik, jedna gra w wishliście
            modelBuilder.Entity<WishlistItem>()
                .HasIndex(w => new { w.UserId, w.GameId })
                .IsUnique();

            // Review relationships
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Game)
                .WithMany(g => g.Reviews)
                .HasForeignKey(r => r.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unikalne: jeden użytkownik, jedna recenzja na grę
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.UserId, r.GameId })
                .IsUnique();

            // Tag - unique name
            modelBuilder.Entity<Tag>()
                .HasIndex(t => t.Name)
                .IsUnique();

            // GameTag - klucz złożony dla relacji wiele-do-wielu
            modelBuilder.Entity<GameTag>()
                .HasKey(gt => new { gt.GameId, gt.TagId });

            modelBuilder.Entity<GameTag>()
                .HasOne(gt => gt.Game)
                .WithMany(g => g.GameTags)
                .HasForeignKey(gt => gt.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GameTag>()
                .HasOne(gt => gt.Tag)
                .WithMany(t => t.GameTags)
                .HasForeignKey(gt => gt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed data - Tags
            modelBuilder.Entity<Tag>().HasData(
                new Tag { Id = 1, Name = "RPG", Description = "Gry fabularne" },
                new Tag { Id = 2, Name = "Action", Description = "Gry akcji" },
                new Tag { Id = 3, Name = "Adventure", Description = "Gry przygodowe" },
                new Tag { Id = 4, Name = "Metroidvania", Description = "Gry w stylu Metroidvania" },
                new Tag { Id = 5, Name = "Open World", Description = "Gry z otwartym światem" },
                new Tag { Id = 6, Name = "Indie", Description = "Gry niezależnych twórców" }
            );

            // Seed data - Games
            modelBuilder.Entity<Game>().HasData(
                new Game
                {
                    Id = 1,
                    Title = "Cyberpunk 2137",
                    Description = "Futurystyczna gra RPG w otwartym świecie Day City",
                    Price = 199.99m,
                    Developer = "VHS Projekt Green",
                    Publisher = "VHS Projekt",
                    ReleaseDate = new DateTime(2020, 12, 10),
                    ImageUrl = "/images/cyberpunk.jpg",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 10, 31, 23, 6, 0, 300, DateTimeKind.Utc)
                },
                new Game
                {
                    Id = 2,
                    Title = "Wichur 3 Dziki Zgon",
                    Description = "Epicka przygoda Gerwazego z Rumunii",
                    Price = 129.99m,
                    Developer = "VHS Projekt Green",
                    Publisher = "VHS Projekt",
                    ReleaseDate = new DateTime(2015, 5, 19),
                    ImageUrl = "/images/witcher3.jpg",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 10, 31, 23, 6, 0, 306, DateTimeKind.Utc)
                },
                new Game
                {
                    Id = 3,
                    Title = "Holluw Knuht",
                    Description = "Zejdź w głąb królestwa Hallownest",
                    Price = 19.99m,
                    Developer = "Team Mirabelle",
                    Publisher = "Team Mirabelle",
                    ReleaseDate = new DateTime(2017, 2, 24),
                    ImageUrl = "/images/hollowknight.jpg",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 10, 31, 23, 6, 0, 312, DateTimeKind.Utc)
                }
           );

            // Seed data - GameTags (przypisanie tagów do gier)
            modelBuilder.Entity<GameTag>().HasData(
                // Cyberpunk 2137 - RPG, Action, Open World
                new GameTag { GameId = 1, TagId = 1 },
                new GameTag { GameId = 1, TagId = 2 },
                new GameTag { GameId = 1, TagId = 5 },
                // Wichur 3 - RPG, Adventure, Open World
                new GameTag { GameId = 2, TagId = 1 },
                new GameTag { GameId = 2, TagId = 3 },
                new GameTag { GameId = 2, TagId = 5 },
                // Hollow Knight - Metroidvania, Action, Indie
                new GameTag { GameId = 3, TagId = 4 },
                new GameTag { GameId = 3, TagId = 2 },
                new GameTag { GameId = 3, TagId = 6 }
            );
        }

        public override int SaveChanges()
        {
            try
            {
                var entries = ChangeTracker.Entries<Game>()
                    .Where(e => e.State == EntityState.Added);

                foreach (var entry in entries)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
            }
            catch
            {
                // Ignoruj błędy przy przetwarzaniu ChangeTracker - może wystąpić gdy nawigacje są null
            }

            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entries = ChangeTracker.Entries<Game>()
                    .Where(e => e.State == EntityState.Added);

                foreach (var entry in entries)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
            }
            catch
            {
                // Ignoruj błędy przy przetwarzaniu ChangeTracker - może wystąpić gdy nawigacje są null
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}