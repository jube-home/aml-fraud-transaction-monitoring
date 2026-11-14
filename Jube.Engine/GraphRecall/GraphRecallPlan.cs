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
using Jube.Data.Poco;

namespace Jube.Engine.GraphRecall
{
    public sealed class GraphRecallPlan
    {
        public const int DefaultDistanceCap = 4;

        public static readonly GraphRecallPlan Empty = new([], DefaultDistanceCap);

        private GraphRecallPlan(IReadOnlyList<GraphRecallEntity> entities, int distanceCap)
        {
            Entities = entities;
            DistanceCap = distanceCap;
            Names = [.. entities.Select(e => e.Name)];
        }

        public IReadOnlyList<GraphRecallEntity> Entities { get; }
        public IReadOnlyList<string> Names { get; }
        public int DistanceCap { get; }
        public int Beyond => DistanceCap + 1;
        public bool Any => Entities.Count > 0;

        public static GraphRecallPlan Build(IEnumerable<EntityAnalysisModelGraphRecall> rows, int distanceCap)
        {
            var configured = rows
                .Where(r => r is { Active: 1 } && (r.Deleted ?? 0) == 0 && !string.IsNullOrWhiteSpace(r.Name) &&
                            !string.IsNullOrWhiteSpace(r.RequestXPathName))
                .OrderBy(r => r.Id)
                .GroupBy(r => r.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var names = configured.Select(r => r.Name.Trim()).ToList();

            var entities = configured.Select(r =>
            {
                var field = r.RequestXPathName.Trim().Replace(" ", "_");
                var counterpart = string.IsNullOrWhiteSpace(r.CounterpartName) ? null : r.CounterpartName.Trim();

                var counterpartIndex = counterpart == null
                    ? -1
                    : names.FindIndex(n => string.Equals(n, counterpart, StringComparison.OrdinalIgnoreCase));

                return new GraphRecallEntity(r.Name.Trim(), field,
                    string.IsNullOrWhiteSpace(r.NodeKey) ? field : r.NodeKey.Trim(), r.GraphRegistryGuid,
                    counterpart, r.ResponsePayload == 1, (r.ReportTable ?? 1) == 1,
                    counterpartIndex == names.IndexOf(r.Name.Trim()) ? -1 : counterpartIndex);
            }).ToList();

            return new GraphRecallPlan(entities, distanceCap);
        }

        public static int ResolveDistanceCap(string setting)
        {
            return int.TryParse(setting, out var cap) && cap > 0 ? cap : DefaultDistanceCap;
        }
    }
}
