using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace reports_be.Models;

public class RestorePoint : IEquatable<RestorePoint>
{
    public int VbrId { get; set; }
    public int BHostId { get; set; }
    public DateOnly Date { get; set; }
    [MaxLength(64)]
    public string? ParentJob { get; set; }
    //public long ApproximateSize { get; set; }
    public VbrHost? VbrHost { get; set; } // Navigation property
    public BackedUpHost? BackedUpHost { get; set; }

    public bool Equals(RestorePoint? other)
    {
        if (other is null)
            return false;
        
        return BHostId == other.BHostId && Date == other.Date && VbrId == other.VbrId && ParentJob == other.ParentJob;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(BHostId, Date, VbrId, ParentJob);
    }
}