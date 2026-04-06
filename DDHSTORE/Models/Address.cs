using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DDHSTORE.Models
{
    [Table("ADDRESS")]
    public class Address
    {
        [Key]

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        [Column("ADDRESS_ID")]
        public int AddressId { get; set; }

        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("RECIPIENT_NAME")]
        public string RecipientName { get; set; }

        [Required]
        [MaxLength(15)]
        [Column("PHONE")]
        public string Phone { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("PROVINCE")]
        public string Province { get; set; }  // từ API

        [Required]
        [MaxLength(100)]
        [Column("DISTRICT")]
        public string District { get; set; }  // từ API

        [Required]
        [MaxLength(100)]
        [Column("WARD")]
        public string Ward { get; set; }      // từ API

        [MaxLength(255)]
        [Column("DETAIL")]
        public string? Detail { get; set; }   // số nhà, đường

        [Column("STATUS")]
        public int Status { get; set; } = 1;  // active/inactive

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 🔥 Navigation
        [ForeignKey("UserId")]
        public User User { get; set; }

        // Nếu muốn 1 User có nhiều địa chỉ
        // public ICollection<User> Users { get; set; }
    }
}