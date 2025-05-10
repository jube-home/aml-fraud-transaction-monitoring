namespace Jube.Cache.Models;

public class SortedSetEntry
{
    public DateTime Timestamp { get; set; }
    public Guid Guid { get; set; }
    public DateTime Score { get; set; } 
    public string Element { get; set; }
    public Dictionary<string, object> Payload { get; set; } = new();
}