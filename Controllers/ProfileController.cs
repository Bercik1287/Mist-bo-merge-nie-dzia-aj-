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
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public ProfileController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // GET: Profile/Edit (dla użytkownika)
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsAdminEdit = false
            };

            return View(model);
        }

        // POST: Profile/Edit (dla użytkownika)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            var userId = GetCurrentUserId();

            if (model.Id != userId)
            {
                return Forbid();
            }

            // Usuń walidację dla pól, które nie są wymagane w tym kontekście
            ModelState.Remove("IsAdminEdit");
            if (string.IsNullOrEmpty(model.NewPassword))
            {
                ModelState.Remove("NewPassword");
                ModelState.Remove("ConfirmNewPassword");
            }

            // Jeśli użytkownik chce zmienić hasło, wymagane jest obecne hasło
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Obecne hasło jest wymagane do zmiany hasła");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Jeśli zmienia się hasło, sprawdź obecne hasło
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (!_authService.VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Nieprawidłowe obecne hasło");
                    return View(model);
                }
            }

            // Sprawdź czy nowa nazwa użytkownika jest już zajęta
            if (model.Username != user.Username)
            {
                if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.Id != userId))
                {
                    ModelState.AddModelError("Username", "Ta nazwa użytkownika jest już zajęta");
                    return View(model);
                }
            }

            // Sprawdź czy nowy email jest już zajęty
            if (model.Email != user.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId))
                {
                    ModelState.AddModelError("Email", "Ten adres email jest już zarejestrowany");
                    return View(model);
                }
            }

            // Zaktualizuj dane użytkownika
            user.Username = model.Username;
            user.Email = model.Email;

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                user.PasswordHash = _authService.HashPassword(model.NewPassword);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profil został zaktualizowany pomyślnie";
            return RedirectToAction(nameof(Edit));
        }

        // GET: Profile/Details (podgląd własnego profilu)
        [HttpGet]
        public async Task<IActionResult> Details()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users
                .Include(u => u.Purchases)
                    .ThenInclude(p => p.Game)
                        .ThenInclude(g => g.GameTags)
                            .ThenInclude(gt => gt.Tag)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }
    }
}