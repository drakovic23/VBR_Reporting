namespace reports_be.Models;

public class RestorePointDto //Used as a data transfer object
{
    public string VbrHost { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    //public long ApproximateSize { get; set; }
    public string? ParentJob { get; set; }
}