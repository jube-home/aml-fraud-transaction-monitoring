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

using Jube.Charting.Text;
using Jube.Charting.Ticks;
using Jube.Charting.Diagrams.Models;

namespace Jube.Charting.Diagrams
{
    internal static class TreeText
    {
        public static string Condition(TreeNode n)
        {
            if (!string.IsNullOrWhiteSpace(n.Condition))
            {
                return n.Condition!;
            }

            var feature = string.IsNullOrWhiteSpace(n.Feature) ? "split" : n.Feature!;

            if (n.Categories is { Count: > 0 })
            {
                var shown = n.Categories.Take(4).Select(c => TextMetrics.Truncate(c, 60, 11));

                return feature + " in {" + string.Join(", ", shown) +
                       (n.Categories.Count > 4 ? ", +" + (n.Categories.Count - 4) : string.Empty) + "}";
            }

            if (n.Threshold is { } t)
            {
                return feature + " ≤ " + NumberFormat.Value(t);
            }

            return feature;
        }

        public static int Majority(TreeNode n)
        {
            if (n.ClassCounts is not { Count: > 0 })
            {
                return 0;
            }

            var best = 0;

            for (var i = 1; i < n.ClassCounts.Count; i++)
            {
                if (n.ClassCounts[i] > n.ClassCounts[best])
                {
                    best = i;
                }
            }

            return best;
        }

        public static string Prediction(TreeNode n, TreeDiagramSpec spec)
        {
            if (!string.IsNullOrWhiteSpace(n.Prediction))
            {
                return n.Prediction!;
            }

            if (n.ClassCounts is { Count: > 0 })
            {
                var m = Majority(n);
                var name = m < spec.ClassNames.Count ? spec.ClassNames[m] : "class " + m;
                var total = n.ClassCounts.Sum();

                return total > 0 ? name + " " + NumberFormat.Percent(n.ClassCounts[m] / total, 0) : name;
            }

            return n.Value is { } v ? spec.ValueName + " " + NumberFormat.Value(v) : "leaf";
        }

        public static string Stats(TreeNode n, TreeDiagramSpec spec)
        {
            var parts = new List<string>();

            if (n.Samples is { } s)
            {
                parts.Add("n = " + NumberFormat.Value(s));
            }

            if (n.Impurity is { } i)
            {
                parts.Add(spec.ImpurityName + " " + NumberFormat.Value(i));
            }

            return string.Join(" · ", parts);
        }
    }
}