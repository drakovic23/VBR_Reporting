using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace reports_be.Models;

public class VbrHost //Represents a VBR Server
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [MaxLength(64)]
    public string? VbrHostName { get; set; }
    
    public ICollection<BackedUpHost>? BackedUpHosts { get; set; } // Navigation property
    public ICollection<RestorePoint>? RestorePoints { get; set; } // Navigation property
}