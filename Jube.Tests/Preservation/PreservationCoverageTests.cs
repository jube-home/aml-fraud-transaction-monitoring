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

using Jube.Data.Repository.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Preservation;
using Jube.Preservation.Models;
using LinqToDB.Mapping;
using MessagePack;
using Xunit;
using Key = MessagePack.KeyAttribute;

namespace Jube.Test.Preservation
{
    [Trait("Category", "Convention")]
    public sealed class PreservationCoverageTests
    {
        private static readonly Type[] advancedAnalyticsRoundTripped =
        [
            typeof(AdvancedAnalyticsInstance), typeof(AdvancedAnalyticsTrainedModel),
            typeof(AdvancedAnalyticsTrainedModelVersion), typeof(AdvancedAnalyticsTrainingRun),
            typeof(AdvancedAnalyticsTrainingRunStep), typeof(AdvancedAnalyticsTrainingRunTrial),
            typeof(AdvancedAnalyticsTrainingRunIteration), typeof(AdvancedAnalyticsTrainingRunVariable),
            typeof(AdvancedAnalyticsTrainingRunMetric), typeof(AdvancedAnalyticsTrainingRunCurvePoint)
        ];

        private static readonly HashSet<Type> tablesWithTheirOwnRoundTripTests =
        [
            typeof(ExhaustiveInstanceCsv),
            typeof(ExhaustiveInstanceCsvRow),
            typeof(ExhaustiveInstanceJson),
            typeof(ExhaustiveInstanceJsonRow),
            typeof(ExhaustiveInstanceCsvExport),
            typeof(ExhaustiveInstanceJsonExport)
        ];

        private static string TableName(Type type)
        {
            return type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;
        }

        private static IEnumerable<Type> CoveredPocos()
        {
            return typeof(AdvancedAnalyticsInstance).Assembly.GetTypes()
                .Where(w => w.Namespace == "Jube.Data.Poco" && w.GetCustomAttribute<TableAttribute>() != null)
                .Where(w => PreservationCoverage.CoveredPrefixes.Any(a =>
                    w.Name.StartsWith(a, StringComparison.Ordinal)
                    || TableName(w).StartsWith(a, StringComparison.Ordinal)));
        }

        private static HashSet<Type> ReachableFromPayload()
        {
            var reachable = new HashSet<Type>();
            var visited = new HashSet<Type>();
            var queue = new Queue<Type>();
            queue.Enqueue(typeof(Payload));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (!visited.Add(current))
                {
                    continue;
                }

                foreach (var property in current.GetProperties())
                {
                    var sequence = property.PropertyType.GetInterfaces().Append(property.PropertyType)
                        .FirstOrDefault(f => f.IsGenericType
                                             && f.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                    var element = sequence != null
                        ? sequence.GetGenericArguments()[0]
                        : property.PropertyType;

                    if (element.Namespace == "Jube.Data.Poco" && element.GetCustomAttribute<TableAttribute>() != null)
                    {
                        reachable.Add(element);
                        queue.Enqueue(element);
                    }
                    else if (element.Namespace == "Jube.Preservation.Models"
                             && element.GetCustomAttribute<MessagePackObjectAttribute>() != null)
                    {
                        queue.Enqueue(element);
                    }
                }
            }

            return reachable;
        }

        [Fact]
        public void EveryExhaustiveAndAnalyticsTableIsExportedOrExplicitlyExcludedWithAReason()
        {
            var reachable = ReachableFromPayload();

            var missing = CoveredPocos()
                .Where(w => !reachable.Contains(w) && !PreservationCoverage.Excluded.ContainsKey(w))
                .Select(s => s.Name)
                .Order(StringComparer.Ordinal)
                .ToList();

            missing.Should().BeEmpty(
                "these tables belong to a family the tenant export covers but are neither carried by the export nor " +
                "listed in PreservationCoverage.Excluded with a reason. To carry a table: (1) give the Poco a " +
                "[MessagePackObject] and a [Key] on every column, appending keys only; (2) add an " +
                "IEnumerable<ThePoco> property to its parent Poco (Exhaustive tables) or to AdvancedAnalyticsPayload " +
                "(AdvancedAnalytics tables) using the next unused [Key]; (3) load it in Preservation.ExportPayloadAsync " +
                "or AdvancedAnalyticsPreservation.ExportAsync; (4) delete or soft delete the tenant's existing rows " +
                "and insert it, remapping every foreign key, in Preservation.ImportAsync or " +
                "AdvancedAnalyticsPreservation.ImportAsync; (5) add it to ExhaustiveTreeSpec.Links (Exhaustive) or to " +
                "AdvancedAnalyticsSeed and the round trip test (AdvancedAnalytics); (6) document it in " +
                "docs/AdvancedAnalytics/PreservationAnalytics.md. If the table must not travel, add it to " +
                "PreservationCoverage.Excluded with the reason instead");
        }

        [Fact]
        public void ExclusionsAreRealFamilyTablesThatAreNotAlsoExportedAndCarryAReason()
        {
            var reachable = ReachableFromPayload();
            var covered = CoveredPocos().ToHashSet();

            foreach (var (type, reason) in PreservationCoverage.Excluded)
            {
                covered.Should().Contain(type, $"{type.Name} is excluded but is not a covered table");
                reachable.Should().NotContain(type, $"{type.Name} is both exported and excluded");
                reason.Length.Should().BeGreaterThan(30, $"{type.Name} needs a real reason");
            }
        }

        [Fact]
        public void TheCacheIsExcludedAndTheAnalyticsPayloadCarriesExactlyTheRoundTrippedTables()
        {
            PreservationCoverage.Excluded.Keys.Should().Contain(typeof(AdvancedAnalyticsCache));

            var carried = typeof(AdvancedAnalyticsPayload).GetProperties()
                .Select(s => s.PropertyType.GetGenericArguments().Single()).ToList();

            carried.Should().BeEquivalentTo(advancedAnalyticsRoundTripped);

            CoveredPocos().Where(w => w.Name.StartsWith("AdvancedAnalytics", StringComparison.Ordinal))
                .Where(w => !PreservationCoverage.Excluded.ContainsKey(w))
                .Should().BeEquivalentTo(advancedAnalyticsRoundTripped);
        }

        [Fact]
        public void TheExhaustiveTreeSpecCoversEveryExportedExhaustiveTable()
        {
            var exportedExhaustive = ReachableFromPayload()
                .Where(w => w.Name.StartsWith("ExhaustiveSearchInstance", StringComparison.Ordinal))
                .Where(w => !tablesWithTheirOwnRoundTripTests.Contains(w))
                .ToList();

            ExhaustiveTreeSpec.Types.Should().BeEquivalentTo(exportedExhaustive,
                "the round trip test seeds and compares every exported Exhaustive table; add the new table to " +
                "ExhaustiveTreeSpec.Links with its parent and foreign key");
        }

        public static IEnumerable<object[]> PreservedTypes()
        {
            return CoveredPocos().Select(s => new object[] { s })
                .Concat(new[]
                        { typeof(Payload), typeof(AdvancedAnalyticsPayload), typeof(Wrapper), typeof(EntityPermission) }
                    .Select(s => new object[] { s }));
        }

        [Theory]
        [MemberData(nameof(PreservedTypes))]
        public void EveryMappedMemberHasAUniqueMessagePackKeyOnAMessagePackObject(Type type)
        {
            type.GetCustomAttribute<MessagePackObjectAttribute>().Should()
                .NotBeNull($"{type.Name} must be a MessagePackObject");

            var members = type.GetProperties()
                .Where(w => w.GetCustomAttribute<ColumnAttribute>() != null
                            || w.GetCustomAttribute<AssociationAttribute>() != null
                            || type.Namespace == "Jube.Preservation.Models")
                .ToList();

            members.Should().NotBeEmpty();

            foreach (var member in members)
            {
                member.GetCustomAttribute<Key>().Should().NotBeNull($"{type.Name}.{member.Name} needs a Key");
            }

            var keys = type.GetProperties().Select(s => s.GetCustomAttribute<Key>()).Where(w => w != null)
                .Select(s => s!.IntKey!.Value).ToList();

            keys.Should().OnlyHaveUniqueItems(type.Name);
            keys.Should().OnlyContain(o => o >= 0);

            if (type.Name.StartsWith("AdvancedAnalytics", StringComparison.Ordinal))
            {
                keys.Order().Should().Equal(Enumerable.Range(0, keys.Count), $"{type.Name} keys are contiguous from 0");
            }
        }

        [Fact]
        public void EnumColumnsAreWrittenAsTheirIntegerValue()
        {
            var row = new AdvancedAnalyticsTrainedModel
            {
                KindId = AdvancedAnalyticsKind.Regressor,
                SourceId = AdvancedAnalyticsSource.Upload,
                Name = "n"
            };

            var array = MessagePackSerializer.Deserialize<object[]>(MessagePackSerializer.Serialize(row));

            Convert.ToInt32(array[5]).Should().Be((int)AdvancedAnalyticsKind.Regressor);
            Convert.ToInt32(array[8]).Should().Be((int)AdvancedAnalyticsSource.Upload);
        }

        public static IEnumerable<object[]> AnalyticsTypes()
        {
            return advancedAnalyticsRoundTripped.Select(s => new object[] { s });
        }

        [Theory]
        [MemberData(nameof(AnalyticsTypes))]
        public void EveryAnalyticsColumnSurvivesMessagePackWhetherSetOrNull(Type type)
        {
            var counter = 0;
            var filled = ExhaustiveTreeSpec.Fill(type, ref counter);

            foreach (var property in type.GetProperties().Where(w => w.PropertyType == typeof(DateTime)
                                                                     || w.PropertyType == typeof(DateTime?)))
            {
                if (property.GetValue(filled) is DateTime value)
                {
                    property.SetValue(filled, DateTime.SpecifyKind(value, DateTimeKind.Utc));
                }
            }

            var nulled = Activator.CreateInstance(type)!;

            foreach (var property in RowCompare.ColumnProperties(type)
                         .Where(w => !w.PropertyType.IsValueType || Nullable.GetUnderlyingType(w.PropertyType) != null))
            {
                property.SetValue(nulled, null);
            }

            foreach (var row in new[] { filled, nulled })
            {
                var bytes = MessagePackSerializer.Serialize(type, row);
                var back = MessagePackSerializer.Deserialize(type, bytes);

                foreach (var property in RowCompare.ColumnProperties(type))
                {
                    var expected = property.GetValue(row);
                    var actual = property.GetValue(back);

                    if (expected is byte[] expectedBytes)
                    {
                        ((byte[])actual!).Should().Equal(expectedBytes);
                    }
                    else if (expected is DateTime expectedDate)
                    {
                        ((DateTime)actual!).Ticks.Should().Be(expectedDate.Ticks, property.Name);
                    }
                    else
                    {
                        actual.Should().Be(expected, $"{type.Name}.{property.Name}");
                    }
                }
            }
        }

        [Fact]
        public void KeysOfTheCoveredTypesNeverRenumberAndNewMembersOnlyAppend()
        {
            var assembly = typeof(PreservationCoverageTests).Assembly;
            var resource = assembly.GetManifestResourceNames().Single(s => s.EndsWith("Preservation.KeyBaseline.txt"));
            using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);

            var baseline = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().Split('='))
                .Where(w => w.Length == 2)
                .Select(s => (Member: s[0], Key: int.Parse(s[1])))
                .ToList();

            baseline.Should().HaveCountGreaterThan(400);

            var types = typeof(AdvancedAnalyticsInstance).Assembly.GetTypes()
                .Concat(typeof(Payload).Assembly.GetTypes())
                .Where(w => w.Namespace is "Jube.Data.Poco" or "Jube.Preservation.Models")
                .ToDictionary(s => s.Name, s => s);

            foreach (var (member, key) in baseline)
            {
                var parts = member.Split('.');
                types.Should().ContainKey(parts[0]);
                var property = types[parts[0]].GetProperty(parts[1]);

                property.Should()
                    .NotBeNull($"{member} was removed; a preserved member may never be removed or renamed");

                property!.GetCustomAttribute<Key>()!.IntKey.Should().Be(key,
                    $"{member} was renumbered; keys are append only so that older exports stay readable");
            }

            foreach (var group in baseline.GroupBy(g => g.Member.Split('.')[0]))
            {
                var highest = group.Max(m => m.Key);
                var known = group.Select(s => s.Member.Split('.')[1]).ToHashSet();

                foreach (var property in types[group.Key].GetProperties()
                             .Where(w => w.GetCustomAttribute<Key>() != null && !known.Contains(w.Name)))
                {
                    property.GetCustomAttribute<Key>()!.IntKey.Should().BeGreaterThan(highest,
                        $"{group.Key}.{property.Name} is new and must take a key above {highest}");
                }
            }
        }
    }
}