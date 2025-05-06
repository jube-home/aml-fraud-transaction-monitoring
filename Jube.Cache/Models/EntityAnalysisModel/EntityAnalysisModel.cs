using System.Collections.Concurrent;

namespace Jube.Cache.Models.EntityAnalysisModel;

public class EntityAnalysisModel
{
    public int EntityAnalysisModelId { get; set; }
    public ConcurrentDictionary<string,SearchKey.SearchKey> SearchKeys { get; set; } = new();
}