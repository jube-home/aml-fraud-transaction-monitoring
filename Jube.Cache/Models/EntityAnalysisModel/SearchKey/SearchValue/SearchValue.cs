using System.Collections.Concurrent;

namespace Jube.Cache.Models.EntityAnalysisModel.SearchKey.SearchValue;

public class SearchValue
{
    public DateTime Timestamp;
    public ConcurrentDictionary<string, object> Payload { get; set; } = new();
}