namespace DDHSTORE.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("ORDERS")]
public class Order
{
    [Key]
    [Column("ORDER_ID")]
    public int OrderId { get; set; }

    [Column("USER_ID")]
    public int UserId { get; set; }

    [Column("ORDER_DATE")]
    public DateTime OrderDate { get; set; }

    [Column("TOTAL_AMOUNT")]
    public decimal TotalAmount { get; set; }

    [Column("STATUS")]
    public string Status { get; set; }

    [Column("ADDRESS_ID")]
    public int AddressId { get; set; }

    public User User { get; set; }
    public Address Address { get; set; }
    public ICollection<OrderDetail> OrderDetails { get; set; }

    public Payment Payment { get; set; }
}