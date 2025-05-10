using System.Collections.Concurrent;
using Jube.Cache.Kvp;
using Jube.Cache.Models;
using DateTime = System.DateTime;

namespace Jube.Cache;

public class Cache
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> keyStringValueDictionaryInt;
    
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> keyStringValueDictionaryObject;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ValueDictionary>>
        keyStringValueDictionaryValueDictionary;

    private readonly ConcurrentDictionary<string, SortedSet<SortedSetEntry>> keyStringValueSortedSetSortedSetEntryByDatetime;
    
    private readonly ConcurrentDictionary<string, DateTime> keyStringValueSortedSetDateTimeByDatetime;

    public Cache()
    {
        keyStringValueDictionaryValueDictionary =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, ValueDictionary>>();
        keyStringValueSortedSetSortedSetEntryByDatetime = [];
        keyStringValueSortedSetDateTimeByDatetime = [];
        keyStringValueDictionaryInt = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();
        keyStringValueDictionaryObject = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();
    }

    public async Task HashDeleteAsync(string key, string hashKey)
    {
        throw new NotImplementedException();
    }

    public async Task<int?> HashGetAsync(string key, string hashKey)
    {
        throw new NotImplementedException();
    }

    public async Task<DateTime?> HashGetDateTimeAsync(string key, string hashKey)
    {
        throw new NotImplementedException();
    }

    public async Task<int?> HashGetIntAsync(string key, string hashKey)
    {
        throw new NotImplementedException();
    }

    public async Task<List<string>> HashKeysAsync(string key)
    {
        throw new NotImplementedException();
    }

    public async Task HashDeleteAsync(string key, string[] hashKey)
    {
        throw new NotImplementedException();
    }

    public async Task HashSetAsync(string key, string hashKey, string value)
    {
        throw new NotImplementedException();
    }

    public async Task HashSetAsync(string key, string hashKey, DateTime value)
    {
        throw new NotImplementedException();
    }

    public async Task HashSetAsync(string key, string hashKey, object value)
    {
        keyStringValueDictionaryObject.TryGetValue(key, out var valueDictionaryObject);
        if (valueDictionaryObject == null)
        {
            valueDictionaryObject = new ConcurrentDictionary<string, object>();
            keyStringValueDictionaryObject.TryAdd(key, valueDictionaryObject);
        }

        valueDictionaryObject.TryAdd(hashKey,value);
    }

    public async Task HashSetAsync(string key, string hashKey, Dictionary<string, object> value)
    {
        keyStringValueDictionaryValueDictionary.TryGetValue(key, out var valueDictionary);
        if (valueDictionary == null)
        {
            valueDictionary = new ConcurrentDictionary<string, ValueDictionary>();
            keyStringValueDictionaryValueDictionary.TryAdd(key, valueDictionary);
        }

        valueDictionary.TryAdd(hashKey, new ValueDictionary
        {
            Timestamp = DateTime.Now,
            Value = value
        });
    }

    public async Task<double?> HashGetDoubleAsync(string key, string hashKey)
    {
        throw new NotImplementedException();
    }

    public async Task SortedSetUpdateAsync(string key, string hashKey, DateTime value)
    {
        throw new NotImplementedException();
    }

    public async Task SortedSetAddAsync(string key, string hashKey, DateTime value)
    {
        Console.WriteLine(key);
    }

    public async Task SortedSetAddAsync(string key, Dictionary<string, object> value, DateTime dateTime)
    {
        keyStringValueSortedSetSortedSetEntryByDatetime.TryGetValue(key, out var sortedSet);
        sortedSet ??= new SortedSet<SortedSetEntry>(new SortedSetEntryComparer());

        lock (sortedSet)
        {
            sortedSet.Add(new SortedSetEntry
            {
                Timestamp = DateTime.Now,
                Score = dateTime,
                Payload = value
            });
        }

        keyStringValueSortedSetSortedSetEntryByDatetime.TryAdd(key, sortedSet);
    }

    public async Task SortedSetRemoveAsync(string key, string hashKey)
    {
        throw new NotImplementedException();
    }

    public async Task SortedSetRemoveAsync(string key, string[] hashKeys)
    {
        throw new NotImplementedException();
    }

    public async Task HashIncrementAsync(string key, string hashKey, int increment = 1)
    {
        keyStringValueDictionaryInt.TryGetValue(key, out var dictionary);
        dictionary ??= new ConcurrentDictionary<string, int>();
        var hasValue = dictionary.TryGetValue(hashKey, out var value);
        if (!hasValue)
        {
            dictionary.TryAdd(hashKey, increment);
            return;
        }

        Interlocked.Add(ref value, increment);
    }

    public async Task HashDecrementAsync(string key, string hashKey, int value = 1)
    {
        throw new NotImplementedException();
    }

    public async Task HashDecrementAsync(string key, int hashKey, int value = 1)
    {
        throw new NotImplementedException();
    }

    public async Task HashIncrementAsync(string key, int hashKey, int value = 1)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> HashExistsAsync(string key, string hashKey)
    {
        keyStringValueDictionaryValueDictionary.TryGetValue(key, out var sortedSetByDatetime);
        return sortedSetByDatetime != null && sortedSetByDatetime.ContainsKey(hashKey);
    }

    public async Task<SortedSetEntry?> SortedSetScoreAsync(string key, string hashKey)
    {
        keyStringValueSortedSetSortedSetEntryByDatetime.TryGetValue(key, out var sortedSet);
        return sortedSet?.FirstOrDefault(sortedSetEntry => sortedSetEntry.Element == hashKey);
    }

    public async Task<List<string>> HashGetAllAsync(string key)
    {
        throw new NotImplementedException();
    }

    public async Task<SortedSet<SortedSetEntry>> GetSortedSetByKey(string key, int from, int limit, bool desc = false)
    {
        keyStringValueSortedSetSortedSetEntryByDatetime.TryGetValue(key, out var sortedSetValue);
        return sortedSetValue ?? [];
    }
}

public class SortedSetEntryComparer : IComparer<SortedSetEntry>
{
    public int Compare(SortedSetEntry? x, SortedSetEntry? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (y is null) return 1;
        if (x is null) return -1;
        return x.Timestamp.CompareTo(y.Timestamp);
    }
}