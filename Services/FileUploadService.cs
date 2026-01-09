using Microsoft.AspNetCore.Http;

namespace mist.Services
{
    public interface IFileUploadService
    {
        Task<(bool Success, string Message, string FilePath)> UploadGameImageAsync(IFormFile file);
        Task<bool> DeleteGameImageAsync(string filePath);
        Task<(bool Success, string Message, string FilePath)> UploadGameFileAsync(IFormFile file);
        Task<bool> DeleteGameFileAsync(string filePath);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly string[] _allowedGameExtensions = { ".zip", ".rar", ".7z", ".exe", ".msi" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private const long MaxGameFileSize = 500 * 1024 * 1024; // 500MB

        public FileUploadService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<(bool Success, string Message, string FilePath)> UploadGameImageAsync(IFormFile file)
        {
            try
            {
                // Walidacja - plik nie może być null
                if (file == null || file.Length == 0)
                {
                    return (false, "Nie wybrano pliku", null);
                }

                // Walidacja - rozmiar pliku
                if (file.Length > MaxFileSize)
                {
                    return (false, $"Plik jest za duży. Maksymalny rozmiar to {MaxFileSize / 1024 / 1024}MB", null);
                }

                // Walidacja - rozszerzenie pliku
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                {
                    return (false, $"Niedozwolony format pliku. Dozwolone formaty: {string.Join(", ", _allowedExtensions)}", null);
                }

                // Generuj unikalną nazwę pliku
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                
                // Ścieżka do folderu uploads
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "games");
                
                // Utwórz folder jeśli nie istnieje
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Pełna ścieżka do pliku
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Zapisz plik
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Zwróć relatywną ścieżkę do użycia w bazie danych
                var relativePath = $"/images/games/{uniqueFileName}";
                
                return (true, "Plik został przesłany pomyślnie", relativePath);
            }
            catch (Exception ex)
            {
                return (false, $"Błąd podczas przesyłania pliku: {ex.Message}", null);
            }
        }

        public async Task<bool> DeleteGameImageAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return true;
                }

                // Usuń początkowy "/" jeśli istnieje
                filePath = filePath.TrimStart('/');
                
                var fullPath = Path.Combine(_environment.WebRootPath, filePath);

                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    return true;
                }

                return true; // Plik nie istnieje, więc uznajemy że został "usunięty"
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Success, string Message, string FilePath)> UploadGameFileAsync(IFormFile file)
        {
            try
            {
                // Walidacja - plik nie może być null
                if (file == null || file.Length == 0)
                {
                    return (false, "Nie wybrano pliku", null);
                }

                // Walidacja - rozmiar pliku
                if (file.Length > MaxGameFileSize)
                {
                    return (false, $"Plik jest za duży. Maksymalny rozmiar to {MaxGameFileSize / 1024 / 1024}MB", null);
                }

                // Walidacja - rozszerzenie pliku
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedGameExtensions.Contains(extension))
                {
                    return (false, $"Niedozwolony format pliku. Dozwolone formaty: {string.Join(", ", _allowedGameExtensions)}", null);
                }

                // Generuj unikalną nazwę pliku
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                
                // Ścieżka do folderu downloads
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "downloads", "games");
                
                // Utwórz folder jeśli nie istnieje
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Pełna ścieżka do pliku
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Zapisz plik
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Zwróć relatywną ścieżkę do użycia w bazie danych
                var relativePath = $"/downloads/games/{uniqueFileName}";
                
                return (true, "Plik został przesłany pomyślnie", relativePath);
            }
            catch (Exception ex)
            {
                return (false, $"Błąd podczas przesyłania pliku: {ex.Message}", null);
            }
        }

        public async Task<bool> DeleteGameFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return true;
                }

                // Usuń początkowy "/" jeśli istnieje
                filePath = filePath.TrimStart('/');
                
                var fullPath = Path.Combine(_environment.WebRootPath, filePath);

                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    return true;
                }

                return true; // Plik nie istnieje, więc uznajemy że został "usunięty"
            }
            catch
            {
                return false;
            }
        }
    }
}