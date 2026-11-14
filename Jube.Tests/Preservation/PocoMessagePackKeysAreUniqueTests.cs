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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Jube.Data.Poco;
using LinqToDB.Mapping;
using MessagePack;
using Xunit;

namespace Jube.Test.Preservation
{
    public class PocoMessagePackKeysAreUniqueTests
    {
        public static IEnumerable<object[]> MessagePackPocos()
        {
            return typeof(AskJoobyAnalyticsInstance).Assembly.GetTypes()
                .Where(w => w.Namespace == "Jube.Data.Poco" &&
                            w.GetCustomAttribute<MessagePackObjectAttribute>() != null)
                .OrderBy(o => o.Name, StringComparer.Ordinal)
                .Select(s => new object[] { s });
        }

        [Theory]
        [MemberData(nameof(MessagePackPocos))]
        public void KeysAreUniqueAndNonNegativeWithinEveryMessagePackPoco(Type type)
        {
            var keys = type.GetProperties()
                .Select(s => (s.Name, Key: s.GetCustomAttribute<KeyAttribute>()))
                .Where(w => w.Key?.IntKey != null)
                .Select(s => (s.Name, Key: s.Key!.IntKey!.Value))
                .ToList();

            keys.Select(s => s.Key).Should().OnlyContain(o => o >= 0, type.Name);

            var duplicates = keys.GroupBy(g => g.Key).Where(w => w.Count() > 1)
                .Select(s => $"{s.Key}: {string.Join(", ", s.Select(m => m.Name))}").ToList();

            duplicates.Should().BeEmpty($"{type.Name} reuses a MessagePack key");
        }

        [Theory]
        [MemberData(nameof(MessagePackPocos))]
        public void EveryMappedColumnOfTheExhaustiveAndAdvancedAnalyticsFamiliesHasAKey(Type type)
        {
            if (!type.Name.StartsWith("ExhaustiveSearchInstance", StringComparison.Ordinal)
                && !type.Name.StartsWith("ExhaustiveInstance", StringComparison.Ordinal)
                && !type.Name.StartsWith("AdvancedAnalytics", StringComparison.Ordinal))
            {
                return;
            }

            var unkeyed = type.GetProperties()
                .Where(w => w.GetCustomAttribute<ColumnAttribute>() != null
                            && w.GetCustomAttribute<KeyAttribute>() == null)
                .Select(s => s.Name).ToList();

            unkeyed.Should().BeEmpty($"{type.Name} has mapped columns without a MessagePack key");
        }
    }
}