using System.Collections.Concurrent;

namespace Jube.Cache.Kvp;

public class ValueDictionary
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Value { get; set; } = new();
}