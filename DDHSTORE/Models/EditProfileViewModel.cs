using System.ComponentModel.DataAnnotations;

namespace DDHSTORE.Models
{
    public class EditProfileViewModel
    {
        public string? Username { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string? Email { get; set; }

        [RegularExpression(@"^\+?\d{9,15}$", ErrorMessage = "Số điện thoại không hợp lệ (chỉ chứa số, 9-15 ký tự, có thể có dấu +).")]
        public string? Phone { get; set; }

        // Default/first address (simple UX)
        [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
        public string RecipientName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Tỉnh/Thành phố.")]
        public string Province { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Quận/Huyện.")]
        public string District { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Phường/Xã.")]
        public string Ward { get; set; }

        public string? Detail { get; set; }
    }
}

