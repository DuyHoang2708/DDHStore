using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DDHSTORE.Models
{
    [Table("FAVORITES")]
    public class Favorite
    {
        [Key]
        [Column("FAVORITE_ID")]
        public int FavoriteId { get; set; }

        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        [Required]
        [Column("PRODUCT_ID")]
        public int ProductId { get; set; }

        [Column("CREATED_AT")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now; // 🔥 auto time

        // ================= NAVIGATION =================
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}