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

namespace Jube.Graph.Algorithms
{
    public static class VertexStatistics
    {
        public static IReadOnlyList<NodeStatistics> Compute(
            IEnumerable<(long NodeId, DateTimeOffset At, double Amount, bool Outbound)> events)
        {
            ArgumentNullException.ThrowIfNull(events);

            var byNode = new SortedDictionary<long, List<(DateTimeOffset At, double Amount, bool Outbound)>>();

            foreach (var (nodeId, at, amount, outbound) in events)
            {
                if (!byNode.TryGetValue(nodeId, out var list))
                {
                    list = [];
                    byNode[nodeId] = list;
                }

                list.Add((at, amount, outbound));
            }

            var result = new List<NodeStatistics>(byNode.Count);

            foreach (var (nodeId, list) in byNode)
            {
                result.Add(ForNode(nodeId, list));
            }

            return result;
        }

        public static bool IsRoundAmount(double amount)
        {
            if (amount <= 0 || !double.IsFinite(amount))
            {
                return false;
            }

            var hundreds = amount / 100d;

            return Math.Abs(hundreds - Math.Round(hundreds)) < 1e-9;
        }

        private static NodeStatistics ForNode(long nodeId, List<(DateTimeOffset At, double Amount, bool Outbound)> list)
        {
            var ordered = list.OrderBy(e => e.At).ThenBy(e => e.Outbound).ToArray();
            var inbound = new List<double>();
            var outbound = new List<double>();
            var interArrival = new List<double>();
            var round = 0;

            for (var i = 0; i < ordered.Length; i++)
            {
                var (at, amount, isOutbound) = ordered[i];
                (isOutbound ? outbound : inbound).Add(amount);

                if (IsRoundAmount(amount))
                {
                    round++;
                }

                if (i > 0)
                {
                    interArrival.Add((at - ordered[i - 1].At).TotalSeconds);
                }
            }

            var inboundMoments = Moments.Of(inbound);
            var outboundMoments = Moments.Of(outbound);
            var holds = new List<double>();
            var nextOutbound = 0;

            for (var i = 0; i < ordered.Length; i++)
            {
                if (ordered[i].Outbound)
                {
                    continue;
                }

                nextOutbound = Math.Max(nextOutbound, i + 1);

                while (nextOutbound < ordered.Length && !ordered[nextOutbound].Outbound)
                {
                    nextOutbound++;
                }

                if (nextOutbound < ordered.Length)
                {
                    holds.Add((ordered[nextOutbound].At - ordered[i].At).TotalSeconds);
                }
            }

            return new NodeStatistics(nodeId, inboundMoments, outboundMoments, Moments.Of(interArrival),
                ordered.Length == 0 ? 0d : round / (double)ordered.Length,
                inboundMoments.Sum > 0 ? outboundMoments.Sum / inboundMoments.Sum : null,
                holds.Count == 0 ? null : Moments.Of(holds).Median);
        }
    }
}
