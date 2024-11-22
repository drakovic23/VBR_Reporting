namespace reports_be.Models;

public class BackupStatus // Represents the view of the latest backups for each BHost
{
    public string VbrHostName { get; set; }
    public string BHostName { get; set; }
    public string LatestDate { get; set; }
    public string ParentJob { get; set; }
    public string Status { get; set; }
}