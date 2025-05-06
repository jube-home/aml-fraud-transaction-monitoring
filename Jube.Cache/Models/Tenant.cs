using System.Collections.Concurrent;
using Jube.Cache.Models.EntityAnalysisModel.SearchKey;

namespace Jube.Cache.Models.Abstraction;

public class Tenant
{
    public int TenantId { get; set; }
    public ConcurrentDictionary<int, SearchKey> EntityAnalysisModels { get; set; } = new();
}