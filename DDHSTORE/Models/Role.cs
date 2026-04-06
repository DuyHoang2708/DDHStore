namespace DDHSTORE.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("ROLE")]
public class Role
{
    [Key]
    [Column("ROLE_ID")]
    public int RoleId { get; set; }

    [Column("ROLE_NAME")]
    public string RoleName { get; set; }

    public ICollection<User> Users { get; set; }
}