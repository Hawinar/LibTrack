using System.ComponentModel.DataAnnotations;

namespace LibTrack.Models
{
    public sealed class RegisterRequest
    {
        [Required(ErrorMessage = "Введите email.")]
        [EmailAddress(ErrorMessage = "Введите корректный email.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите пароль.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Повторите пароль.")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают.")]
        [DataType(DataType.Password)]
        public string RepeatPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите ФИО.")]
        [StringLength(50, ErrorMessage = "ФИО не должно превышать 50 символов.")]
        public string FullName { get; set; } = string.Empty;
    }
}
