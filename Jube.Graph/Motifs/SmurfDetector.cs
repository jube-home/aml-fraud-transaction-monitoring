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

namespace Jube.Graph.Motifs
{
    public static class SmurfDetector
    {
        public static IReadOnlyList<PatternInstance> Detect(IReadOnlyList<GraphEvent> events,
            SmurfParameters? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(events);

            var p = parameters ?? SmurfParameters.Default;

            if (p.MinimumAccounts < 3 || p.MaximumAccounts < p.MinimumAccounts || p.MinimumFan < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(parameters));
            }

            var filtered = events.Where(e => e.Amount >= p.MinimumAmount && e.From != e.To).ToArray();
            var full = new EventIndex(filtered);
            var allowed = full.Nodes.Where(n => full.DistinctCounterparts(n) <= p.MaximumDegree).ToHashSet();
            var index = new EventIndex(filtered.Where(e => allowed.Contains(e.From) && allowed.Contains(e.To)));
            var result = new List<PatternInstance>();

            foreach (var middle in index.Nodes)
            {
                TypeOne(index, middle, p, result);
            }

            foreach (var source in index.Nodes)
            {
                TypeTwo(index, source, p, result);
            }

            return result;
        }

        private static void TypeOne(EventIndex index, long middle, SmurfParameters p, List<PatternInstance> result)
        {
            var inbound = index.Inbound(middle);
            var outbound = index.Outbound(middle);
            var i = 0;

            while (i < inbound.Count)
            {
                var from = inbound[i].At;
                var until = from + p.Span;
                var end = EventIndex.WindowEnd(inbound, i, until);
                var gathered = inbound.Skip(i).Take(end - i + 1).ToArray();
                var scattered = outbound.Where(e => e.At > from && e.At <= until).ToArray();
                var sources = gathered.Select(e => e.From).Distinct().OrderBy(s => s).ToArray();
                var targets = scattered.Select(e => e.To).Distinct().OrderBy(t => t).ToArray();
                var accounts = sources.Concat(targets).Append(middle).Distinct().Count();

                if (sources.Length < p.MinimumFan || targets.Length < p.MinimumFan ||
                    accounts < p.MinimumAccounts || accounts > p.MaximumAccounts)
                {
                    i++;

                    continue;
                }

                var evidence = gathered.Concat(scattered).ToArray();
                var members = new List<PatternMember>();
                members.AddRange(sources.Select(s => new PatternMember(s, "Source")));
                members.Add(new PatternMember(middle, "Middle"));
                members.AddRange(targets.Select(t => new PatternMember(t, "Target")));

                result.Add(new PatternInstance(PatternKind.SmurfTypeOne, middle, members, TimeWindow.Of(evidence),
                    accounts, Parameters(p, accounts), PatternInstance.ArchiveIdsOf(evidence)));

                i = end + 1;
            }
        }

        private static void TypeTwo(EventIndex index, long source, SmurfParameters p, List<PatternInstance> result)
        {
            var outbound = index.Outbound(source);
            var i = 0;

            while (i < outbound.Count)
            {
                var from = outbound[i].At;
                var until = from + p.Span;
                var end = EventIndex.WindowEnd(outbound, i, until);
                var firstLegs = outbound.Skip(i).Take(end - i + 1).ToArray();
                var byTarget = new SortedDictionary<long, List<(long Middle, GraphEvent First, GraphEvent Second)>>();

                foreach (var first in firstLegs)
                {
                    foreach (var second in index.Outbound(first.To))
                    {
                        if (second.At <= first.At || second.At > until || second.To == source)
                        {
                            continue;
                        }

                        if (!byTarget.TryGetValue(second.To, out var list))
                        {
                            list = [];
                            byTarget[second.To] = list;
                        }

                        list.Add((first.To, first, second));
                    }
                }

                var emitted = false;

                foreach (var (target, legs) in byTarget)
                {
                    var middles = legs.Select(l => l.Middle).Distinct().OrderBy(m => m).ToArray();
                    var accounts = middles.Length + 2;

                    if (middles.Length < p.MinimumFan || accounts < p.MinimumAccounts || accounts > p.MaximumAccounts)
                    {
                        continue;
                    }

                    var evidence = legs.Select(l => l.First).Concat(legs.Select(l => l.Second)).ToArray();
                    var members = new List<PatternMember> { new(source, "Source") };
                    members.AddRange(middles.Select(m => new PatternMember(m, "Middle")));
                    members.Add(new PatternMember(target, "Target"));

                    result.Add(new PatternInstance(PatternKind.SmurfTypeTwo, source, members, TimeWindow.Of(evidence),
                        accounts, Parameters(p, accounts), PatternInstance.ArchiveIdsOf(evidence)));
                    emitted = true;
                }

                i = emitted ? end + 1 : i + 1;
            }
        }

        private static SortedDictionary<string, double> Parameters(SmurfParameters p, int accounts)
        {
            return new SortedDictionary<string, double>
            {
                ["MinimumAccounts"] = p.MinimumAccounts,
                ["MaximumAccounts"] = p.MaximumAccounts,
                ["MinimumFan"] = p.MinimumFan,
                ["SpanSeconds"] = p.Span.TotalSeconds,
                ["MinimumAmount"] = p.MinimumAmount,
                ["MaximumDegree"] = p.MaximumDegree,
                ["Accounts"] = accounts
            };
        }
    }
}
