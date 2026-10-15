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

using Jube.Charting;
using Jube.Charting.Charts;
using Jube.Charting.Core.Models;

namespace Jube.Service.ModelDiagrams.Adapters
{
    internal sealed record ProjectedGroup(string Name, IReadOnlyList<int> Rows);

    internal sealed record ProjectionAxes(double[] First, double[] Second, double FirstShare, double SecondShare);

    internal static class SupportSetProjection
    {
        public const int MaximumPoints = 400;

        public static ScatterChart Build(double[][] rows, IReadOnlyList<string> names,
            IReadOnlyList<ProjectedGroup> groups,
            Func<int, string> label, ModelDiagramOptions options, string title,
            string subtitle, string description)
        {
            var dimension = names.Count;

            if (rows.Length == 0)
            {
                throw ModelJson.Inconsistent("the model stores no rows");
            }

            if (rows.Any(r => r.Length != dimension || r.Any(v => !double.IsFinite(v))))
            {
                throw ModelJson.Inconsistent("every stored row must hold " + dimension + " finite values");
            }

            options.RequireWithin(dimension * (long)dimension, "covariance cells");
            var axes = Axes(rows, dimension);
            var centre = Enumerable.Range(0, dimension).Select(f => rows.Average(r => r[f])).ToArray();
            var stride = Math.Max(1, (int)Math.Ceiling(rows.Length / (double)MaximumPoints));
            var builder = Chart.Scatter();

            foreach (var group in groups)
            {
                var shown = group.Rows.Where(i => i % stride == 0).ToArray();

                if (shown.Length == 0)
                {
                    continue;
                }

                var points = shown.Select(i => new DataPoint(Project(rows[i], centre, axes.First),
                    Project(rows[i], centre, axes.Second), label(i))).ToArray();

                builder.Series(group.Name, points);
            }

            var drawn = groups.Sum(g => g.Rows.Count(i => i % stride == 0));

            var sampled = stride > 1
                ? "; every " + Ordinal(stride) + " row drawn, " + drawn + " of " + rows.Length
                : "";

            var spec = builder.Build().Spec;

            return new ScatterChart(spec with
            {
                Options = options.ToChartOptions(title, subtitle + sampled,
                        description + " The axes are the two principal components of the stored rows: " +
                        Loadings("the first", axes.First, names) + "; " + Loadings("the second", axes.Second, names) +
                        ".")
                    with
                    {
                        XAxisTitle = "Principal component 1 (" + Percent(axes.FirstShare) + " of variance)",
                        YAxisTitle = "Principal component 2 (" + Percent(axes.SecondShare) + " of variance)"
                    }
            });
        }

        public static ProjectionAxes Axes(double[][] rows, int dimension)
        {
            var centre = Enumerable.Range(0, dimension).Select(f => rows.Average(r => r[f])).ToArray();
            var covariance = new double[dimension][];

            for (var i = 0; i < dimension; i++)
            {
                covariance[i] = new double[dimension];

                for (var j = 0; j <= i; j++)
                {
                    var sum = 0d;

                    foreach (var r in rows)
                    {
                        sum += (r[i] - centre[i]) * (r[j] - centre[j]);
                    }

                    covariance[i][j] = sum / Math.Max(1, rows.Length - 1);
                }
            }

            for (var i = 0; i < dimension; i++)
            {
                for (var j = i + 1; j < dimension; j++)
                {
                    covariance[i][j] = covariance[j][i];
                }
            }

            var total = Enumerable.Range(0, dimension).Sum(i => covariance[i][i]);
            var (first, firstValue) = Leading(covariance, dimension, null);

            var (second, secondValue) =
                dimension > 1 ? Leading(covariance, dimension, first) : (new double[dimension], 0);

            return new ProjectionAxes(first, second, total > 0 ? firstValue / total : 0,
                total > 0 ? secondValue / total : 0);
        }

        private static (double[] Vector, double Value) Leading(double[][] covariance, int dimension, double[]? deflate)
        {
            var matrix = covariance.Select(r => r.ToArray()).ToArray();

            if (deflate is not null)
            {
                var value = Rayleigh(covariance, deflate);

                for (var i = 0; i < dimension; i++)
                {
                    for (var j = 0; j < dimension; j++)
                    {
                        matrix[i][j] -= value * deflate[i] * deflate[j];
                    }
                }
            }

            var vector = Enumerable.Range(0, dimension).Select(i => 1.0 + 0.01 * i).ToArray();
            Normalise(vector);

            for (var iteration = 0; iteration < 500; iteration++)
            {
                var next = new double[dimension];

                for (var i = 0; i < dimension; i++)
                {
                    for (var j = 0; j < dimension; j++)
                    {
                        next[i] += matrix[i][j] * vector[j];
                    }
                }

                if (deflate is not null)
                {
                    var overlap = next.Select((v, i) => v * deflate[i]).Sum();

                    for (var i = 0; i < dimension; i++)
                    {
                        next[i] -= overlap * deflate[i];
                    }
                }

                if (!Normalise(next))
                {
                    break;
                }

                var change = next.Select((v, i) => Math.Abs(v - vector[i])).Max();
                vector = next;

                if (change < 1e-10)
                {
                    break;
                }
            }

            var largest = vector.Select((v, i) => (Value: v, Index: i)).OrderByDescending(x => Math.Abs(x.Value))
                .ThenBy(x => x.Index).First();

            if (largest.Value < 0)
            {
                vector = vector.Select(v => -v).ToArray();
            }

            return (vector, Math.Max(0, Rayleigh(covariance, vector)));
        }

        private static double Rayleigh(double[][] matrix, double[] vector)
        {
            var sum = 0d;

            for (var i = 0; i < vector.Length; i++)
            {
                for (var j = 0; j < vector.Length; j++)
                {
                    sum += vector[i] * matrix[i][j] * vector[j];
                }
            }

            return sum;
        }

        private static bool Normalise(double[] vector)
        {
            var norm = Math.Sqrt(vector.Sum(v => v * v));

            if (!(norm > 1e-300) || !double.IsFinite(norm))
            {
                return false;
            }

            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }

            return true;
        }

        private static double Project(double[] row, double[] centre, double[] axis)
        {
            var sum = 0d;

            for (var i = 0; i < row.Length; i++)
            {
                sum += (row[i] - centre[i]) * axis[i];
            }

            return sum;
        }

        private static string Loadings(string which, double[] axis, IReadOnlyList<string> names)
        {
            var top = axis.Select((v, i) => (Value: v, Index: i)).Where(x => Math.Abs(x.Value) > 1e-9)
                .OrderByDescending(x => Math.Abs(x.Value)).ThenBy(x => x.Index).Take(3)
                .Select(x => names[x.Index] + " " + (x.Value >= 0 ? "+" : "") + CoefficientMapping.Short(x.Value))
                .ToArray();

            return which + " is led by " + (top.Length == 0 ? "no variable" : string.Join(", ", top));
        }

        private static string Percent(double share)
        {
            return ModelJson.Format(Math.Round(share * 100, 1)) + "%";
        }

        private static string Ordinal(int n)
        {
            var suffix = n % 100 is 11 or 12 or 13
                ? "th"
                : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };

            return n + suffix;
        }

        public static string Kernel(string? kernel, double? gamma, int? degree)
        {
            var name = kernel is { Length: > 0 and <= 24 } k && k.All(char.IsAsciiLetterOrDigit) ? k : "custom";
            var parts = new List<string> { name + " kernel" };

            if (gamma is { } g && double.IsFinite(g))
            {
                parts.Add("gamma " + ModelJson.Format(g));
            }

            if (degree is { } d && name.Contains("Poly", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add("degree " + d);
            }

            return string.Join(", ", parts);
        }
    }
}