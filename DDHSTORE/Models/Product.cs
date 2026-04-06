using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DDHSTORE.Models
{
    [Table("PRODUCT")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("PRODUCT_ID")]
        public int ProductId { get; set; }

        [Required]
        [Column("PRODUCT_NAME")]
        public string ProductName { get; set; }

        [Required]
        [Column("PRICE")]
        public decimal Price { get; set; }

        [Column("DESCRIPTION")]
        public string? Description { get; set; }

        [Required]
        [Column("CATEGORY_ID")]
        public int CategoryId { get; set; }

        [Required]
        [Column("BRAND_ID")]
        public int BrandId { get; set; }

        [Column("STATUS")]
        public int Status { get; set; }

        [Column("IMAGE_URL")]
        public string? ImageUrl { get; set; }

        // 🔥 THÔNG SỐ KỸ THUẬT (trước là ProductSpec)
        [Column("CPU")]
        public string? CPU { get; set; }

        [Column("RAM")]
        public string? RAM { get; set; }

        [Column("STORAGE")]
        public string? Storage { get; set; }

        [Column("SCREEN")]
        public string? Screen { get; set; }

        [Column("OS")]
        public string? OS { get; set; }

        [Column("BATTERY")]
        public string? Battery { get; set; }

        // 🔥 TỒN KHO (trước là Inventory)
        [Column("QUANTITY")]
        public int Quantity { get; set; }

        [Column("LAST_UPDATE")]
        public DateTime LastUpdate { get; set; } = DateTime.Now;

        // 🔥 NAVIGATION PROPERTIES
        public Category? Category { get; set; }
        public Brand? Brand { get; set; }

        public ICollection<ProductColor> Colors { get; set; } = new List<ProductColor>();
    }
}