using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

[Keyless]
[Table("V_REVENUE")]
public class Revenue
{
    public DateTime ReportDate { get; set; }
    public decimal TotalRevenue { get; set; }
}