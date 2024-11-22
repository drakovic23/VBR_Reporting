using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace reports_be.Models;

public class BackedUpHost
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int VbrId { get; set; }
    [MaxLength(64)]
    public string? BHostName { get; set; }
    public VbrHost? VbrHost { get; set; }
    public ICollection<RestorePoint>? RestorePoint { get; set; }
}