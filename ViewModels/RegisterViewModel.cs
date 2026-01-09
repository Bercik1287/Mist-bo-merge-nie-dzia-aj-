using System.ComponentModel.DataAnnotations;

namespace mist.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Nazwa użytkownika jest wymagana")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Nazwa użytkownika musi mieć 3-20 znaków")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tylko litery, cyfry i podkreślnik")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Hasło musi mieć min. 8 znaków")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$", 
            ErrorMessage = "Hasło musi zawierać wielką literę, małą literę, cyfrę i znak specjalny")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane")]
        [Compare("Password", ErrorMessage = "Hasła nie są identyczne")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}