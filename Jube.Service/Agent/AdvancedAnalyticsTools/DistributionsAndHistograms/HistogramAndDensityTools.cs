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

using System.ComponentModel;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    [AdvancedAnalyticsToolGroup("DistributionsAndHistograms")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class HistogramAndDensityTools
    {
        private const int MaximumValues = 1000000;
        private const int MaximumKernelValues = 100000;

        private const string ValuesText =
            "The observations to summarise, at least one number, no missing values (remove NaN first).";

        private const string ScaleText =
            "What the bar heights mean: Count (default), RelativeFrequency (share of all values), Percent, or Density (height times width sums to 1, comparable with a probability density).";

        private const string CumulativeText =
            "When true the returned values accumulate from the first bin to the last, giving a cumulative histogram that ends at the total (or 1, or 100 percent).";

        [AdvancedAnalyticsTool("HistogramFixedBinCount")]
        [Description(
            "Builds a histogram of a sample with a chosen number of equal width bins spanning the smallest to the largest value. " +
            "It returns the bin edges, centres, widths, raw counts, relative frequencies, densities and the values on the requested scale (optionally cumulative). " +
            "Every bin except the last includes its lower edge and excludes its upper edge; the last bin includes both, so every value is counted once. " +
            "If every value is identical a single bin of width 1 around that value is returned and a note explains it.")]
        public HistogramResult HistogramFixedBinCount(
            [Description(ValuesText)] double[] values,
            [Description("Number of equal width bins, between 1 and 100000.")]
            int binCount,
            [Description(ScaleText)] HistogramScale scale = HistogramScale.Count,
            [Description(CumulativeText)] bool cumulative = false)
        {
            DistributionGuard.Values(values, nameof(values));
            Limit(values, MaximumValues);
            DistributionGuard.PositiveCount(binCount, nameof(binCount));

            return HistogramEngine.FixedBins(values, binCount, scale, cumulative, $"FixedBinCount({binCount})",
                new List<string>());
        }

        [AdvancedAnalyticsTool("HistogramFixedBinWidth")]
        [Description(
            "Builds a histogram of a sample with bins of a chosen width, starting at the smallest value and extending until the largest value is covered (so the last bin may run past the maximum). " +
            "Returns the bin edges, centres, widths, raw counts, relative frequencies, densities and the values on the requested scale (optionally cumulative). " +
            "Use it when the bin width has a natural meaning, such as 10 currency units or 1 hour. " +
            "The width must be positive and small enough to give at most 100000 bins.")]
        public HistogramResult HistogramFixedBinWidth(
            [Description(ValuesText)] double[] values,
            [Description("Width of every bin, in the same units as the values; must be greater than 0.")]
            double binWidth,
            [Description(ScaleText)] HistogramScale scale = HistogramScale.Count,
            [Description(CumulativeText)] bool cumulative = false)
        {
            DistributionGuard.Values(values, nameof(values));
            Limit(values, MaximumValues);

            if (!(binWidth > 0) || !double.IsFinite(binWidth))
            {
                throw new ArgumentException($"binWidth must be a positive number, got {binWidth}.", nameof(binWidth));
            }

            return HistogramEngine.ByWidth(values, binWidth, scale, cumulative, $"FixedBinWidth({binWidth})",
                new List<string>());
        }

        [AdvancedAnalyticsTool("HistogramByBinningRule")]
        [Description(
            "Builds a histogram whose number of equal width bins is chosen automatically by a standard rule, so you do not have to guess. " +
            "Sturges uses 1 plus log base 2 of n and suits roughly normal data of modest size; Rice uses twice the cube root of n; SquareRoot uses the square root of n; " +
            "Scott and FreedmanDiaconis set the bin width from the standard deviation or the interquartile range and the cube root of n (Freedman-Diaconis is robust to outliers); Doane adjusts Sturges for skewed data. " +
            "Returns the same fields as HistogramFixedBinCount; a note says if a rule could not be applied (for example a zero interquartile range) and which fallback was used.")]
        public HistogramResult HistogramByBinningRule(
            [Description(ValuesText)] double[] values,
            [Description(
                "The rule that decides the number of bins: Sturges, Scott, FreedmanDiaconis, Rice, SquareRoot or Doane.")]
            BinningRule rule,
            [Description(ScaleText)] HistogramScale scale = HistogramScale.Count,
            [Description(CumulativeText)] bool cumulative = false)
        {
            DistributionGuard.Values(values, nameof(values));
            Limit(values, MaximumValues);
            var notes = new List<string>();
            var bins = HistogramEngine.BinsForRule(values, rule, notes, out _);

            return HistogramEngine.FixedBins(values, bins, scale, cumulative, $"{rule}({bins} bins)", notes);
        }

        [AdvancedAnalyticsTool("HistogramCustomEdges")]
        [Description(
            "Builds a histogram with bin edges that you choose, which may be unequal, for example 0, 10, 50, 100, 1000 for transaction size bands. " +
            "The edges must be strictly increasing with at least two entries. Every bin includes its lower edge and excludes its upper edge except the last, which includes both. " +
            "Values outside the first and last edge are not binned; outsideCount reports how many. Densities divide by bin width, so unequal bins remain comparable.")]
        public HistogramResult HistogramCustomEdges(
            [Description(ValuesText)] double[] values,
            [Description("The bin edges in strictly increasing order, at least 2 numbers; n edges make n-1 bins.")]
            double[] edges,
            [Description(ScaleText)] HistogramScale scale = HistogramScale.Count,
            [Description(CumulativeText)] bool cumulative = false)
        {
            DistributionGuard.Values(values, nameof(values));
            Limit(values, MaximumValues);
            CheckEdges(edges, nameof(edges));
            var counts = HistogramEngine.CountIntoEdges(values, edges, out var outside);
            var notes = new List<string>();

            if (outside > 0)
            {
                notes.Add($"{outside} value(s) fell outside the edges and were not counted in any bin.");
            }

            return HistogramEngine.Build(values, (double[])edges.Clone(), counts, outside, scale, cumulative,
                $"CustomEdges({edges.Length - 1} bins)", notes);
        }

        [AdvancedAnalyticsTool("SuggestHistogramBinCounts")]
        [Description(
            "Reports how many equal width bins each standard rule would choose for the sample, with the resulting bin width: Sturges, Rice, SquareRoot, Doane, Scott and Freedman-Diaconis. " +
            "Use it to compare the rules before drawing a histogram: a big disagreement usually means outliers or strong skew, where Freedman-Diaconis is the safer choice. " +
            "Fewer than 2 values or identical values give a single bin.")]
        public BinSuggestionResult SuggestHistogramBinCounts(
            [Description(ValuesText)] double[] values)
        {
            DistributionGuard.Values(values, nameof(values));
            Limit(values, MaximumValues);
            var notes = new List<string>();
            var rows = new List<BinSuggestion>();

            foreach (var rule in Enum.GetValues<BinningRule>())
            {
                var bins = HistogramEngine.BinsForRule(values, rule, notes, out var width);
                rows.Add(new BinSuggestion(rule.ToString(), bins, width));
            }

            return new BinSuggestionResult(values.Length, values.Min(), values.Max(), rows.ToArray(), notes.ToArray());
        }

        [AdvancedAnalyticsTool("Histogram2D")]
        [Description(
            "Builds a two dimensional histogram (a grid of counts) of paired observations, showing where two variables occur together. " +
            "The x and y arrays are paired by position and must be the same length. Bins are equal width unless you supply explicit edges. " +
            "It returns the edges on both axes, the count matrix indexed as counts[xBin][yBin], the relative frequencies, and the marginal counts along each axis. " +
            "Pairs outside supplied edges are counted in outsideCount only.")]
        public Histogram2DResult Histogram2D(
            [Description("First variable, one number per pair, no missing values.")]
            double[] x,
            [Description("Second variable, same length and order as x, no missing values.")]
            double[] y,
            [Description(
                "Number of equal width bins along x, between 1 and 1000. Default 10. Ignored when xEdges is given.")]
            int xBins = 10,
            [Description(
                "Number of equal width bins along y, between 1 and 1000. Default 10. Ignored when yEdges is given.")]
            int yBins = 10,
            [Description("Optional explicit x bin edges, strictly increasing; leave empty to use xBins.")]
            double[]? xEdges = null,
            [Description("Optional explicit y bin edges, strictly increasing; leave empty to use yBins.")]
            double[]? yEdges = null)
        {
            DistributionGuard.Values(x, nameof(x));
            DistributionGuard.Values(y, nameof(y));
            Limit(x, MaximumValues);

            if (x.Length != y.Length)
            {
                throw new ArgumentException(
                    $"x has {x.Length} values but y has {y.Length}; they must be paired one to one.", nameof(y));
            }

            var ex = xEdges is { Length: > 0 } ? xEdges : UniformEdges(x, xBins, nameof(xBins));
            var ey = yEdges is { Length: > 0 } ? yEdges : UniformEdges(y, yBins, nameof(yBins));
            CheckEdges(ex, nameof(xEdges));
            CheckEdges(ey, nameof(yEdges));

            var counts = new int[ex.Length - 1][];

            for (var i = 0; i < counts.Length; i++)
            {
                counts[i] = new int[ey.Length - 1];
            }

            var outside = 0;

            for (var i = 0; i < x.Length; i++)
            {
                var bx = BinIndex(ex, x[i]);
                var by = BinIndex(ey, y[i]);

                if (bx < 0 || by < 0)
                {
                    outside++;
                    continue;
                }

                counts[bx][by]++;
            }

            var relative = counts.Select(r => r.Select(c => (double)c / x.Length).ToArray()).ToArray();
            var xm = counts.Select(r => r.Sum()).ToArray();
            var ym = Enumerable.Range(0, ey.Length - 1).Select(j => counts.Sum(r => r[j])).ToArray();

            return new Histogram2DResult(x.Length, ex, ey, counts, relative, xm, ym, outside);
        }

        [AdvancedAnalyticsTool("EmpiricalCumulativeDistribution")]
        [Description(
            "Evaluates the empirical cumulative distribution function of a sample at the points you give: the fraction of the sample less than or equal to each point (or, with upperTail true, strictly greater). " +
            "It is the assumption-free version of a percentile rank. Use it to say what share of history a new value exceeds. " +
            "Points below the smallest value give 0 and points at or above the largest give 1.")]
        public EmpiricalCdfResult EmpiricalCumulativeDistribution(
            [Description(ValuesText)] double[] values,
            [Description("The points at which to evaluate the function, any real numbers.")]
            double[] points,
            [Description(
                "When true returns the share strictly above each point (the empirical survival function) instead. Default false.")]
            bool upperTail = false)
        {
            DistributionGuard.Values(values, nameof(values));
            Limit(values, MaximumKernelValues);
            DistributionGuard.Points(points, nameof(points));

            var sorted = (double[])values.Clone();
            Array.Sort(sorted);
            var result = new double[points.Length];

            for (var i = 0; i < points.Length; i++)
            {
                var atMost = CountAtMost(sorted, points[i]);
                var p = (double)atMost / sorted.Length;
                result[i] = upperTail ? 1.0 - p : p;
            }

            return new EmpiricalCdfResult(values.Length, upperTail, points, result);
        }

        [AdvancedAnalyticsTool("EmpiricalCumulativeDistributionSteps")]
        [Description(
            "Returns the full empirical cumulative distribution of a sample as its steps: each distinct value in ascending order, how many times it occurs, the running count, the cumulative probability (share at or below) and the survival probability (share strictly above). " +
            "Use it to plot the ECDF or to read exact percentile ranks for every observed value. " +
            "Repeated values appear once with their count.")]
        public EmpiricalCdfStepsResult EmpiricalCumulativeDistributionSteps(
            [Description(ValuesText)] double[] values)
        {
            DistributionGuard.Values(values, nameof(values));
            Limit(values, MaximumValues);

            var groups = values.GroupBy(v => v).OrderBy(g => g.Key).ToArray();
            var distinct = groups.Select(g => g.Key).ToArray();
            var counts = groups.Select(g => g.Count()).ToArray();
            var cumulative = new int[counts.Length];
            var running = 0;

            for (var i = 0; i < counts.Length; i++)
            {
                running += counts[i];
                cumulative[i] = running;
            }

            var probabilities = cumulative.Select(c => (double)c / values.Length).ToArray();

            return new EmpiricalCdfStepsResult(values.Length, distinct, counts, cumulative, probabilities,
                probabilities.Select(p => 1.0 - p).ToArray());
        }

        [AdvancedAnalyticsTool("EmpiricalQuantileFunction")]
        [Description(
            "Evaluates the sample quantile function: for each probability, the value below which that fraction of the sample lies (the inverse of the empirical cumulative distribution). " +
            "Nine standard definitions are offered because they differ for small samples: Type7 (default) is the R and Excel PERCENTILE.INC style linear interpolation, Type6 is the Excel PERCENTILE.EXC style, Type1 is the plain inverse of the step function, and Type8 is the recommended median unbiased choice. " +
            "Probabilities must be between 0 and 1.")]
        public EmpiricalQuantileResult EmpiricalQuantileFunction(
            [Description(ValuesText)] double[] values,
            [Description(
                "Probabilities between 0 and 1 inclusive, for example 0.5 for the median and 0.99 for the 99th percentile.")]
            double[] probabilities,
            [Description(
                "Which of the nine standard sample quantile definitions to use, Type1 to Type9. Default Type7.")]
            QuantileDefinition definition = QuantileDefinition.Type7)
        {
            DistributionGuard.Values(values, nameof(values));
            Limit(values, MaximumValues);
            DistributionGuard.Probabilities(probabilities, nameof(probabilities));
            var quantiles = HistogramEngine.Quantiles(values, probabilities, definition);

            return new EmpiricalQuantileResult(values.Length, definition.ToString(), probabilities, quantiles);
        }

        [AdvancedAnalyticsTool("KernelDensityEstimate")]
        [Description(
            "Estimates the probability density of a sample at the points you give by kernel density estimation, a smooth alternative to a histogram. " +
            "Each observation contributes a small bump (Gaussian, Epanechnikov or Uniform kernel) whose width is the bandwidth; larger bandwidths are smoother. " +
            "The bandwidth is chosen by a rule (NormalReference is the standard 1.06 sigma n to the minus one fifth rule, SilvermanRobust also uses the interquartile range and suits skewed data, Oversmoothed is the widest reasonable choice) or given manually. " +
            "Returns the bandwidth used and the density at each point; the density integrates to 1 but is not a probability at a point.")]
        public KernelDensityResult KernelDensityEstimate(
            [Description(ValuesText)] double[] values,
            [Description("The points at which to evaluate the density, any real numbers.")]
            double[] points,
            [Description(
                "The kernel shape: Gaussian (default, smooth and unbounded), Epanechnikov (finite width, most efficient) or Uniform (box).")]
            KernelKind kernel = KernelKind.Gaussian,
            [Description(
                "How the bandwidth is chosen: NormalReference (default), SilvermanRobust, Oversmoothed, or Manual to use the bandwidth argument.")]
            BandwidthRule bandwidthRule = BandwidthRule.NormalReference,
            [Description(
                "The bandwidth to use when bandwidthRule is Manual, in the units of the values; ignored otherwise.")]
            double? bandwidth = null)
        {
            DistributionGuard.Values(values, nameof(values));
            Limit(values, MaximumKernelValues);
            DistributionGuard.Points(points, nameof(points));
            var h = KernelDensityEngine.Bandwidth(values, bandwidthRule, bandwidth);
            var density = KernelDensityEngine.Evaluate(values, points, kernel, h);

            return new KernelDensityResult(values.Length, kernel.ToString(), bandwidthRule.ToString(), h, points,
                density);
        }

        [AdvancedAnalyticsTool("KernelDensityCurve")]
        [Description(
            "Computes a smooth kernel density curve of a sample over an evenly spaced grid and finds its peaks, which is how to see whether the data has one mode or several. " +
            "The grid runs from the smallest value minus padding bandwidths to the largest value plus padding bandwidths. " +
            "Returns the grid, the density on it, the locations and heights of local maxima (peakLocations and peakDensities), and modeCount. " +
            "More than one peak suggests a mixture of groups; tiny peaks in noisy data disappear with a larger bandwidth, so check with a larger manual bandwidth or the Oversmoothed rule.")]
        public KernelDensityCurveResult KernelDensityCurve(
            [Description(ValuesText)] double[] values,
            [Description("Number of grid points, between 3 and 5000. Default 200.")]
            int gridCount = 200,
            [Description("The kernel shape: Gaussian (default), Epanechnikov or Uniform.")]
            KernelKind kernel = KernelKind.Gaussian,
            [Description(
                "How the bandwidth is chosen: NormalReference (default), SilvermanRobust, Oversmoothed, or Manual.")]
            BandwidthRule bandwidthRule = BandwidthRule.NormalReference,
            [Description("The bandwidth to use when bandwidthRule is Manual; ignored otherwise.")]
            double? bandwidth = null,
            [Description(
                "How many bandwidths to extend the grid beyond the smallest and largest value, 0 to 10. Default 3.")]
            double padding = 3.0)
        {
            DistributionGuard.Values(values, nameof(values));
            Limit(values, MaximumKernelValues);

            if (gridCount < 3 || gridCount > 5000)
            {
                throw new ArgumentException($"gridCount must be between 3 and 5000, got {gridCount}.",
                    nameof(gridCount));
            }

            if (!(padding >= 0 && padding <= 10))
            {
                throw new ArgumentException($"padding must be between 0 and 10 bandwidths, got {padding}.",
                    nameof(padding));
            }

            var h = KernelDensityEngine.Bandwidth(values, bandwidthRule, bandwidth);
            var lo = values.Min() - padding * h;
            var hi = values.Max() + padding * h;
            var x = new double[gridCount];

            for (var i = 0; i < gridCount; i++)
            {
                x[i] = lo + (hi - lo) * i / (gridCount - 1);
            }

            var density = KernelDensityEngine.Evaluate(values, x, kernel, h);
            var (locations, heights) = KernelDensityEngine.Peaks(x, density);
            var notes = new List<string>();

            if (locations.Length > 1)
            {
                notes.Add(
                    "More than one peak was found: the data may be a mixture of groups. Confirm with a larger bandwidth.");
            }

            return new KernelDensityCurveResult(values.Length, kernel.ToString(), bandwidthRule.ToString(), h, x,
                density,
                locations, heights, locations.Length, notes.ToArray());
        }

        [AdvancedAnalyticsTool("KernelBandwidthRules")]
        [Description(
            "Reports the kernel density bandwidth each automatic rule would choose for a sample, together with the standard deviation and interquartile range they are based on: NormalReference (1.06 sigma n to the minus one fifth), SilvermanRobust (0.9 times the smaller of sigma and IQR over 1.34, times n to the minus one fifth) and Oversmoothed (1.144 sigma n to the minus one fifth). " +
            "Use it to pick a bandwidth for KernelDensityEstimate. " +
            "A rule that cannot be computed, for example because all values are identical, is omitted and a note says why.")]
        public KernelBandwidthResult KernelBandwidthRules(
            [Description(ValuesText)] double[] values)
        {
            DistributionGuard.Values(values, nameof(values));
            Limit(values, MaximumKernelValues);
            var notes = new List<string>();
            var estimates = new List<BandwidthEstimate>();

            foreach (var rule in new[]
                         { BandwidthRule.NormalReference, BandwidthRule.SilvermanRobust, BandwidthRule.Oversmoothed })
            {
                try
                {
                    estimates.Add(new BandwidthEstimate(rule.ToString(),
                        KernelDensityEngine.Bandwidth(values, rule, null)));
                }
                catch (ArgumentException ex)
                {
                    notes.Add($"{rule}: {ex.Message}");
                }
            }

            var sd = values.Length > 1 ? HistogramEngine.StandardDeviation(values) : 0.0;

            return new KernelBandwidthResult(values.Length, sd, HistogramEngine.InterquartileRange(values),
                estimates.ToArray(), notes.ToArray());
        }

        private static void Limit(double[] values, int maximum)
        {
            if (values.Length > maximum)
            {
                throw new ArgumentException(
                    $"values has {values.Length} entries; this tool accepts at most {maximum}. Sample the data down first.",
                    nameof(values));
            }
        }

        private static void CheckEdges(double[]? edges, string name)
        {
            if (edges == null || edges.Length < 2)
            {
                throw new ArgumentException($"{name} needs at least 2 numbers (n edges make n-1 bins).", name);
            }

            for (var i = 0; i < edges.Length; i++)
            {
                if (!double.IsFinite(edges[i]))
                {
                    throw new ArgumentException($"{name}[{i}] must be a finite number.", name);
                }

                if (i > 0 && edges[i] <= edges[i - 1])
                {
                    throw new ArgumentException(
                        $"{name} must be strictly increasing but {name}[{i}] = {edges[i]} is not above {name}[{i - 1}] = {edges[i - 1]}.",
                        name);
                }
            }
        }

        private static double[] UniformEdges(double[] values, int bins, string name)
        {
            DistributionGuard.PositiveCount(bins, name, 1000);
            var min = values.Min();
            var max = values.Max();

            if (max - min <= 0)
            {
                return new[] { min - 0.5, min + 0.5 };
            }

            var edges = new double[bins + 1];

            for (var i = 0; i <= bins; i++)
            {
                edges[i] = min + (max - min) * i / bins;
            }

            edges[bins] = max;

            return edges;
        }

        private static int BinIndex(double[] edges, double v)
        {
            if (v < edges[0] || v > edges[^1])
            {
                return -1;
            }

            var index = Array.BinarySearch(edges, v);

            if (index < 0)
            {
                index = ~index - 1;
            }

            return Math.Min(index, edges.Length - 2);
        }

        private static int CountAtMost(double[] sorted, double point)
        {
            var lo = 0;
            var hi = sorted.Length;

            while (lo < hi)
            {
                var mid = (lo + hi) / 2;

                if (sorted[mid] <= point)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo;
        }
    }
}