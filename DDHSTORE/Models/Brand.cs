using DDHSTORE.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DDHSTORE.Models
{
    [Table("BRAND")]
    public class Brand
    {
        [Key]
        [Column("BRAND_ID")]
        public int BrandId { get; set; }

        [Column("BRAND_NAME")]
        public string BrandName { get; set; }

        [Column("STATUS")]
        public int Status { get; set; }

        // Navigation property
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}