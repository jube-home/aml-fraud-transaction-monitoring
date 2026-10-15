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
using Accord.Math;
using Accord.Math.Optimization;
using Jube.Analytics;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling
{
    public enum TableScalingMethod
    {
        Standard = 0,
        MinMax = 1,
        RobustMad = 2,
        RobustIqr = 3,
        MaxAbs = 4
    }

    public enum RowNorm
    {
        L1 = 0,
        L2 = 1,
        Max = 2
    }

    public enum QuantileOutputDistribution
    {
        Uniform = 0,
        Normal = 1
    }

    public enum PowerTransformMethod
    {
        BoxCox = 0,
        YeoJohnson = 1
    }

    public enum ColumnFunction
    {
        NaturalLog = 0,
        Log10 = 1,
        Log2 = 2,
        Log1P = 3,
        SignedLog1P = 4,
        Sqrt = 5,
        SignedSqrt = 6,
        CubeRoot = 7,
        Reciprocal = 8,
        Square = 9,
        AbsoluteValue = 10
    }

    public enum OutlierBoundsMethod
    {
        Quantiles = 0,
        IqrFences = 1,
        StandardDeviations = 2,
        MedianAbsoluteDeviations = 3
    }

    [AdvancedAnalyticsToolGroup("DataPreparationAndResampling")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class ScalingTools
    {
        [AdvancedAnalyticsTool("FitTableScaler")]
        [Description(
            "Learns a per column rescaling from the rows given and returns the rescaled table together with the parameters (Centers and Scales) so ApplyTableScaler can transform new data identically. " +
            "Each cell becomes (value - center) / scale. Standard uses mean and sample standard deviation (z-score); MinMax uses the minimum and range so values land in 0..1; RobustMad uses the median and 1.4826 times the median absolute deviation and shrugs off outliers; RobustIqr uses the median and the interquartile range; MaxAbs uses 0 and the largest absolute value so signs and zeros are kept. " +
            "A constant column has scale 0 and is mapped to 0. Missing cells (NaN or infinite) are ignored when fitting and come back as NaN. A column with no finite value is rejected.")]
        public ScalingResult FitTableScaler(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Standard, MinMax, RobustMad, RobustIqr or MaxAbs. Default Standard.")]
            TableScalingMethod method = TableScalingMethod.Standard)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            var centers = new double[columns];
            var scales = new double[columns];

            for (var j = 0; j < columns; j++)
            {
                var finite = DataPreparationGuard.FiniteColumn(data, j);

                if (finite.Length == 0)
                {
                    throw new ArgumentException(
                        $"Column '{names[j]}' has no finite value to fit a scaler on. Impute or drop it first.",
                        nameof(data));
                }

                var sorted = finite.OrderBy(v => v).ToArray();

                switch (method)
                {
                    case TableScalingMethod.Standard:
                        var z = Standardisation.Fit(finite);
                        centers[j] = z.Mean;
                        scales[j] = z.StandardDeviation;
                        break;
                    case TableScalingMethod.MinMax:
                        centers[j] = sorted[0];
                        scales[j] = sorted[^1] - sorted[0];
                        break;
                    case TableScalingMethod.RobustMad:
                        var robust = RobustScaler.Fit(finite);
                        centers[j] = robust.Median;
                        scales[j] = robust.Scale;
                        break;
                    case TableScalingMethod.RobustIqr:
                        centers[j] = DataPreparationGuard.Quantile(sorted, 0.5);

                        scales[j] = DataPreparationGuard.Quantile(sorted, 0.75) -
                                    DataPreparationGuard.Quantile(sorted, 0.25);

                        break;
                    default:
                        centers[j] = 0;
                        scales[j] = Math.Max(Math.Abs(sorted[0]), Math.Abs(sorted[^1]));
                        break;
                }
            }

            var table = Scale(data, centers, scales);

            return new ScalingResult(method, names, centers, scales, table,
                names.Where((_, j) => scales[j] == 0 || !double.IsFinite(scales[j])).ToArray());
        }

        [AdvancedAnalyticsTool("ApplyTableScaler")]
        [Description(
            "Applies scaling parameters learned by FitTableScaler to a table: each cell becomes (value - center) / scale, and a column whose scale is 0 becomes 0. " +
            "Use it to transform validation, test or live rows with the parameters learned from training rows only, which avoids leakage. Missing cells stay NaN.")]
        public double[][] ApplyTableScaler(
            [Description(
                "Table rows to scale: one array of numbers per case, in the same column order used for fitting.")]
            double[][] data,
            [Description("The Centers returned by FitTableScaler, one per column.")]
            double[] centers,
            [Description("The Scales returned by FitTableScaler, one per column.")]
            double[] scales)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            ValidateParameters(columns, centers, scales);

            return Scale(data, centers, scales);
        }

        [AdvancedAnalyticsTool("InvertTableScaler")]
        [Description(
            "Reverses a scaling: each cell becomes value * scale + center, returning scaled values to their original units using the Centers and Scales from FitTableScaler. " +
            "Use it to report model outputs, thresholds or cluster centres in original units. Columns with scale 0 return their centre. Missing cells stay NaN.")]
        public double[][] InvertTableScaler(
            [Description(
                "Scaled table rows: one array of numbers per case, in the same column order used for fitting.")]
            double[][] data,
            [Description("The Centers returned by FitTableScaler, one per column.")]
            double[] centers,
            [Description("The Scales returned by FitTableScaler, one per column.")]
            double[] scales)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            ValidateParameters(columns, centers, scales);

            return DataPreparationGuard.Build(data.Length, columns,
                (i, j) => DataPreparationGuard.IsMissing(data[i][j])
                    ? double.NaN
                    : data[i][j] * scales[j] + centers[j]);
        }

        [AdvancedAnalyticsTool("NormaliseRowsToUnitNorm")]
        [Description(
            "Rescales each row (not each column) so its length is 1 under the chosen norm: L1 divides by the sum of absolute values, L2 by the Euclidean length, Max by the largest absolute value. " +
            "Use it for profile or spend-mix style features where the shape of a row matters and not its size. " +
            "Returns the normalised rows and each row's original norm; a row whose norm is 0 is left as zeros. Missing cells are excluded from the norm and stay NaN.")]
        public RowNormalisationResult NormaliseRowsToUnitNorm(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description("L1, L2 or Max. Default L2.")]
            RowNorm norm = RowNorm.L2)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var norms = new double[data.Length];
            var table = new double[data.Length][];

            for (var i = 0; i < data.Length; i++)
            {
                var finite = data[i].Where(v => !DataPreparationGuard.IsMissing(v)).ToArray();

                norms[i] = norm switch
                {
                    RowNorm.L1 => finite.Sum(Math.Abs),
                    RowNorm.L2 => Math.Sqrt(finite.Sum(v => v * v)),
                    _ => finite.Length == 0 ? 0 : finite.Max(Math.Abs)
                };

                table[i] = new double[columns];

                for (var j = 0; j < columns; j++)
                {
                    table[i][j] = DataPreparationGuard.IsMissing(data[i][j])
                        ? double.NaN
                        : norms[i] == 0
                            ? 0
                            : data[i][j] / norms[i];
                }
            }

            return new RowNormalisationResult(table, norms);
        }

        [AdvancedAnalyticsTool("FitQuantileTransformer")]
        [Description(
            "Learns a monotone mapping that turns each column into a uniform 0..1 distribution (or a standard normal) using the empirical distribution of the supplied rows, and returns the transformed table with the reference quantiles to reuse in ApplyQuantileTransformer. " +
            "It is robust to outliers and heavy tails because only the rank of a value matters. Values beyond the training range map to the ends. Ties share the midpoint rank. " +
            "Missing cells are ignored when fitting and stay NaN. A column with no finite value is rejected.")]
        public QuantileTransformResult FitQuantileTransformer(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Uniform gives values in 0..1; Normal maps those to standard normal scores. Default Uniform.")]
            QuantileOutputDistribution output = QuantileOutputDistribution.Uniform,
            [Description(
                "Number of reference quantiles kept per column, at least 2; capped at the number of finite values. Default 1000.")]
            int quantileCount = 1000)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            if (quantileCount < 2)
            {
                throw new ArgumentException("quantileCount must be at least 2.", nameof(quantileCount));
            }

            var references = new double[columns][];

            for (var j = 0; j < columns; j++)
            {
                var finite = DataPreparationGuard.FiniteColumn(data, j);

                if (finite.Length == 0)
                {
                    throw new ArgumentException($"Column '{names[j]}' has no finite value to fit on.", nameof(data));
                }

                var sorted = finite.OrderBy(v => v).ToArray();
                var count = Math.Min(quantileCount, Math.Max(sorted.Length, 2));

                references[j] = Enumerable.Range(0, count)
                    .Select(q => DataPreparationGuard.Quantile(sorted, q / (double)(count - 1)))
                    .ToArray();
            }

            return new QuantileTransformResult(output, names, references, TransformQuantiles(data, references, output));
        }

        [AdvancedAnalyticsTool("ApplyQuantileTransformer")]
        [Description(
            "Applies the reference quantiles learned by FitQuantileTransformer to new rows, mapping each value to its position in the training distribution as uniform 0..1 or standard normal. " +
            "Values outside the training range map to the ends (0 and 1, or about -5.2 and 5.2 for Normal). Missing cells stay NaN.")]
        public double[][] ApplyQuantileTransformer(
            [Description(
                "Table rows to transform: one array of numbers per case, in the same column order used for fitting.")]
            double[][] data,
            [Description("The ReferenceQuantiles returned by FitQuantileTransformer: one ascending array per column.")]
            double[][] referenceQuantiles,
            [Description("Uniform or Normal; must match the choice made when fitting. Default Uniform.")]
            QuantileOutputDistribution output = QuantileOutputDistribution.Uniform)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));

            if (referenceQuantiles == null || referenceQuantiles.Length != columns ||
                referenceQuantiles.Any(r => r == null || r.Length < 2))
            {
                throw new ArgumentException(
                    $"Give one array of at least 2 reference quantiles per column: the table has {columns} columns.",
                    nameof(referenceQuantiles));
            }

            return TransformQuantiles(data, referenceQuantiles, output);
        }

        [AdvancedAnalyticsTool("FitPowerTransformer")]
        [Description(
            "Learns a Box-Cox or Yeo-Johnson power transform per column that makes a skewed distribution as close to normal as possible, choosing each lambda by maximising the normal log-likelihood. " +
            "Returns the transformed table and the fitted lambdas for ApplyPowerTransformer. Lambda near 1 means little change, near 0 is a log, near 0.5 a square root, negative is a reciprocal-like transform. " +
            "Box-Cox needs every finite value to be strictly positive; Yeo-Johnson accepts zeros and negatives. Missing cells are ignored when fitting and stay NaN. A column with fewer than 3 finite values is rejected; a constant column gets lambda 1.")]
        public PowerTransformResult FitPowerTransformer(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("BoxCox (positive data only) or YeoJohnson (any real data). Default YeoJohnson.")]
            PowerTransformMethod method = PowerTransformMethod.YeoJohnson,
            [Description(
                "Lower and upper limit of the lambda search as two numbers, for example [-5, 5]. Leave empty for [-5, 5].")]
            double[]? lambdaRange = null)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            double[] range = [-5d, 5d];

            if (lambdaRange is { Length: > 0 })
            {
                if (lambdaRange.Length != 2 || !(lambdaRange[0] < lambdaRange[1]))
                {
                    throw new ArgumentException("lambdaRange must be two numbers, lower then upper, lower below upper.",
                        nameof(lambdaRange));
                }

                range = lambdaRange;
            }

            var lambdas = new double[columns];

            for (var j = 0; j < columns; j++)
            {
                var finite = DataPreparationGuard.FiniteColumn(data, j);

                if (finite.Length < 3)
                {
                    throw new ArgumentException(
                        $"Column '{names[j]}' has {finite.Length} finite value(s); at least 3 are needed to fit a power transform.",
                        nameof(data));
                }

                if (method == PowerTransformMethod.BoxCox && finite.Any(v => v <= 0))
                {
                    throw new ArgumentException(
                        $"Column '{names[j]}' has zero or negative values, which Box-Cox cannot handle. Use YeoJohnson or shift the column to be positive.",
                        nameof(data));
                }

                lambdas[j] = finite.Max() - finite.Min() == 0
                    ? 1
                    : BrentSearch.Maximize(l => LogLikelihood(finite, l, method), range[0], range[1], 1e-9);
            }

            return new PowerTransformResult(method, names, lambdas, PowerApply(data, lambdas, method));
        }

        [AdvancedAnalyticsTool("ApplyPowerTransformer")]
        [Description(
            "Applies fixed lambdas from FitPowerTransformer to a table using the Box-Cox or Yeo-Johnson formula, one lambda per column. " +
            "Box-Cox is (x^lambda - 1) / lambda and ln(x) when lambda is 0; Yeo-Johnson extends it to zero and negative values. " +
            "Cells that the method cannot handle (Box-Cox with x at or below 0) and missing cells come back as NaN.")]
        public PowerTransformResult ApplyPowerTransformer(
            [Description(
                "Table rows to transform: one array of numbers per case, in the same column order used for fitting.")]
            double[][] data,
            [Description("One lambda per column, as returned by FitPowerTransformer.")]
            double[] lambdas,
            [Description("BoxCox or YeoJohnson; must match the method used to fit. Default YeoJohnson.")]
            PowerTransformMethod method = PowerTransformMethod.YeoJohnson)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));

            if (lambdas == null || lambdas.Length != columns)
            {
                throw new ArgumentException($"Give one lambda per column: the table has {columns} columns.",
                    nameof(lambdas));
            }

            return new PowerTransformResult(method, DataPreparationGuard.Names(null, columns, "names"), lambdas,
                PowerApply(data, lambdas, method));
        }

        [AdvancedAnalyticsTool("ApplyColumnFunction")]
        [Description(
            "Applies a fixed function to every cell of a table: natural log, log10, log2, log1p, signed log1p (sign(x) * ln(1 + |x|), good for money with negatives), square root, signed square root, cube root, reciprocal, square or absolute value. " +
            "An offset is added first, so ln(x + offset) can handle zeros. Cells where the function is undefined (log of a non-positive number, square root of a negative, reciprocal of zero) and missing cells become NaN. " +
            "Returns the table and the number of cells that became NaN because the function was undefined.")]
        public ColumnFunctionResult ApplyColumnFunction(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "The function to apply: NaturalLog, Log10, Log2, Log1P, SignedLog1P, Sqrt, SignedSqrt, CubeRoot, Reciprocal, Square or AbsoluteValue.")]
            ColumnFunction function,
            [Description("A constant added to each value before the function is applied. Default 0.")]
            double offset = 0)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));

            if (!double.IsFinite(offset))
            {
                throw new ArgumentException("The offset must be a finite number.", nameof(offset));
            }

            var undefined = 0;

            var table = DataPreparationGuard.Build(data.Length, columns, (i, j) =>
            {
                var x = data[i][j];

                if (DataPreparationGuard.IsMissing(x))
                {
                    return double.NaN;
                }

                x += offset;

                var y = function switch
                {
                    ColumnFunction.NaturalLog => x > 0 ? Math.Log(x) : double.NaN,
                    ColumnFunction.Log10 => x > 0 ? Math.Log10(x) : double.NaN,
                    ColumnFunction.Log2 => x > 0 ? Math.Log2(x) : double.NaN,
                    ColumnFunction.Log1P => x > -1 ? Math.Log(1 + x) : double.NaN,
                    ColumnFunction.SignedLog1P => Math.Sign(x) * Math.Log(1 + Math.Abs(x)),
                    ColumnFunction.Sqrt => x >= 0 ? Math.Sqrt(x) : double.NaN,
                    ColumnFunction.SignedSqrt => Math.Sign(x) * Math.Sqrt(Math.Abs(x)),
                    ColumnFunction.CubeRoot => Math.Cbrt(x),
                    ColumnFunction.Reciprocal => x != 0 ? 1 / x : double.NaN,
                    ColumnFunction.Square => x * x,
                    _ => Math.Abs(x)
                };

                if (double.IsNaN(y))
                {
                    undefined++;
                }

                return y;
            });

            return new ColumnFunctionResult(table, undefined);
        }

        [AdvancedAnalyticsTool("FitOutlierClipper")]
        [Description(
            "Learns lower and upper limits per column and clips (winsorises) values beyond them to the limit instead of deleting rows. " +
            "Quantiles clips at the lowerQuantile and upperQuantile of the data; IqrFences at Q1 - k*IQR and Q3 + k*IQR (k = multiplier, Tukey uses 1.5); StandardDeviations at mean +/- k sample standard deviations; MedianAbsoluteDeviations at median +/- k * 1.4826 * MAD, which is not distorted by the outliers themselves. " +
            "Returns the clipped table, the bounds (keep them for ApplyOutlierClipper) and how many cells were clipped low and high per column. Missing cells are ignored and stay NaN. A column with no finite value is rejected.")]
        public ClippingResult FitOutlierClipper(
            [Description(
                "Table rows: one array of numbers per case, all the same length. NaN or infinity marks a missing value.")]
            double[][] data,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null,
            [Description("Quantiles, IqrFences, StandardDeviations or MedianAbsoluteDeviations. Default Quantiles.")]
            OutlierBoundsMethod method = OutlierBoundsMethod.Quantiles,
            [Description("For the Quantiles method, the lower clipping quantile between 0 and 1. Default 0.01.")]
            double lowerQuantile = 0.01,
            [Description("For the Quantiles method, the upper clipping quantile between 0 and 1. Default 0.99.")]
            double upperQuantile = 0.99,
            [Description(
                "For the other methods, how many IQRs, standard deviations or scaled MADs from the centre to allow. Default 1.5.")]
            double multiplier = 1.5)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));
            DataPreparationGuard.Fraction(lowerQuantile, nameof(lowerQuantile));
            DataPreparationGuard.Fraction(upperQuantile, nameof(upperQuantile));

            if (lowerQuantile > upperQuantile)
            {
                throw new ArgumentException("lowerQuantile must not exceed upperQuantile.", nameof(lowerQuantile));
            }

            if (double.IsNaN(multiplier) || multiplier < 0)
            {
                throw new ArgumentException("The multiplier must be zero or positive.", nameof(multiplier));
            }

            var lower = new double[columns];
            var upper = new double[columns];

            for (var j = 0; j < columns; j++)
            {
                var finite = DataPreparationGuard.FiniteColumn(data, j);

                if (finite.Length == 0)
                {
                    throw new ArgumentException($"Column '{names[j]}' has no finite value to derive bounds from.",
                        nameof(data));
                }

                var sorted = finite.OrderBy(v => v).ToArray();

                switch (method)
                {
                    case OutlierBoundsMethod.Quantiles:
                        lower[j] = DataPreparationGuard.Quantile(sorted, lowerQuantile);
                        upper[j] = DataPreparationGuard.Quantile(sorted, upperQuantile);
                        break;
                    case OutlierBoundsMethod.IqrFences:
                        var q1 = DataPreparationGuard.Quantile(sorted, 0.25);
                        var q3 = DataPreparationGuard.Quantile(sorted, 0.75);
                        lower[j] = q1 - multiplier * (q3 - q1);
                        upper[j] = q3 + multiplier * (q3 - q1);
                        break;
                    case OutlierBoundsMethod.StandardDeviations:
                        var z = Standardisation.Fit(finite);
                        lower[j] = z.Mean - multiplier * z.StandardDeviation;
                        upper[j] = z.Mean + multiplier * z.StandardDeviation;
                        break;
                    default:
                        var robust = RobustScaler.Fit(finite);
                        lower[j] = robust.Median - multiplier * robust.Scale;
                        upper[j] = robust.Median + multiplier * robust.Scale;
                        break;
                }
            }

            return Clip(data, names, lower, upper);
        }

        [AdvancedAnalyticsTool("ApplyOutlierClipper")]
        [Description(
            "Clips each column of a table to fixed lower and upper bounds, such as those returned by FitOutlierClipper, so new data is capped exactly as training data was. " +
            "Use a very large number such as 1e300 (or -1e300) as a bound to leave that side open. Returns the clipped table and how many cells were clipped low and high per column. Missing cells stay NaN.")]
        public ClippingResult ApplyOutlierClipper(
            [Description(
                "Table rows to clip: one array of numbers per case, in the same column order used for fitting.")]
            double[][] data,
            [Description("The lower bound for each column, in column order.")]
            double[] lowerBounds,
            [Description("The upper bound for each column, in column order; each must be at least its lower bound.")]
            double[] upperBounds,
            [Description(
                "Optional name for each column in column order. Leave empty to use column_0, column_1 and so on.")]
            string[]? columnNames = null)
        {
            var columns = DataPreparationGuard.Table(data, nameof(data));
            var names = DataPreparationGuard.Names(columnNames, columns, nameof(columnNames));

            if (lowerBounds == null || upperBounds == null || lowerBounds.Length != columns ||
                upperBounds.Length != columns)
            {
                throw new ArgumentException(
                    $"Give one lower and one upper bound per column: the table has {columns} columns.",
                    nameof(lowerBounds));
            }

            for (var j = 0; j < columns; j++)
            {
                if (double.IsNaN(lowerBounds[j]) || double.IsNaN(upperBounds[j]) || lowerBounds[j] > upperBounds[j])
                {
                    throw new ArgumentException($"The bounds for column '{names[j]}' must satisfy lower <= upper.",
                        nameof(upperBounds));
                }
            }

            return Clip(data, names, lowerBounds, upperBounds);
        }

        private static void ValidateParameters(int columns, double[]? centers, double[]? scales)
        {
            if (centers == null || centers.Length != columns)
            {
                throw new ArgumentException($"Give one center per column: the table has {columns} columns.",
                    nameof(centers));
            }

            if (scales == null || scales.Length != columns)
            {
                throw new ArgumentException($"Give one scale per column: the table has {columns} columns.",
                    nameof(scales));
            }
        }

        private static double[][] Scale(double[][] data, double[] centers, double[] scales)
        {
            return DataPreparationGuard.Build(data.Length, centers.Length, (i, j) =>
                DataPreparationGuard.IsMissing(data[i][j])
                    ? double.NaN
                    : scales[j] == 0 || !double.IsFinite(scales[j])
                        ? 0
                        : (data[i][j] - centers[j]) / scales[j]);
        }

        private static double[][] TransformQuantiles(double[][] data, double[][] references,
            QuantileOutputDistribution output)
        {
            return DataPreparationGuard.Build(data.Length, references.Length, (i, j) =>
            {
                if (DataPreparationGuard.IsMissing(data[i][j]))
                {
                    return double.NaN;
                }

                var p = EmpiricalPosition(references[j], data[i][j]);

                return output == QuantileOutputDistribution.Uniform
                    ? p
                    : Normal.Inverse(Math.Min(Math.Max(p, 1e-7), 1 - 1e-7));
            });
        }

        private static double EmpiricalPosition(double[] references, double x)
        {
            var n = references.Length;

            if (x <= references[0])
            {
                return 0;
            }

            if (x >= references[n - 1])
            {
                return 1;
            }

            var high = 0;

            while (high + 1 < n && references[high + 1] <= x)
            {
                high++;
            }

            var upper = high + (x - references[high]) / (references[high + 1] - references[high]);

            var low = n - 1;

            while (low - 1 >= 0 && references[low - 1] >= x)
            {
                low--;
            }

            var lower = references[low] == x
                ? low
                : low - 1 + (x - references[low - 1]) / (references[low] - references[low - 1]);

            return (upper + lower) / 2 / (n - 1);
        }

        private static double PowerValue(double x, double lambda, PowerTransformMethod method)
        {
            const double epsilon = 1e-12;

            if (method == PowerTransformMethod.BoxCox)
            {
                if (x <= 0)
                {
                    return double.NaN;
                }

                return Math.Abs(lambda) < epsilon ? Math.Log(x) : (Math.Pow(x, lambda) - 1) / lambda;
            }

            if (x >= 0)
            {
                return Math.Abs(lambda) < epsilon ? Math.Log(x + 1) : (Math.Pow(x + 1, lambda) - 1) / lambda;
            }

            return Math.Abs(lambda - 2) < epsilon
                ? -Math.Log(-x + 1)
                : -(Math.Pow(-x + 1, 2 - lambda) - 1) / (2 - lambda);
        }

        private static double LogLikelihood(double[] values, double lambda, PowerTransformMethod method)
        {
            var transformed = values.Select(v => PowerValue(v, lambda, method)).ToArray();
            var mean = transformed.Average();
            var variance = transformed.Sum(t => (t - mean) * (t - mean)) / transformed.Length;

            if (!(variance > 0) || !double.IsFinite(variance))
            {
                return double.NegativeInfinity;
            }

            var jacobian = method == PowerTransformMethod.BoxCox
                ? values.Sum(v => Math.Log(v))
                : values.Sum(v => Math.Sign(v) * Math.Log(1 + Math.Abs(v)));

            return -values.Length / 2d * Math.Log(variance) + (lambda - 1) * jacobian;
        }

        private static double[][] PowerApply(double[][] data, double[] lambdas, PowerTransformMethod method)
        {
            return DataPreparationGuard.Build(data.Length, lambdas.Length, (i, j) =>
                DataPreparationGuard.IsMissing(data[i][j]) ? double.NaN : PowerValue(data[i][j], lambdas[j], method));
        }

        private static ClippingResult Clip(double[][] data, string[] names, double[] lower, double[] upper)
        {
            var low = new int[names.Length];
            var high = new int[names.Length];

            var table = DataPreparationGuard.Build(data.Length, names.Length, (i, j) =>
            {
                var x = data[i][j];

                if (DataPreparationGuard.IsMissing(x))
                {
                    return double.NaN;
                }

                if (x < lower[j])
                {
                    low[j]++;

                    return lower[j];
                }

                if (x > upper[j])
                {
                    high[j]++;

                    return upper[j];
                }

                return x;
            });

            return new ClippingResult(names, lower, upper, table, low, high, low.Sum() + high.Sum());
        }
    }

    public sealed record ScalingResult(
        TableScalingMethod Method,
        string[] ColumnNames,
        double[] Centers,
        double[] Scales,
        double[][] Table,
        string[] ConstantColumns);

    public sealed record RowNormalisationResult(double[][] Table, double[] Norms);

    public sealed record QuantileTransformResult(
        QuantileOutputDistribution Output,
        string[] ColumnNames,
        double[][] ReferenceQuantiles,
        double[][] Table);

    public sealed record PowerTransformResult(
        PowerTransformMethod Method,
        string[] ColumnNames,
        double[] Lambdas,
        double[][] Table);

    public sealed record ColumnFunctionResult(double[][] Table, int UndefinedCells);

    public sealed record ClippingResult(
        string[] ColumnNames,
        double[] LowerBounds,
        double[] UpperBounds,
        double[][] Table,
        int[] ClippedLowPerColumn,
        int[] ClippedHighPerColumn,
        int TotalClipped);
}