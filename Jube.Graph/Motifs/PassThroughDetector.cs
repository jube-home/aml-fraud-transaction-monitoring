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

using Jube.Graph.Algorithms;

namespace Jube.Graph.Motifs
{
    public static class PassThroughDetector
    {
        public static IReadOnlyList<PatternInstance> Detect(IReadOnlyList<GraphEvent> events,
            PassThroughParameters? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(events);

            var p = parameters ?? PassThroughParameters.Default;

            if (p.MinimumRatio < 0 || p.MaximumRatio < p.MinimumRatio || p.MinimumEvents < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters));
            }

            var index = new EventIndex(events);
            var statistics = VertexStatistics.Compute(NodeEvents(events));
            var result = new List<PatternInstance>();

            foreach (var s in statistics)
            {
                var total = s.InboundAmounts.Count + s.OutboundAmounts.Count;

                if (total < p.MinimumEvents || s.InboundAmounts.Count == 0 || s.OutboundAmounts.Count == 0 ||
                    s.PassThroughRatio is not { } ratio || s.MedianHoldSeconds is not { } hold)
                {
                    continue;
                }

                if (ratio < p.MinimumRatio || ratio > p.MaximumRatio || hold > p.MaximumMedianHold.TotalSeconds)
                {
                    continue;
                }

                var inbound = index.Inbound(s.NodeId);
                var outbound = index.Outbound(s.NodeId);
                var evidence = inbound.Concat(outbound).ToArray();
                var members = new List<PatternMember> { new(s.NodeId, "PassThrough") };
                members.AddRange(inbound.Select(e => e.From).Distinct().OrderBy(n => n)
                    .Select(n => new PatternMember(n, "Source")));
                members.AddRange(outbound.Select(e => e.To).Distinct().OrderBy(n => n)
                    .Select(n => new PatternMember(n, "Destination")));

                result.Add(new PatternInstance(PatternKind.PassThrough, s.NodeId, members, TimeWindow.Of(evidence),
                    ratio, new SortedDictionary<string, double>
                    {
                        ["MinimumRatio"] = p.MinimumRatio,
                        ["MaximumRatio"] = p.MaximumRatio,
                        ["MaximumMedianHoldSeconds"] = p.MaximumMedianHold.TotalSeconds,
                        ["Ratio"] = ratio,
                        ["MedianHoldSeconds"] = hold,
                        ["Events"] = total
                    }, PatternInstance.ArchiveIdsOf(evidence)));
            }

            return result;
        }

        private static IEnumerable<(long NodeId, DateTimeOffset At, double Amount, bool Outbound)> NodeEvents(
            IEnumerable<GraphEvent> events)
        {
            foreach (var e in events)
            {
                if (e.From == e.To)
                {
                    continue;
                }

                yield return (e.From, e.At, e.Amount, true);
                yield return (e.To, e.At, e.Amount, false);
            }
        }
    }
}
