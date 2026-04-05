using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DDHSTORE.Models
{
    [Table("USERS")]
    public class User
    {
        [Key]
        [Column("USER_ID")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("USERNAME")]
        public string Username { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("PASSWORD")]
        public string Password { get; set; }

        [MaxLength(100)]
        [Column("EMAIL")]
        public string? Email { get; set; }

        [MaxLength(15)]
        [Column("PHONE")]
        public string? Phone { get; set; }

        [Column("ROLE_ID")]
        public int RoleId { get; set; }

        [Column("STATUS")]
        public int Status { get; set; } = 1; // 1 = active, 0 = inactive

        [NotMapped]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UPDATED_AT")]
        public DateTime? UpdatedAt { get; set; }

        // 🔥 Navigation Properties
        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        // Một user có nhiều đơn hàng
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        // Một user có nhiều địa chỉ
        public ICollection<Address> Addresses { get; set; } = new List<Address>();
    }
}