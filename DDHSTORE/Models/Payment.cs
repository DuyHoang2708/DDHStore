namespace DDHSTORE.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("PAYMENT")]
public class Payment
{
    [Key]
    [Column("PAYMENT_ID")]
    public int PaymentId { get; set; }

    [Column("ORDER_ID")]
    public int OrderId { get; set; }

    [Column("METHOD")]
    public string Method { get; set; }

    [Column("PAYMENT_DATE")]
    public DateTime PaymentDate { get; set; }

    [Column("STATUS")]
    public string Status { get; set; }

    public Order Order { get; set; }
}