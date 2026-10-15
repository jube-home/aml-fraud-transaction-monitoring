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

using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;

namespace Jube.Service.ModelDiagrams.Adapters
{
    internal sealed record GaussianGroup(string Name, double Weight, double[] Means, double[] Deviations);

    internal static class GaussianPanels
    {
        private const int Points = 121;
        private const int MaximumSeries = 8;

        public static DistributionPanels Build(IReadOnlyList<string> features, IReadOnlyList<GaussianGroup> groups,
            bool mixture, ModelDiagramOptions options, string title, string subtitle, string description)
        {
            if (groups.Count == 0 || features.Count == 0)
            {
                throw ModelJson.Inconsistent("there are no distributions to draw");
            }

            foreach (var g in groups)
            {
                if (g.Means.Length != features.Count || g.Deviations.Length != features.Count ||
                    g.Means.Any(m => !double.IsFinite(m)) || g.Deviations.Any(d => !double.IsFinite(d) || d <= 0) ||
                    !double.IsFinite(g.Weight) || g.Weight < 0)
                {
                    throw ModelJson.Inconsistent(
                        "every distribution needs a finite mean and a positive deviation for " +
                        features.Count + " variables");
                }
            }

            options.RequireWithin((long)groups.Count * features.Count, "distributions");
            var room = mixture ? MaximumSeries - 1 : MaximumSeries;

            var shown = groups.Select((g, i) => (Group: g, Index: i)).OrderByDescending(x => x.Group.Weight)
                .ThenBy(x => x.Index).Take(room).OrderBy(x => x.Index).Select(x => x.Group).ToArray();

            var featureOrder = Rank(features.Count, groups).Take(DiagramLimits.MaxPanels).ToArray();

            var panels = new List<DistributionPanel>();

            foreach (var f in featureOrder)
            {
                var low = groups.Min(g => g.Means[f] - 4 * g.Deviations[f]);
                var high = groups.Max(g => g.Means[f] + 4 * g.Deviations[f]);

                if (!(high > low))
                {
                    high = low + 1;
                }

                var xs = Enumerable.Range(0, Points).Select(i => low + (high - low) * i / (Points - 1)).ToArray();
                var series = new List<DistributionSeries>();

                if (mixture)
                {
                    var total = groups.Sum(g => g.Weight);

                    series.Add(new DistributionSeries("Mixture",
                        xs, xs.Select(x => groups.Sum(g => (total > 0 ? g.Weight / total : 1.0 / groups.Count) *
                                                           Density(x, g.Means[f], g.Deviations[f]))).ToArray()));

                    series.AddRange(shown.Select(g => new DistributionSeries(g.Name, xs,
                        xs.Select(x => (total > 0 ? g.Weight / total : 1.0 / groups.Count) *
                                       Density(x, g.Means[f], g.Deviations[f])).ToArray())));
                }
                else
                {
                    series.AddRange(shown.Select(g => new DistributionSeries(g.Name, xs,
                        xs.Select(x => Density(x, g.Means[f], g.Deviations[f])).ToArray())));
                }

                panels.Add(new DistributionPanel(features[f], series));
            }

            var notes = new List<string>();

            if (featureOrder.Length < features.Count)
            {
                notes.Add("the " + featureOrder.Length + " of " + features.Count +
                          " variables that separate the distributions most are shown");
            }

            if (shown.Length < groups.Count)
            {
                notes.Add("the " + shown.Length + " heaviest of " + groups.Count + " components are drawn");
            }

            var suffix = notes.Count == 0 ? "" : "; " + string.Join("; ", notes);

            return new DistributionPanels(new DistributionPanelsSpec
            {
                Options = options.ToChartOptions(title, subtitle + suffix,
                    description + (notes.Count == 0 ? "" : " Of these, " + string.Join(" and ", notes) + ".")),
                Panels = panels
            });
        }

        private static IEnumerable<int> Rank(int count, IReadOnlyList<GaussianGroup> groups)
        {
            if (count <= DiagramLimits.MaxPanels)
            {
                return Enumerable.Range(0, count);
            }

            return Enumerable.Range(0, count).Select(f =>
                {
                    var mean = groups.Average(g => g.Means[f]);
                    var between = groups.Average(g => (g.Means[f] - mean) * (g.Means[f] - mean));
                    var within = groups.Average(g => g.Deviations[f] * g.Deviations[f]);

                    return (Feature: f, Score: within > 0 ? between / within : 0);
                }).OrderByDescending(x => x.Score).ThenBy(x => x.Feature).Take(DiagramLimits.MaxPanels)
                .OrderBy(x => x.Feature).Select(x => x.Feature);
        }

        public static double Density(double x, double mean, double deviation)
        {
            var z = (x - mean) / deviation;

            return Math.Exp(-0.5 * z * z) / (deviation * Math.Sqrt(2 * Math.PI));
        }

        public static double[] Deviations(double[] variances, string what)
        {
            if (variances.Any(v => !double.IsFinite(v) || v <= 0))
            {
                throw ModelJson.Inconsistent(what + " must be positive and finite");
            }

            return variances.Select(Math.Sqrt).ToArray();
        }
    }
}