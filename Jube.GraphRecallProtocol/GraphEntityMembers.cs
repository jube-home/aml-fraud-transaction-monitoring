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
using System.Globalization;
using System.Linq;

namespace Jube.GraphRecallProtocol
{
    public static class GraphEntityMembers
    {
        public const int StringType = 1;
        public const int IntegerType = 2;
        public const int DoubleType = 3;
        public const int BooleanType = 5;
        public const string TagMethod = "Tag";
        public const string PatternMethod = "Pattern";

        private const string IntegerCast = "::int";
        private const string LongCast = "::bigint";
        private const string DoubleCast = "::double precision";
        private const string BooleanCast = "::boolean";

        public static readonly IReadOnlyList<string> MethodNames = [TagMethod, PatternMethod];

        public static readonly IReadOnlyList<string> ProximityMembers =
            [nameof(GraphTagProximity.Distance), nameof(GraphTagProximity.Within1), nameof(GraphTagProximity.Within2), nameof(GraphTagProximity.Within3), nameof(GraphTagProximity.Known)];

        public static readonly IReadOnlyList<GraphEntityMember> Scalars =
        [
            new(nameof(GraphEntity.Value), StringType, string.Empty, e => e.Value, (e, v) => e.Value = ToText(v)),
            new(nameof(GraphEntity.Known), BooleanType, BooleanCast, e => e.Known, (e, v) => e.Known = ToBoolean(v)),
            new(nameof(GraphEntity.Hub), BooleanType, BooleanCast, e => e.Hub, (e, v) => e.Hub = ToBoolean(v)),
            new(nameof(GraphEntity.Degree), IntegerType, IntegerCast, e => e.Degree, (e, v) => e.Degree = ToInt32(v)),
            new(nameof(GraphEntity.DegreeMoney), IntegerType, IntegerCast, e => e.DegreeMoney, (e, v) => e.DegreeMoney = ToInt32(v)),
            new(nameof(GraphEntity.DegreeIdentity), IntegerType, IntegerCast, e => e.DegreeIdentity, (e, v) => e.DegreeIdentity = ToInt32(v)),
            new(nameof(GraphEntity.HubNeighbourCount), IntegerType, IntegerCast, e => e.HubNeighbourCount, (e, v) => e.HubNeighbourCount = ToInt32(v)),
            new(nameof(GraphEntity.DistanceToConfirmed), IntegerType, IntegerCast, e => e.DistanceToConfirmed, (e, v) => e.DistanceToConfirmed = ToInt32(v)),
            new(nameof(GraphEntity.DistanceToNotConfirmed), IntegerType, IntegerCast, e => e.DistanceToNotConfirmed, (e, v) => e.DistanceToNotConfirmed = ToInt32(v)),
            new(nameof(GraphEntity.ConfirmedWithin1), IntegerType, IntegerCast, e => e.ConfirmedWithin1, (e, v) => e.ConfirmedWithin1 = ToInt32(v)),
            new(nameof(GraphEntity.ConfirmedWithin2), IntegerType, IntegerCast, e => e.ConfirmedWithin2, (e, v) => e.ConfirmedWithin2 = ToInt32(v)),
            new(nameof(GraphEntity.ConfirmedWithin3), IntegerType, IntegerCast, e => e.ConfirmedWithin3, (e, v) => e.ConfirmedWithin3 = ToInt32(v)),
            new(nameof(GraphEntity.CommunityId), IntegerType, LongCast, e => e.CommunityId, (e, v) => e.CommunityId = ToInt64(v)),
            new(nameof(GraphEntity.CommunitySize), IntegerType, IntegerCast, e => e.CommunitySize, (e, v) => e.CommunitySize = ToInt32(v)),
            new(nameof(GraphEntity.CommunityConfirmedRate), DoubleType, DoubleCast, e => e.CommunityConfirmedRate, (e, v) => e.CommunityConfirmedRate = ToDouble(v)),
            new(nameof(GraphEntity.CommunityRateRatio), DoubleType, DoubleCast, e => e.CommunityRateRatio, (e, v) => e.CommunityRateRatio = ToDouble(v)),
            new(nameof(GraphEntity.PageRank), DoubleType, DoubleCast, e => e.PageRank, (e, v) => e.PageRank = ToDouble(v)),
            new(nameof(GraphEntity.HubScore), DoubleType, DoubleCast, e => e.HubScore, (e, v) => e.HubScore = ToDouble(v)),
            new(nameof(GraphEntity.AuthorityScore), DoubleType, DoubleCast, e => e.AuthorityScore, (e, v) => e.AuthorityScore = ToDouble(v)),
            new(nameof(GraphEntity.ClusteringCoefficient), DoubleType, DoubleCast, e => e.ClusteringCoefficient, (e, v) => e.ClusteringCoefficient = ToDouble(v)),
            new(nameof(GraphEntity.PropagationScore), DoubleType, DoubleCast, e => e.PropagationScore, (e, v) => e.PropagationScore = ToDouble(v)),
            new(nameof(GraphEntity.CoreNumber), IntegerType, IntegerCast, e => e.CoreNumber, (e, v) => e.CoreNumber = ToInt32(v)),
            new(nameof(GraphEntity.NearestTag), StringType, string.Empty, e => e.NearestTag, (e, v) => e.NearestTag = ToText(v)),
            new(nameof(GraphEntity.NearestTagDistance), IntegerType, IntegerCast, e => e.NearestTagDistance, (e, v) => e.NearestTagDistance = ToInt32(v)),
            new($"{nameof(GraphEntity.Edge)}.{nameof(GraphEdgeFacts.Exists)}", BooleanType, BooleanCast, e => e.Edge.Exists, (e, v) => e.Edge.Exists = ToBoolean(v)),
            new($"{nameof(GraphEntity.Edge)}.{nameof(GraphEdgeFacts.Count)}", IntegerType, IntegerCast, e => e.Edge.Count, (e, v) => e.Edge.Count = ToInt32(v)),
            new($"{nameof(GraphEntity.Edge)}.{nameof(GraphEdgeFacts.AmountSum)}", DoubleType, DoubleCast, e => e.Edge.AmountSum, (e, v) => e.Edge.AmountSum = ToDouble(v)),
            new($"{nameof(GraphEntity.Edge)}.{nameof(GraphEdgeFacts.FirstSeenDays)}", DoubleType, DoubleCast, e => e.Edge.FirstSeenDays, (e, v) => e.Edge.FirstSeenDays = ToDouble(v)),
            new($"{nameof(GraphEntity.Edge)}.{nameof(GraphEdgeFacts.LastSeenDays)}", DoubleType, DoubleCast, e => e.Edge.LastSeenDays, (e, v) => e.Edge.LastSeenDays = ToDouble(v)),
            new($"{nameof(GraphEntity.Edge)}.{nameof(GraphEdgeFacts.Kind)}", StringType, string.Empty, e => e.Edge.Kind, (e, v) => e.Edge.Kind = ToText(v)),
            new(nameof(GraphEntity.BridgesCommunities), BooleanType, BooleanCast, e => e.BridgesCommunities, (e, v) => e.BridgesCommunities = ToBoolean(v)),
            new(nameof(GraphEntity.BridgesToTagged), BooleanType, BooleanCast, e => e.BridgesToTagged, (e, v) => e.BridgesToTagged = ToBoolean(v)),
            new(nameof(GraphEntity.RelaxedDistanceToConfirmed), IntegerType, IntegerCast, e => e.RelaxedDistanceToConfirmed, (e, v) => e.RelaxedDistanceToConfirmed = ToInt32(v))
        ];

        public static readonly IReadOnlyList<string> Tokens =
        [
            .. Scalars.SelectMany(s => s.Segments).Concat(MethodNames).Concat(ProximityMembers)
                .Distinct(StringComparer.Ordinal)
        ];

        public static bool TryGet(string name, out GraphEntityMember member)
        {
            member = name == null
                ? null
                : Scalars.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

            return member != null;
        }

        public static bool IsMethod(string name)
        {
            return MethodNames.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsMember(string name)
        {
            return IsMethod(name) || TryGet(name, out _);
        }

        public static bool TryReadNumber(IDictionary<string, GraphEntity> graph, string qualifiedName,
            out double value)
        {
            value = 0;

            if (!TryResolve(graph, qualifiedName, out var entity, out var member))
            {
                return false;
            }

            var read = member.Read(entity);

            switch (read)
            {
                case bool flag:
                    value = flag ? 1 : 0;

                    return true;
                case string text:
                    return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                case IConvertible convertible:
                    value = convertible.ToDouble(CultureInfo.InvariantCulture);

                    return true;
                default:
                    return false;
            }
        }

        public static bool TryReadText(IDictionary<string, GraphEntity> graph, string qualifiedName, out string value)
        {
            value = null;

            if (!TryResolve(graph, qualifiedName, out var entity, out var member))
            {
                return false;
            }

            value = ToText(member.Read(entity));

            return true;
        }

        public static bool TryWrite(IDictionary<string, GraphEntity> graph, string qualifiedName, object value)
        {
            if (graph == null || !Split(qualifiedName, out var entityName, out var memberName) ||
                !TryGet(memberName, out var member))
            {
                return false;
            }

            if (!graph.TryGetValue(entityName, out var entity) || entity == null)
            {
                entity = new GraphEntity();
                graph[entityName] = entity;
            }

            member.Write(entity, value);

            return true;
        }

        public static bool Split(string qualifiedName, out string entityName, out string memberName)
        {
            entityName = null;
            memberName = null;

            if (string.IsNullOrEmpty(qualifiedName))
            {
                return false;
            }

            var separator = qualifiedName.IndexOf('.');

            if (separator <= 0 || separator == qualifiedName.Length - 1)
            {
                return false;
            }

            entityName = qualifiedName[..separator];
            memberName = qualifiedName[(separator + 1)..];

            return true;
        }

        public static string CamelCase(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
            {
                return name;
            }

            return char.ToLowerInvariant(name[0]) + name[1..];
        }

        private static bool TryResolve(IDictionary<string, GraphEntity> graph, string qualifiedName,
            out GraphEntity entity, out GraphEntityMember member)
        {
            entity = null;
            member = null;

            return graph != null && Split(qualifiedName, out var entityName, out var memberName) &&
                   graph.TryGetValue(entityName, out entity) && entity != null && TryGet(memberName, out member);
        }

        private static string ToText(object value)
        {
            return value switch
            {
                null => string.Empty,
                bool flag => flag ? "true" : "false",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        private static bool ToBoolean(object value)
        {
            return value switch
            {
                null => false,
                bool flag => flag,
                string text => bool.TryParse(text, out var parsed) ? parsed : text.Trim() == "1",
                IConvertible convertible => Math.Abs(convertible.ToDouble(CultureInfo.InvariantCulture)) > 0,
                _ => false
            };
        }

        private static double ToDouble(object value)
        {
            return value switch
            {
                null => 0,
                bool flag => flag ? 1 : 0,
                string text => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : 0,
                IConvertible convertible => convertible.ToDouble(CultureInfo.InvariantCulture),
                _ => 0
            };
        }

        private static int ToInt32(object value)
        {
            return (int)Math.Clamp(Math.Round(ToDouble(value)), int.MinValue, int.MaxValue);
        }

        private static long ToInt64(object value)
        {
            return (long)Math.Clamp(Math.Round(ToDouble(value)), long.MinValue, long.MaxValue);
        }
    }
}
