using System.ComponentModel.DataAnnotations;

namespace DDHSTORE.Models
{
    public class RegisterViewModel
    {
        // User
        [Required]
        public string Username { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[!@#$%^&*(),.?""':{}|<>]).+$",
            ErrorMessage = "Mật khẩu phải có ít nhất 1 chữ in hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt (!@#$%^&*(),.?\"':{{}}|<>).")]
        public string Password { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string? Email { get; set; }

        public string? Phone { get; set; }

        // Address
        [Required]
        public string RecipientName { get; set; }

        [Required]
        public string Province { get; set; }

        [Required]
        public string District { get; set; }

        [Required]
        public string Ward { get; set; }

        public string? Detail { get; set; }
    }
}