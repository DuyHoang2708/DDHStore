namespace DDHSTORE.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("PRODUCT_COLOR")]
public class ProductColor
{
    [Key]
    [Column("COLOR_ID")]
    public int ColorId { get; set; }

    [Column("PRODUCT_ID")]
    public int ProductId { get; set; }

    [Column("COLOR_NAME")]
    public string ColorName { get; set; }

    public Product Product { get; set; }
}