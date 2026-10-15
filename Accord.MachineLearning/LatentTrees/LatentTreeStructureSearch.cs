// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Jube, 2026
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Collections.Generic;
using System.Threading;

namespace Accord.MachineLearning.LatentTrees
{
    internal sealed class LatentGroup
    {
        public LatentGroup[] Children = new LatentGroup[0];
        public int Feature = -1;
        public int[] Features;
        public int Height;
        public int Id;
        public double Score;
        public int States;
    }

    internal sealed class LatentTreeStructureSearch
    {
        private readonly int classCount;
        private readonly double[] classPrior;
        private readonly int[] classes;
        private readonly int[][] columns;
        private readonly int[] featureStates;
        private readonly LatentTreeOptions options;
        private readonly CancellationToken token;
        private readonly double totalWeight;
        private readonly double[] weights;
        private int fitCounter;
        private int nextId;

        public LatentTreeStructureSearch(int[][] columns, int[] featureStates, int[] classes, double[] weights,
            double[] classPrior, LatentTreeOptions options, CancellationToken token)
        {
            this.columns = columns;
            this.featureStates = featureStates;
            this.classes = classes;
            this.weights = weights;
            this.classPrior = classPrior;
            classCount = classPrior.Length;
            this.options = options;
            this.token = token;
            double total = 0;
            for (var i = 0; i < weights.Length; i++)
            {
                total += weights[i];
            }

            totalWeight = total;
        }

        public List<LatentGroup> Run()
        {
            var p = columns.Length;
            var groups = new List<LatentGroup>();
            for (var f = 0; f < p; f++)
            {
                var leaf = new LatentGroup
                {
                    Id = nextId++,
                    Feature = f,
                    States = featureStates[f],
                    Height = 0,
                    Features = new[] { f }
                };
                leaf.Score = ScoreOf(leaf);
                groups.Add(leaf);
            }

            if (p < 2 || options.MaxDepth < 1)
            {
                return groups;
            }

            var edges = SpanningTree(p);
            var owner = new int[p];
            var cache = new Dictionary<long, LatentGroup>();

            while (groups.Count > 1)
            {
                token.ThrowIfCancellationRequested();
                for (var g = 0; g < groups.Count; g++)
                {
                    foreach (var f in groups[g].Features)
                    {
                        owner[f] = g;
                    }
                }

                var linkage = new Dictionary<long, double>();
                foreach (var e in edges)
                {
                    var a = owner[e.Item1];
                    var b = owner[e.Item2];
                    if (a == b)
                    {
                        continue;
                    }

                    var lo = System.Math.Min(a, b);
                    var hi = System.Math.Max(a, b);
                    var key = (long)lo * groups.Count + hi;
                    double current;
                    if (!linkage.TryGetValue(key, out current) || e.Item3 > current)
                    {
                        linkage[key] = e.Item3;
                    }
                }

                var pairs = new List<KeyValuePair<long, double>>(linkage);
                pairs.Sort((x, y) =>
                {
                    var c = y.Value.CompareTo(x.Value);
                    return c != 0 ? c : x.Key.CompareTo(y.Key);
                });

                var limit = System.Math.Min(System.Math.Max(1, options.CandidatesPerRound), pairs.Count);
                LatentGroup bestGroup = null;
                var bestGain = options.MinimumGain;
                var bestA = -1;
                var bestB = -1;

                for (var k = 0; k < limit; k++)
                {
                    token.ThrowIfCancellationRequested();
                    var a = (int)(pairs[k].Key / groups.Count);
                    var b = (int)(pairs[k].Key % groups.Count);
                    var ga = groups[a];
                    var gb = groups[b];

                    foreach (var candidate in Candidates(ga, gb, cache))
                    {
                        var gain = candidate.Score - ga.Score - gb.Score;
                        if (gain > bestGain)
                        {
                            bestGain = gain;
                            bestGroup = candidate;
                            bestA = a;
                            bestB = b;
                        }
                    }
                }

                if (bestGroup == null)
                {
                    break;
                }

                groups[bestA] = bestGroup;
                groups.RemoveAt(bestB);
            }

            return groups;
        }

        private IEnumerable<LatentGroup> Candidates(LatentGroup a, LatentGroup b, Dictionary<long, LatentGroup> cache)
        {
            var results = new List<LatentGroup>();
            var depth = options.MaxDepth;

            if (1 + System.Math.Max(a.Height, b.Height) <= depth)
            {
                results.Add(Cached(cache, a, b, 0, () => BestLatent(new[] { a, b })));
            }

            if (a.Feature < 0 && System.Math.Max(a.Height, b.Height + 1) <= depth)
            {
                results.Add(Cached(cache, a, b, 1, () => BestLatent(Append(a.Children, b))));
            }

            if (b.Feature < 0 && System.Math.Max(b.Height, a.Height + 1) <= depth)
            {
                results.Add(Cached(cache, a, b, 2, () => BestLatent(Append(b.Children, a))));
            }

            results.RemoveAll(r => r == null);
            return results;
        }

        private LatentGroup Cached(Dictionary<long, LatentGroup> cache, LatentGroup a, LatentGroup b, int kind,
            Func<LatentGroup> build)
        {
            var key = (a.Id * 1000003L + b.Id) * 4 + kind;
            LatentGroup value;
            if (!cache.TryGetValue(key, out value))
            {
                value = build();
                cache[key] = value;
            }

            return value;
        }

        private static LatentGroup[] Append(LatentGroup[] children, LatentGroup extra)
        {
            var result = new LatentGroup[children.Length + 1];
            Array.Copy(children, result, children.Length);
            result[children.Length] = extra;
            return result;
        }

        private LatentGroup BestLatent(LatentGroup[] children)
        {
            LatentGroup best = null;
            for (var k = 2; k <= options.MaxLatentStates; k++)
            {
                token.ThrowIfCancellationRequested();
                var height = 0;
                var features = new List<int>();
                foreach (var c in children)
                {
                    if (c.Height + 1 > height)
                    {
                        height = c.Height + 1;
                    }

                    features.AddRange(c.Features);
                }

                features.Sort();
                var candidate = new LatentGroup
                {
                    Id = nextId++,
                    States = k,
                    Height = height,
                    Children = children,
                    Features = features.ToArray()
                };
                candidate.Score = ScoreOf(candidate);
                if (best != null && candidate.Score <= best.Score)
                {
                    break;
                }

                best = candidate;
            }

            return best;
        }

        private double ScoreOf(LatentGroup group)
        {
            var parents = new List<int> { -1 };
            var states = new List<int> { classCount };
            var cols = new List<int[]> { null };
            Flatten(group, 0, parents, states, cols);

            var shape = new LatentTreeShape(parents.ToArray(), states.ToArray());
            var leafOnly = group.Feature >= 0;
            var seed = unchecked(options.Seed * 31 + fitCounter++ * 17);
            var fit = LatentTreeFitter.Fit(shape, cols.ToArray(), classes, weights, classPrior, options.Smoothing,
                leafOnly ? 1 : options.LocalMaxIterations, options.LocalTolerance, seed,
                System.Math.Max(1, options.LocalRestarts), token);

            double parameters = 0;
            for (var u = 1; u < shape.Count; u++)
            {
                parameters += (double)shape.States[shape.Parents[u]] * (shape.States[u] - 1);
            }

            return fit.LogLikelihood - 0.5 * System.Math.Log(System.Math.Max(totalWeight, 2.0)) * parameters;
        }

        private void Flatten(LatentGroup group, int parent, List<int> parents, List<int> states, List<int[]> cols)
        {
            var index = parents.Count;
            parents.Add(parent);
            states.Add(group.States);
            cols.Add(group.Feature >= 0 ? columns[group.Feature] : null);
            foreach (var c in group.Children)
            {
                Flatten(c, index, parents, states, cols);
            }
        }

        private List<Tuple<int, int, double>> SpanningTree(int p)
        {
            var mi = new double[p, p];
            for (var i = 0; i < p; i++)
            {
                for (var j = i + 1; j < p; j++)
                {
                    token.ThrowIfCancellationRequested();
                    mi[i, j] = mi[j, i] = ConditionalMutualInformation(i, j);
                }
            }

            var inTree = new bool[p];
            var bestWeight = new double[p];
            var bestFrom = new int[p];
            for (var i = 0; i < p; i++)
            {
                bestWeight[i] = double.NegativeInfinity;
                bestFrom[i] = -1;
            }

            bestWeight[0] = 0;
            var edges = new List<Tuple<int, int, double>>();
            for (var step = 0; step < p; step++)
            {
                var pick = -1;
                for (var i = 0; i < p; i++)
                {
                    if (!inTree[i] && (pick < 0 || bestWeight[i] > bestWeight[pick]))
                    {
                        pick = i;
                    }
                }

                inTree[pick] = true;
                if (bestFrom[pick] >= 0)
                {
                    edges.Add(Tuple.Create(bestFrom[pick], pick, bestWeight[pick]));
                }

                for (var i = 0; i < p; i++)
                {
                    if (!inTree[i] && mi[pick, i] > bestWeight[i])
                    {
                        bestWeight[i] = mi[pick, i];
                        bestFrom[i] = pick;
                    }
                }
            }

            return edges;
        }

        private double ConditionalMutualInformation(int i, int j)
        {
            var si = featureStates[i];
            var sj = featureStates[j];
            var joint = new double[classCount, si, sj];
            var ci = columns[i];
            var cj = columns[j];
            for (var r = 0; r < classes.Length; r++)
            {
                if (ci[r] < 0 || cj[r] < 0)
                {
                    continue;
                }

                joint[classes[r], ci[r], cj[r]] += weights[r];
            }

            double total = 0;
            for (var c = 0; c < classCount; c++)
            {
                for (var a = 0; a < si; a++)
                {
                    for (var b = 0; b < sj; b++)
                    {
                        total += joint[c, a, b];
                    }
                }
            }

            if (!(total > 0))
            {
                return 0;
            }

            double result = 0;
            for (var c = 0; c < classCount; c++)
            {
                var pa = new double[si];
                var pb = new double[sj];
                double nc = 0;
                for (var a = 0; a < si; a++)
                {
                    for (var b = 0; b < sj; b++)
                    {
                        var v = joint[c, a, b];
                        pa[a] += v;
                        pb[b] += v;
                        nc += v;
                    }
                }

                if (!(nc > 0))
                {
                    continue;
                }

                for (var a = 0; a < si; a++)
                {
                    for (var b = 0; b < sj; b++)
                    {
                        var v = joint[c, a, b];
                        if (v > 0)
                        {
                            result += v / total * System.Math.Log(v * nc / (pa[a] * pb[b]));
                        }
                    }
                }
            }

            return System.Math.Max(0, result);
        }
    }
}