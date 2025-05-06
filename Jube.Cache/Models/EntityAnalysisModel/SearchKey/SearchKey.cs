using System.Collections.Concurrent;

namespace Jube.Cache.Models.EntityAnalysisModel.SearchKey;

public class SearchKey
{
    public ConcurrentDictionary<string, SearchValue.SearchValue> SearchValues { get; set; } = new();
}