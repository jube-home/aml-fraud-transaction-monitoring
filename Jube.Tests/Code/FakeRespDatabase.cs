/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using Jube.ResilientRespConnection.Interfaces;
using StackExchange.Redis;

namespace Jube.Test.Code;

public class FakeRespDatabase : DispatchProxy
{
    public ConcurrentDictionary<string, byte[]> Strings { get; set; } = new();
    public ConcurrentDictionary<string, ConcurrentDictionary<string, string>> Hashes { get; set; } = new();

    public static IHybridResilientRespDatabase Create(FakeRespDatabase shared)
    {
        var proxy = DispatchProxy.Create<IHybridResilientRespDatabase, FakeRespDatabase>();
        var fake = (FakeRespDatabase)(object)proxy;

        fake.Strings = shared.Strings;
        fake.Hashes = shared.Hashes;

        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        switch (targetMethod!.Name)
        {
            case "KeyExistsAsync":
                return Task.FromResult(Strings.ContainsKey(args![0]!.ToString()!));
            case "StringSetAsync":
                Strings[args![0]!.ToString()!] = (byte[])(RedisValue)args[1]!;
                return Task.FromResult(true);
            case "StringGetAsync":
                return Task.FromResult(Strings.TryGetValue(args![0]!.ToString()!, out var bytes)
                    ? (RedisValue)bytes
                    : RedisValue.Null);
            case "HashSet" when args![1] is HashEntry[] entries:
                var hash = Hashes.GetOrAdd(args[0]!.ToString()!, _ => new ConcurrentDictionary<string, string>());

                foreach (var entry in entries)
                {
                    hash[entry.Name.ToString()] = entry.Value.ToString();
                }

                return null;
            case "HashGetAsync":
                return Task.FromResult(Hashes.TryGetValue(args![0]!.ToString()!, out var fields)
                                       && fields.TryGetValue(args[1]!.ToString()!, out var value)
                    ? (RedisValue)value
                    : RedisValue.Null);
            default:
                throw new NotSupportedException(targetMethod.Name);
        }
    }
}
