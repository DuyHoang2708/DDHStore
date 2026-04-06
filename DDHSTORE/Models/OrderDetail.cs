namespace DDHSTORE.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("ORDER_DETAIL")]
public class OrderDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ORDER_DETAIL_ID")]
    public int OrderDetailId { get; set; }

    [Column("ORDER_ID")]
    public int OrderId { get; set; }

    [Column("PRODUCT_ID")]
    public int ProductId { get; set; }

    [Column("QUANTITY")]
    public int Quantity { get; set; }

    [Column("PRICE")]
    public decimal Price { get; set; }

    [NotMapped]
    public decimal Subtotal => Price * Quantity;
    public Order Order { get; set; }
    public Product Product { get; set; }
}