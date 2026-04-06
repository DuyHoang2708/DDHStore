using DDHSTORE.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DDHSTORE.Models
{
    [Table("CATEGORY")]
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("CATEGORY_ID")]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("CATEGORY_NAME")]
        public string CategoryName { get; set; }

        [Column("STATUS")]
        public int Status { get; set; } = 1; // 1 = hiện, 0 = ẩn

        // 🔥 Navigation
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}