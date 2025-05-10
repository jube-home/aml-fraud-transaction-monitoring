using Jube.Cache.Models;

namespace Jube.Cache.Kvp;

public class HashSetDictionary
{
    private Dictionary<string, SortedSet<SortedSetEntry>> kvpSortedSet;
}