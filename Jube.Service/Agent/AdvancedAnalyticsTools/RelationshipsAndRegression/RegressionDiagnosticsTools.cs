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
using Accord.Math.Decompositions;
using Accord.Statistics;
using Accord.Statistics.Testing;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    [AdvancedAnalyticsToolGroup("RelationshipsAndRegression")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class RegressionDiagnosticsTools
    {
        [AdvancedAnalyticsTool("RegressionInfluenceDiagnostics")]
        [Description(
            "Finds the rows that have an outsized effect on an ordinary least squares fit, using the standard influence measures. " +
            "For each returned row: Leverage (hat value, how unusual the predictor values are; average is k/n), StandardisedResidual and StudentisedResidual (the residual scaled by its standard error, the latter with the row left out so an outlier cannot mask itself; beyond about 3 in size is an outlier), CooksDistance (how far all fitted values move if the row is deleted; above 4/n is influential, above 1 is severe), Dffits (change in that row's own fitted value in standard errors), CovarianceRatio and Dfbetas (change in each coefficient, in standard errors, when the row is deleted). " +
            "Flags name the thresholds crossed: HighLeverage (above 2k/n), Outlier (|studentised| above 3), Influential (Cook above 4/n), HighDffits (above 2*sqrt(k/n)) and HighDfbetas (above 2/sqrt(n)). " +
            "By default only flagged rows are returned in Rows (up to maximumRows, worst Cook first); MostInfluential always holds the top rows by Cook's distance. Also returned are PRESS (sum of squared leave-one-out prediction errors) and the leave-one-out RMSE. Row indices refer to the original input rows, and KeptRowIndices maps them when incomplete rows were dropped.")]
        public InfluenceDiagnosticsResult RegressionInfluenceDiagnostics(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description("True returns every row in Rows instead of only flagged rows. Default false.")]
            bool returnAllRows = false,
            [Description("Maximum number of rows returned in Rows, from 1 to 10000. Default 50.")]
            int maximumRows = 50,
            [Description("Number of rows listed in MostInfluential, from 1 to 100. Default 10.")]
            int topCount = 10,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            if (maximumRows < 1 || maximumRows > 10000)
            {
                throw new ArgumentException("maximumRows must be between 1 and 10000.", nameof(maximumRows));
            }

            if (topCount < 1 || topCount > 100)
            {
                throw new ArgumentException("topCount must be between 1 and 100.", nameof(topCount));
            }

            var context = DiagnosticsCore.Prepare(inputs, outputs, variableNames, includeIntercept, dropIncompleteRows);
            var fit = context.Fit;
            var n = fit.N;
            var k = fit.K;
            var standardised = DiagnosticsCore.StandardisedResiduals(fit);
            var studentised = DiagnosticsCore.ExternallyStudentised(fit);
            var cooks = DiagnosticsCore.CooksDistance(fit);
            var leverageThreshold = 2d * k / n;
            var cooksThreshold = 4d / n;
            var dffitsThreshold = 2 * Math.Sqrt((double)k / n);
            var dfbetasThreshold = 2 / Math.Sqrt(n);
            var rows = new InfluenceRow[n];
            var press = 0d;

            for (var i = 0; i < n; i++)
            {
                var h = fit.Leverage[i];
                var oneMinusH = Math.Max(1e-12, 1 - h);
                var e = fit.Residuals[i];
                var s2I = fit.DfResidual > 1 ? (fit.Rss - e * e / oneMinusH) / (fit.DfResidual - 1) : double.NaN;
                var t = studentised[i];
                var dffits = t * Math.Sqrt(h / oneMinusH);
                var covRatio = Math.Pow(s2I / fit.Sigma2, k) / oneMinusH;
                var dfbetas = new double[k];
                var direction = new double[k];

                for (var a = 0; a < k; a++)
                {
                    for (var b = 0; b < k; b++)
                    {
                        direction[a] += fit.XtXInverse[a][b] * fit.Design[i][b];
                    }
                }

                for (var a = 0; a < k; a++)
                {
                    dfbetas[a] = direction[a] * e / oneMinusH / (Math.Sqrt(s2I) * Math.Sqrt(fit.XtXInverse[a][a]));
                }

                press += e * e / (oneMinusH * oneMinusH);
                var flags = new List<string>();

                if (h > leverageThreshold)
                {
                    flags.Add("HighLeverage");
                }

                if (Math.Abs(t) > 3)
                {
                    flags.Add("Outlier");
                }

                if (cooks[i] > cooksThreshold)
                {
                    flags.Add("Influential");
                }

                if (Math.Abs(dffits) > dffitsThreshold)
                {
                    flags.Add("HighDffits");
                }

                if (dfbetas.Any(d => Math.Abs(d) > dfbetasThreshold))
                {
                    flags.Add("HighDfbetas");
                }

                rows[i] = new InfluenceRow
                {
                    Row = context.Data.KeptRows[i],
                    Fitted = fit.Fitted[i],
                    Residual = e,
                    Leverage = h,
                    StandardisedResidual = standardised[i],
                    StudentisedResidual = t,
                    CooksDistance = cooks[i],
                    Dffits = dffits,
                    CovarianceRatio = covRatio,
                    Dfbetas = dfbetas,
                    Flags = flags.ToArray()
                };
            }

            var byCook = rows.OrderByDescending(r => r.CooksDistance).ToArray();

            var reported = (returnAllRows ? byCook : byCook.Where(r => r.Flags.Length > 0).ToArray()).Take(maximumRows)
                .ToArray();

            return new InfluenceDiagnosticsResult
            {
                Rows = reported,
                MostInfluential = byCook.Take(topCount).ToArray(),
                Terms = fit.Terms,
                LeverageThreshold = leverageThreshold,
                CooksDistanceThreshold = cooksThreshold,
                DffitsThreshold = dffitsThreshold,
                DfbetasThreshold = dfbetasThreshold,
                StudentisedResidualThreshold = 3,
                HighLeverageCount = rows.Count(r => r.Flags.Contains("HighLeverage")),
                InfluentialCount = rows.Count(r => r.Flags.Contains("Influential")),
                OutlierCount = rows.Count(r => r.Flags.Contains("Outlier")),
                PressStatistic = press,
                LeaveOneOutRmse = Math.Sqrt(press / n),
                Observations = n,
                RowsDropped = context.Data.Dropped,
                KeptRowIndices = context.Data.KeptRows
            };
        }

        [AdvancedAnalyticsTool("DurbinWatsonRegressionTest")]
        [Description(
            "Checks whether the residuals of a linear regression are correlated with the residual of the row before (serial correlation), using the Durbin-Watson statistic on the rows in the order supplied, so sort by time first. " +
            "DurbinWatson runs from 0 to 4: about 2 means no first-order autocorrelation, below about 1.5 positive autocorrelation (residuals tend to repeat the previous sign, making standard errors too small) and above about 2.5 negative. FirstOrderAutocorrelation is the lag-1 correlation of the residuals (roughly 1 - DW/2). " +
            "ApproximateZ and ApproximatePValue use the large-sample normal approximation sqrt(n) times the autocorrelation; the exact Durbin-Watson bounds tables are not used, so treat the p-value as a guide for n above about 30. Serial correlation in row order invalidates the standard errors; for a time series consider the time series tools. See also BreuschGodfreySerialCorrelationTest for higher orders.")]
        public ResidualDurbinWatsonResult DurbinWatsonRegressionTest(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            var context = DiagnosticsCore.Prepare(inputs, outputs, variableNames, includeIntercept, dropIncompleteRows);
            var fit = context.Fit;
            var dw = LinearReporting.DurbinWatson(fit.Residuals);
            var rho = DiagnosticsCore.FirstOrderAutocorrelation(fit.Residuals);
            var z = rho * Math.Sqrt(fit.N);

            return new ResidualDurbinWatsonResult
            {
                DurbinWatson = dw,
                FirstOrderAutocorrelation = rho,
                ApproximateZ = z,
                ApproximatePValue = RegressionStats.NormalTwoSided(z),
                Interpretation = double.IsNaN(dw)
                    ? "The residuals are all zero, so the statistic is undefined."
                    : dw < 1.5
                        ? "Residuals look positively autocorrelated (DW below 1.5)."
                        : dw > 2.5
                            ? "Residuals look negatively autocorrelated (DW above 2.5)."
                            : "No strong first-order autocorrelation (DW between 1.5 and 2.5).",
                Observations = fit.N
            };
        }

        [AdvancedAnalyticsTool("BreuschGodfreySerialCorrelationTest")]
        [Description(
            "Tests for serial correlation of any order in linear regression residuals with the Breusch-Godfrey Lagrange multiplier test: the residuals are regressed on the original predictors plus their own lags, and LM = n * R squared of that auxiliary regression is compared with a chi-square with `order` degrees of freedom (missing initial lags are set to 0). " +
            "A small p-value means the residuals are correlated across neighbouring rows, so standard errors are unreliable. Unlike Durbin-Watson it is valid with lagged outcomes as predictors and tests several lags at once. Rows must be in time order. " +
            "Returns the statistic, degrees of freedom, p-value, the auxiliary R squared and a plain-language conclusion.")]
        public DiagnosticTestResult BreuschGodfreySerialCorrelationTest(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(
                "Highest lag of residual autocorrelation to test, from 1 to 20 and well below the row count. Default 1.")]
            int order = 1,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            if (order < 1 || order > 20)
            {
                throw new ArgumentException("order must be between 1 and 20.", nameof(order));
            }

            var context = DiagnosticsCore.Prepare(inputs, outputs, variableNames, includeIntercept, dropIncompleteRows);
            var fit = context.Fit;
            var n = fit.N;

            if (n <= order + fit.K + 2)
            {
                throw new ArgumentException(
                    $"There are {n} usable rows, too few for order {order} with {fit.K} coefficients. Lower the order or supply more rows.",
                    nameof(order));
            }

            var columns = context.Data.X.Select((row, i) =>
                    row.Concat(Enumerable.Range(1, order).Select(l => i - l >= 0 ? fit.Residuals[i - l] : 0d))
                        .ToArray())
                .ToArray();

            var aux = LinearFit.Fit(columns, fit.Residuals, null,
                context.Data.Names.Concat(Enumerable.Range(1, order).Select(l => "lag" + l)).ToArray(), true);

            var lm = n * aux.RSquared;

            return DiagnosticsCore.Lm("Breusch-Godfrey", $"There is no residual autocorrelation up to lag {order}.", lm,
                order, aux.RSquared, n,
                "Evidence of serial correlation in the residuals; standard errors are unreliable and the model is missing time structure.",
                "No significant evidence of serial correlation in the residuals.");
        }

        [AdvancedAnalyticsTool("BreuschPaganHeteroscedasticityTest")]
        [Description(
            "Tests whether the spread of the residuals of a linear regression changes with the predictors (heteroscedasticity), using the Breusch-Pagan Lagrange multiplier test: the squared residuals are regressed on the predictors and the statistic is compared with a chi-square with one degree of freedom per predictor. " +
            "The default Studentised (Koenker) variant uses LM = n * R squared of the auxiliary regression and is robust to non-normal errors; Classical divides the explained sum of squares by 2*sigma^4 and assumes normal errors. " +
            "A small p-value means the variance is not constant, so ordinary standard errors and p-values are unreliable: consider a transformation of the outcome, weighted least squares or robust standard errors. Returns the statistic, degrees of freedom, p-value, the auxiliary R squared and a plain-language conclusion. See WhiteHeteroscedasticityTest for non-linear forms of variance change.")]
        public DiagnosticTestResult BreuschPaganHeteroscedasticityTest(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(
                "Studentised (Koenker, default) or Classical (Breusch-Pagan original, assumes normal errors).")]
            HeteroscedasticityVariant variant = HeteroscedasticityVariant.Studentised,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            if (!Enum.IsDefined(typeof(HeteroscedasticityVariant), variant))
            {
                throw new ArgumentException("Unknown variant. Use Studentised or Classical.", nameof(variant));
            }

            var context = DiagnosticsCore.Prepare(inputs, outputs, variableNames, includeIntercept, dropIncompleteRows);

            if (context.Fit.N <= context.Fit.K + 2)
            {
                throw new ArgumentException("There are too few rows for the auxiliary regression.", nameof(inputs));
            }

            return DiagnosticsCore.BreuschPagan(context, variant);
        }

        [AdvancedAnalyticsTool("WhiteHeteroscedasticityTest")]
        [Description(
            "Tests for heteroscedasticity of unknown form with White's test: the squared residuals are regressed on the predictors, their squares and all pairwise products, and LM = n * R squared of that auxiliary regression is compared with a chi-square with one degree of freedom per auxiliary term. " +
            "It detects variance that changes non-linearly or through interactions, which BreuschPaganHeteroscedasticityTest can miss, but it uses many terms so it is less powerful in small samples. It also reacts to some kinds of model misspecification. " +
            "At most 10 predictors are accepted; constant, duplicated or collinear auxiliary terms (for example the square of a 0/1 dummy) are rejected with a message, so remove such columns first. Returns the statistic, degrees of freedom, p-value, auxiliary R squared and a plain-language conclusion.")]
        public DiagnosticTestResult WhiteHeteroscedasticityTest(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            var context =
                DiagnosticsCore.Prepare(inputs, outputs, variableNames, includeIntercept, dropIncompleteRows, 8);

            var p = context.Data.P;

            if (p > 10)
            {
                throw new ArgumentException("White's test accepts at most 10 predictors.", nameof(inputs));
            }

            var columns = context.Data.X.Select(r =>
            {
                var terms = new List<double>(r);

                for (var a = 0; a < p; a++)
                {
                    for (var b = a; b < p; b++)
                    {
                        terms.Add(r[a] * r[b]);
                    }
                }

                return terms.ToArray();
            }).ToArray();

            var squared = context.Fit.Residuals.Select(e => e * e).ToArray();

            if (context.Fit.N <= columns[0].Length + 2)
            {
                throw new ArgumentException(
                    $"White's test needs more than {columns[0].Length + 2} rows for {columns[0].Length} auxiliary terms; {context.Fit.N} were supplied.",
                    nameof(inputs));
            }

            var aux = LinearFit.Fit(columns, squared, null,
                Enumerable.Range(1, columns[0].Length).Select(i => "t" + i).ToArray(), true);

            return DiagnosticsCore.Lm("White", "The residual variance is constant (homoscedastic).",
                context.Fit.N * aux.RSquared,
                columns[0].Length, aux.RSquared, context.Fit.N,
                "Evidence of heteroscedasticity (or misspecification); consider a transformation, weighted least squares or robust standard errors.",
                "No significant evidence of heteroscedasticity.");
        }

        [AdvancedAnalyticsTool("GoldfeldQuandtTest")]
        [Description(
            "Tests whether the residual variance differs between the low and high end of one predictor with the Goldfeld-Quandt test: rows are sorted by the chosen predictor, a middle fraction is omitted, the regression is refitted separately on the lower and upper groups and the ratio of their residual variances is compared with an F distribution. " +
            "The F statistic is (upper group variance) / (lower group variance); a small p-value in the default one-sided test means variance rises with the predictor, and twoSided also flags variance that falls. Estimate1 and Estimate2 are the residual variances of the lower and upper groups. " +
            "Omitting about 20 percent of the central rows increases power; each group needs more rows than coefficients. Assumes normal errors.")]
        public DiagnosticTestResult GoldfeldQuandtTest(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(
                "Name of the predictor column to sort the rows by, exactly as in variableNames (or x1, x2, ... when names are empty).")]
            string sortVariable,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description("Fraction of central rows left out, from 0 to 0.6. Default 0.2.")]
            double fractionOmitted = 0.2,
            [Description(
                "True tests for any difference in variance; false (default) tests only that variance is larger in the upper group.")]
            bool twoSided = false,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            if (!(fractionOmitted >= 0 && fractionOmitted <= 0.6))
            {
                throw new ArgumentException("fractionOmitted must be between 0 and 0.6.", nameof(fractionOmitted));
            }

            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, dropIncompleteRows, 6);
            var column = Array.IndexOf(data.Names, sortVariable);

            if (column < 0)
            {
                throw new ArgumentException(
                    $"'{sortVariable}' is not one of the variable names ({string.Join(", ", data.Names)}).",
                    nameof(sortVariable));
            }

            var order = Enumerable.Range(0, data.N).OrderBy(i => data.X[i][column]).ToArray();
            var omitted = (int)Math.Round(fractionOmitted * data.N);
            var groupSize = (data.N - omitted) / 2;
            var k = data.P + (includeIntercept ? 1 : 0);

            if (groupSize <= k)
            {
                throw new ArgumentException(
                    $"Each group would have {groupSize} row(s) for {k} coefficients. Omit fewer rows or supply more data.",
                    nameof(fractionOmitted));
            }

            var low = order.Take(groupSize).ToArray();
            var high = order.Skip(data.N - groupSize).ToArray();

            var fitLow = LinearFit.Fit(low.Select(i => data.X[i]).ToArray(), low.Select(i => data.Y[i]).ToArray(), null,
                data.Names, includeIntercept);
            var fitHigh = LinearFit.Fit(high.Select(i => data.X[i]).ToArray(), high.Select(i => data.Y[i]).ToArray(),
                null,
                data.Names, includeIntercept);

            var f = fitHigh.Sigma2 / fitLow.Sigma2;
            var upper = RegressionStats.FUpperTail(f, fitHigh.DfResidual, fitLow.DfResidual);
            var p = twoSided ? Math.Min(1, 2 * Math.Min(upper, 1 - upper)) : upper;

            return new DiagnosticTestResult
            {
                Test = "Goldfeld-Quandt",
                Statistic = f,
                DegreesOfFreedom = fitHigh.DfResidual,
                DegreesOfFreedom2 = fitLow.DfResidual,
                PValue = p,
                Distribution = "F",
                NullHypothesis = "The residual variance is the same in the lower and upper groups.",
                Conclusion = p < 0.05
                    ? twoSided
                        ? "The residual variance differs between the lower and upper groups."
                        : "The residual variance is larger in the upper group (variance rises with the predictor)."
                    : "No significant difference in residual variance between the groups.",
                Observations = data.N,
                AuxiliaryRSquared = double.NaN,
                Estimate1 = fitLow.Sigma2,
                Estimate2 = fitHigh.Sigma2
            };
        }

        [AdvancedAnalyticsTool("ResidualNormalityTests")]
        [Description(
            "Summarises whether the residuals of a linear regression look normally distributed, which matters for the exactness of t tests, F tests and prediction intervals in small samples. " +
            "It returns the residual mean, standard deviation, skewness (0 for symmetric) and excess kurtosis (0 for normal tails; positive means heavy tails, so more extreme residuals than normal), the Jarque-Bera statistic n/6*(S^2 + K^2/4) with its chi-square (2 degrees of freedom) p-value, the Shapiro-Wilk statistic W (close to 1 for normal) and p-value (needs 4 to 5000 rows, otherwise NaN), the extremes, and the fractions of residuals beyond 2 and 3 standard deviations (about 0.046 and 0.003 if normal). " +
            "Small p-values indicate non-normal residuals. With many rows the tests flag trivial departures and coefficient inference remains approximately valid by the central limit theorem; with few rows heavy tails or skew matter more.")]
        public ResidualNormalityResult ResidualNormalityTests(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            var context = DiagnosticsCore.Prepare(inputs, outputs, variableNames, includeIntercept, dropIncompleteRows);
            var e = context.Fit.Residuals;
            var mean = e.Average();
            var sd = e.StandardDeviation();
            var jb = DiagnosticsCore.JarqueBera(e);
            var w = double.NaN;
            var wp = double.NaN;

            if (e.Length >= 4 && e.Length <= 5000 && e.Max() > e.Min())
            {
                var sw = new ShapiroWilkTest(e);
                w = sw.Statistic;
                wp = sw.PValue;
            }

            var nonNormal = jb.P < 0.05 || wp < 0.05;

            return new ResidualNormalityResult
            {
                Observations = e.Length,
                Mean = mean,
                StandardDeviation = sd,
                Skewness = jb.Skew,
                ExcessKurtosis = jb.Kurt,
                JarqueBeraStatistic = jb.Statistic,
                JarqueBeraPValue = jb.P,
                ShapiroWilkStatistic = w,
                ShapiroWilkPValue = wp,
                MinimumResidual = e.Min(),
                MaximumResidual = e.Max(),
                FractionBeyondTwoSigma = e.Count(v => Math.Abs(v - mean) > 2 * sd) / (double)e.Length,
                FractionBeyondThreeSigma = e.Count(v => Math.Abs(v - mean) > 3 * sd) / (double)e.Length,
                Conclusion = nonNormal
                    ? "The residuals depart significantly from normality; check skewness and kurtosis and consider a transformation, robust methods or more data before relying on small-sample intervals."
                    : "No significant departure from normality detected."
            };
        }

        [AdvancedAnalyticsTool("RamseyResetTest")]
        [Description(
            "Tests whether a linear regression is missing curvature or omitted non-linear terms with Ramsey's RESET: powers 2 to `power` of the fitted values are added as extra predictors and an F test checks whether they jointly improve the fit. " +
            "A small p-value means the linear specification is inadequate (the relationship is curved, or an important variable or interaction is omitted); try polynomial terms, transformations or extra predictors. " +
            "Returns the F statistic with its numerator and denominator degrees of freedom, the p-value, the R squared of the original and augmented models (Estimate1 and Estimate2) and a conclusion. The test cannot say which variable to change.")]
        public DiagnosticTestResult RamseyResetTest(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description("Highest power of the fitted values to add, from 2 to 5. Default 3 (adds squares and cubes).")]
            int power = 3,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            if (power < 2 || power > 5)
            {
                throw new ArgumentException("power must be between 2 and 5.", nameof(power));
            }

            var context =
                DiagnosticsCore.Prepare(inputs, outputs, variableNames, includeIntercept, dropIncompleteRows, 8);

            var fit = context.Fit;
            var extra = power - 1;

            if (fit.N <= fit.K + extra + 1)
            {
                throw new ArgumentException("There are too few rows for the augmented model.", nameof(inputs));
            }

            var augmentedRows = context.Data.X.Select((r, i) =>
                r.Concat(Enumerable.Range(2, extra).Select(pw => Math.Pow(fit.Fitted[i], pw))).ToArray()).ToArray();

            var names = context.Data.Names.Concat(Enumerable.Range(2, extra).Select(pw => "fitted^" + pw)).ToArray();
            LinearFit augmented;

            try
            {
                augmented = LinearFit.Fit(augmentedRows, context.Data.Y, null, names, includeIntercept);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException(
                    "The powers of the fitted values are collinear with the predictors (the model is already exactly linear in them), so RESET cannot be computed.",
                    nameof(inputs));
            }

            if (fit.Tss > 0 && fit.Rss <= 1e-18 * fit.Tss)
            {
                throw new ArgumentException(
                    "The model already fits the outcome exactly (zero residual variance), so RESET is undefined.",
                    nameof(inputs));
            }

            var f = (fit.Rss - augmented.Rss) / extra / (augmented.Rss / augmented.DfResidual);
            var p = RegressionStats.FUpperTail(f, extra, augmented.DfResidual);

            return new DiagnosticTestResult
            {
                Test = "Ramsey RESET",
                Statistic = f,
                DegreesOfFreedom = extra,
                DegreesOfFreedom2 = augmented.DfResidual,
                PValue = p,
                Distribution = "F",
                NullHypothesis = "The linear specification is adequate (no omitted non-linear terms).",
                Conclusion = p < 0.05
                    ? "Evidence of non-linearity or omitted variables; the linear specification is inadequate."
                    : "No significant evidence that the linear specification is inadequate.",
                Observations = fit.N,
                AuxiliaryRSquared = augmented.RSquared,
                Estimate1 = fit.RSquared,
                Estimate2 = augmented.RSquared
            };
        }

        [AdvancedAnalyticsTool("ChowStructuralBreakTest")]
        [Description(
            "Tests whether a regression relationship changes at a known point in the rows (for example before and after a policy change or a system upgrade) with the Chow test: the model is fitted separately to the rows before and from breakIndex, and an F test compares the two separate fits against a single pooled fit. " +
            "F = ((RSS pooled - RSS1 - RSS2)/k) / ((RSS1+RSS2)/(n-2k)) with k coefficients. A small p-value means the coefficients differ between the two periods. Estimate1 and Estimate2 hold the residual sums of squares of the two parts. " +
            "Rows must be ordered so that the break splits them; breakIndex is the position (0 based) of the first row of the second period, and each part needs more rows than coefficients.")]
        public DiagnosticTestResult ChowStructuralBreakTest(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(
                "Zero-based position of the first row of the second period; rows before it form the first period.")]
            int breakIndex,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true)
        {
            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, false, 6);
            var k = data.P + (includeIntercept ? 1 : 0);

            if (breakIndex <= k || data.N - breakIndex <= k)
            {
                throw new ArgumentException(
                    $"breakIndex {breakIndex} leaves too few rows on one side; each period needs more than {k} rows (there are {data.N} in total).",
                    nameof(breakIndex));
            }

            var pooled = LinearFit.Fit(data.X, data.Y, null, data.Names, includeIntercept);

            var first = LinearFit.Fit(data.X.Take(breakIndex).ToArray(), data.Y.Take(breakIndex).ToArray(), null,
                data.Names, includeIntercept);
            var second = LinearFit.Fit(data.X.Skip(breakIndex).ToArray(), data.Y.Skip(breakIndex).ToArray(), null,
                data.Names, includeIntercept);

            var separate = first.Rss + second.Rss;
            var df2 = data.N - 2d * k;
            var f = (pooled.Rss - separate) / k / (separate / df2);
            var p = RegressionStats.FUpperTail(f, k, df2);

            return new DiagnosticTestResult
            {
                Test = "Chow",
                Statistic = f,
                DegreesOfFreedom = k,
                DegreesOfFreedom2 = df2,
                PValue = p,
                Distribution = "F",
                NullHypothesis = "The regression coefficients are the same before and after the break.",
                Conclusion = p < 0.05
                    ? "The coefficients differ between the two periods: there is a structural break."
                    : "No significant evidence of a structural break at this point.",
                Observations = data.N,
                AuxiliaryRSquared = double.NaN,
                Estimate1 = first.Rss,
                Estimate2 = second.Rss
            };
        }

        [AdvancedAnalyticsTool("VarianceInflationFactors")]
        [Description(
            "Measures how much each predictor is explained by the other predictors, the main check for multicollinearity before trusting individual regression coefficients. " +
            "For each column it returns the variance inflation factor VIF = 1/(1-R squared of that column regressed on all the others), Tolerance = 1/VIF, RSquaredOnOthers, StandardErrorInflation = sqrt(VIF) (how many times larger the coefficient's standard error is than with uncorrelated predictors) and a Severity label: Low below 5, Moderate from 5 to 10, High above 10. " +
            "A column that is an exact combination of others gets VIF infinity. Outcome values are not needed. With one column the VIF is 1. Fix high VIFs by dropping or combining predictors, or use FitRidgeRegression.")]
        public VarianceInflationResult VarianceInflationFactors(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description("True drops rows containing NaN and reports how many; false rejects missing values.")]
            bool dropIncompleteRows = false)
        {
            var columns = RegressionInput.ColumnCount(inputs, nameof(inputs));
            var names = RegressionInput.ResolveNames(variableNames, columns, nameof(variableNames));
            var placeholder = new double[inputs.Length];
            var data = RegressionInput.Prepare(inputs, placeholder, names, null, dropIncompleteRows, columns + 2);

            if (data.N <= columns + 1)
            {
                throw new ArgumentException($"At least {columns + 2} rows are needed for {columns} columns.",
                    nameof(inputs));
            }

            var vifs = new double[columns];

            if (columns == 1)
            {
                vifs[0] = 1;
            }
            else
            {
                var cols = RegressionInput.Columns(data.X);

                for (var j = 0; j < columns; j++)
                {
                    if (!(cols[j].StandardDeviation() > 0))
                    {
                        vifs[j] = double.NaN;
                        continue;
                    }

                    vifs[j] = VifByRegression(data.X, cols, j, names);
                }

                var inverse = TryInverseCorrelation(data.X);

                if (inverse != null)
                {
                    for (var j = 0; j < columns; j++)
                    {
                        if (!double.IsInfinity(vifs[j]) && !double.IsNaN(vifs[j]))
                        {
                            vifs[j] = inverse[j][j];
                        }
                    }
                }
            }

            var rows = names.Select((n, j) => new CollinearityRow
            {
                Variable = n,
                VarianceInflationFactor = vifs[j],
                Tolerance = 1 / vifs[j],
                RSquaredOnOthers = 1 - 1 / vifs[j],
                StandardErrorInflation = Math.Sqrt(vifs[j]),
                Severity = double.IsNaN(vifs[j]) ? "Undefined" :
                    vifs[j] > 10 ? "High" :
                    vifs[j] >= 5 ? "Moderate" : "Low"
            }).ToArray();

            var finite = vifs.Where(v => !double.IsNaN(v)).ToArray();

            return new VarianceInflationResult
            {
                Variables = rows,
                MaximumVif = finite.Length == 0 ? double.NaN : finite.Max(),
                MeanVif = finite.Length == 0 ? double.NaN : finite.Average(),
                Observations = data.N,
                Summary = rows.Any(r => r.Severity == "High")
                    ? "At least one predictor has a VIF above 10: strong multicollinearity. Drop or combine the flagged predictors, or use ridge regression."
                    : rows.Any(r => r.Severity == "Moderate")
                        ? "Some predictors have moderate VIF (5 to 10); coefficients may be somewhat imprecise."
                        : "All VIFs are below 5: multicollinearity is not a concern."
            };
        }

        [AdvancedAnalyticsTool("MulticollinearityConditionDiagnostics")]
        [Description(
            "Diagnoses collinearity among predictors with the Belsley-Kuh-Welsch method: the columns (including the intercept column when requested) are scaled to unit length and their singular values are examined. " +
            "Each row of Rows is one singular value with its ConditionIndex (largest singular value divided by this one; above about 10 is moderate and above 30 severe collinearity) and VarianceProportions, the share of each coefficient's variance attributable to that component. A collinearity problem is indicated when a high condition index has proportions above about 0.5 for two or more variables: those variables are involved in the near-dependency. " +
            "Also returned are the overall ConditionNumber of the scaled design, the condition number and determinant of the predictor correlation matrix (determinant near 0 means near-singular) and the numerical rank. Outcome values are not needed. Use VarianceInflationFactors for a per-variable summary.")]
        public ConditionDiagnosticsResult MulticollinearityConditionDiagnostics(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(
                "True (default) includes the intercept column in the scaled design, as Belsley recommends when the model has an intercept.")]
            bool includeIntercept = true,
            [Description("True drops rows containing NaN and reports how many; false rejects missing values.")]
            bool dropIncompleteRows = false)
        {
            var columns = RegressionInput.ColumnCount(inputs, nameof(inputs));
            var names = RegressionInput.ResolveNames(variableNames, columns, nameof(variableNames));

            var data = RegressionInput.Prepare(inputs, new double[inputs.Length], names, null, dropIncompleteRows,
                columns);

            var k = columns + (includeIntercept ? 1 : 0);

            if (data.N < k)
            {
                throw new ArgumentException($"At least {k} rows are needed for {k} design columns.", nameof(inputs));
            }

            var terms = includeIntercept ? new[] { "(Intercept)" }.Concat(names).ToArray() : names;

            var design = data.X.Select(r => includeIntercept ? new[] { 1d }.Concat(r).ToArray() : (double[])r.Clone())
                .ToArray();

            var norms = new double[k];

            foreach (var row in design)
            {
                for (var j = 0; j < k; j++)
                {
                    norms[j] += row[j] * row[j];
                }
            }

            for (var j = 0; j < k; j++)
            {
                if (!(norms[j] > 0))
                {
                    throw new ArgumentException($"Column '{terms[j]}' is all zeros. Remove it.", nameof(inputs));
                }

                norms[j] = Math.Sqrt(norms[j]);
            }

            var scaled = design.Select(r => r.Select((v, j) => v / norms[j]).ToArray()).ToArray();
            var svd = new JaggedSingularValueDecomposition(scaled, false, true);
            var singular = svd.Diagonal;
            var vectors = svd.RightSingularVectors;
            var largest = singular.Max();
            var rows = new List<ConditionIndexRow>();
            var phi = new double[k][];

            for (var j = 0; j < k; j++)
            {
                phi[j] = new double[k];

                for (var c = 0; c < k; c++)
                {
                    phi[j][c] = singular[c] > 0
                        ? vectors[j][c] * vectors[j][c] / (singular[c] * singular[c])
                        : double.PositiveInfinity;
                }
            }

            for (var c = 0; c < k; c++)
            {
                var index = singular[c] > 0 ? largest / singular[c] : double.PositiveInfinity;

                var proportions = Enumerable.Range(0, k).Select(j =>
                {
                    var total = phi[j].Sum();

                    return double.IsInfinity(total) ? double.IsInfinity(phi[j][c]) ? 1d : 0d : phi[j][c] / total;
                }).ToArray();

                rows.Add(new ConditionIndexRow
                {
                    Index = c + 1,
                    SingularValue = singular[c],
                    ConditionIndex = index,
                    VarianceProportions = proportions,
                    Severity = index > 30 ? "Severe" : index > 10 ? "Moderate" : "Low"
                });
            }

            var correlation = columns > 1 ? data.X.Correlation() : new[] { new[] { 1d } };
            var corrSingular = new JaggedSingularValueDecomposition(correlation, false, false).Diagonal;
            var tolerance = largest * Math.Max(data.N, k) * 2.220446049250313e-16;
            var conditionNumber = singular.Min() > 0 ? largest / singular.Min() : double.PositiveInfinity;

            return new ConditionDiagnosticsResult
            {
                Terms = terms,
                Rows = rows.OrderBy(r => r.ConditionIndex).ToArray(),
                ConditionNumber = conditionNumber,
                CorrelationMatrixConditionNumber = corrSingular.Min() > 0
                    ? corrSingular.Max() / corrSingular.Min()
                    : double.PositiveInfinity,
                Determinant = correlation.ToMatrix().Determinant(false),
                NumericalRank = singular.Count(s => s > tolerance),
                Observations = data.N,
                Summary = conditionNumber > 30
                    ? "The largest condition index exceeds 30: severe collinearity. Look at the variables with high variance proportions on that component."
                    : conditionNumber > 10
                        ? "The largest condition index is between 10 and 30: moderate collinearity."
                        : "All condition indices are below 10: no meaningful collinearity."
            };
        }

        [AdvancedAnalyticsTool("AddedVariablePlotData")]
        [Description(
            "Produces the data for an added-variable (partial regression) plot, which shows the effect of one predictor after all the other predictors have been accounted for. " +
            "The outcome and the chosen predictor are each regressed on the other predictors (with intercept) and the two sets of residuals are returned as paired points. By the Frisch-Waugh-Lovell theorem the slope of the line through those points equals the predictor's coefficient in the full multiple regression, and their correlation is the partial correlation. " +
            "Use it to see whether a coefficient is driven by a few points, is curved or is truly linear. The residual pairs are in the order of the kept rows; KeptRowIndices maps them to the input rows. Needs at least two predictors so that there is something to control for.")]
        public AddedVariablePlotResult AddedVariablePlotData(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(
                "Name of the predictor to examine, exactly as in variableNames (or x1, x2, ... when names are empty).")]
            string variable,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, dropIncompleteRows, 5);
            var column = Array.IndexOf(data.Names, variable);

            if (column < 0)
            {
                throw new ArgumentException(
                    $"'{variable}' is not one of the variable names ({string.Join(", ", data.Names)}).",
                    nameof(variable));
            }

            if (data.P < 2)
            {
                throw new ArgumentException(
                    "An added-variable plot needs at least two predictors so that the others can be controlled for.",
                    nameof(inputs));
            }

            var others = Enumerable.Range(0, data.P).Where(c => c != column).ToArray();
            var controls = data.X.Select(r => others.Select(c => r[c]).ToArray()).ToArray();
            var controlNames = others.Select(c => data.Names[c]).ToArray();

            var rx = LinearFit.Fit(controls, data.X.Select(r => r[column]).ToArray(), null, controlNames, true)
                .Residuals;

            var ry = LinearFit.Fit(controls, data.Y, null, controlNames, true).Residuals;
            var sxx = rx.Sum(v => v * v);
            var slope = sxx > 0 ? rx.Zip(ry, (a, b) => a * b).Sum() / sxx : double.NaN;

            return new AddedVariablePlotResult
            {
                XResiduals = rx,
                YResiduals = ry,
                Slope = slope,
                PartialCorrelation = CorrelationMath.Pearson(rx, ry),
                Observations = data.N,
                KeptRowIndices = data.KeptRows
            };
        }

        [AdvancedAnalyticsTool("CheckLinearRegressionAssumptions")]
        [Description(
            "Runs the standard assumption checks on a linear regression in one call and reports each as passed or failed at the 5 percent level, with a plain-language summary. " +
            "Checks: Linearity (Ramsey RESET, p-value above 0.05 passes), Independence (Breusch-Godfrey order 1 and Durbin-Watson between 1.5 and 2.5; only meaningful when rows are in time order), Constant variance (studentised Breusch-Pagan), Normality of residuals (Jarque-Bera; Shapiro-Wilk when available), Multicollinearity (largest VIF below 10) and Influence (no row with Cook's distance above 1 and fewer than 5 percent of rows above 4/n). " +
            "Each check has its statistic, p-value where applicable and a comment. A failed check tells you which of RegressionInfluenceDiagnostics, VarianceInflationFactors, ResidualNormalityTests or the specific test tools to run next. Checks that cannot be computed (too few rows, collinear terms) are reported as not passed with a comment saying why.")]
        public AssumptionReport CheckLinearRegressionAssumptions(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Intercept)]
            bool includeIntercept = true,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            var context =
                DiagnosticsCore.Prepare(inputs, outputs, variableNames, includeIntercept, dropIncompleteRows, 8);

            var fit = context.Fit;
            var checks = new List<AssumptionCheck>();

            Safe(checks, "Linearity", "Ramsey RESET", () =>
            {
                var reset = RamseyResetTest(inputs, outputs, 3, variableNames, includeIntercept, dropIncompleteRows);

                return (reset.Statistic, reset.PValue, reset.PValue >= 0.05,
                    reset.PValue >= 0.05
                        ? "No sign of omitted curvature."
                        : "Evidence of non-linearity or omitted variables.");
            });

            Safe(checks, "Independence", "Breusch-Godfrey (order 1)", () =>
            {
                var bg = BreuschGodfreySerialCorrelationTest(inputs, outputs, 1, variableNames, includeIntercept,
                    dropIncompleteRows);

                return (bg.Statistic, bg.PValue, bg.PValue >= 0.05,
                    bg.PValue >= 0.05
                        ? "No serial correlation detected (assumes rows are in time order)."
                        : "Serial correlation in residuals.");
            });

            var dw = LinearReporting.DurbinWatson(fit.Residuals);

            checks.Add(new AssumptionCheck
            {
                Assumption = "Independence",
                Test = "Durbin-Watson",
                Statistic = dw,
                PValue = double.NaN,
                Passed = dw >= 1.5 && dw <= 2.5,
                Comment = dw >= 1.5 && dw <= 2.5
                    ? "Durbin-Watson is between 1.5 and 2.5."
                    : "Durbin-Watson is outside 1.5 to 2.5, suggesting serial correlation."
            });

            Safe(checks, "Constant variance", "Breusch-Pagan (studentised)", () =>
            {
                var bp = DiagnosticsCore.BreuschPagan(context, HeteroscedasticityVariant.Studentised);

                return (bp.Statistic, bp.PValue, bp.PValue >= 0.05,
                    bp.PValue >= 0.05
                        ? "No sign that the variance depends on the predictors."
                        : "Heteroscedasticity detected.");
            });

            var jb = DiagnosticsCore.JarqueBera(fit.Residuals);

            checks.Add(new AssumptionCheck
            {
                Assumption = "Normality of residuals",
                Test = "Jarque-Bera",
                Statistic = jb.Statistic,
                PValue = jb.P,
                Passed = jb.P >= 0.05,
                Comment = jb.P >= 0.05
                    ? "Skewness and kurtosis are consistent with normal residuals."
                    : "Residuals are skewed or heavy-tailed."
            });

            var vifs = LinearReporting.Vifs(context.Data.X);
            var worst = vifs.Where(v => !double.IsNaN(v)).DefaultIfEmpty(1).Max();

            checks.Add(new AssumptionCheck
            {
                Assumption = "Multicollinearity",
                Test = "Largest variance inflation factor",
                Statistic = worst,
                PValue = double.NaN,
                Passed = worst < 10,
                Comment = worst < 10 ? "All VIFs are below 10." : "At least one VIF is 10 or more."
            });

            var cooks = DiagnosticsCore.CooksDistance(fit);
            var influential = cooks.Count(c => c > 4d / fit.N) / (double)fit.N;
            var maxCook = cooks.Max();

            checks.Add(new AssumptionCheck
            {
                Assumption = "Influence",
                Test = "Cook's distance",
                Statistic = maxCook,
                PValue = double.NaN,
                Passed = maxCook < 1 && influential < 0.05,
                Comment = maxCook < 1 && influential < 0.05
                    ? "No single row dominates the fit."
                    : $"Largest Cook's distance {maxCook:0.###}; {influential:P1} of rows exceed 4/n."
            });

            var failed = checks.Count(c => !c.Passed);

            return new AssumptionReport
            {
                Checks = checks.ToArray(),
                FailedCount = failed,
                RSquared = fit.RSquared,
                Observations = fit.N,
                Summary = failed == 0
                    ? "All checked assumptions look acceptable."
                    : $"{failed} of {checks.Count} checks failed: {string.Join(", ", checks.Where(c => !c.Passed).Select(c => c.Assumption + " (" + c.Test + ")"))}."
            };
        }

        private static void Safe(List<AssumptionCheck> checks, string assumption, string test,
            Func<(double Statistic, double P, bool Passed, string Comment)> compute)
        {
            try
            {
                var r = compute();

                checks.Add(new AssumptionCheck
                {
                    Assumption = assumption,
                    Test = test,
                    Statistic = r.Statistic,
                    PValue = r.P,
                    Passed = r.Passed,
                    Comment = r.Comment
                });
            }
            catch (ArgumentException e)
            {
                checks.Add(new AssumptionCheck
                {
                    Assumption = assumption,
                    Test = test,
                    Statistic = double.NaN,
                    PValue = double.NaN,
                    Passed = false,
                    Comment = "Could not be computed: " + e.Message
                });
            }
        }

        private static double VifByRegression(double[][] rows, double[][] cols, int j, string[] names)
        {
            var independent = new List<int>();

            foreach (var c in Enumerable.Range(0, cols.Length).Where(c => c != j))
            {
                var trial = independent.Append(c).ToArray();

                try
                {
                    LinearFit.EnsureFullRank(
                        rows.Select(r => new[] { 1d }.Concat(trial.Select(t => r[t])).ToArray()).ToArray(),
                        new[] { "(Intercept)" }.Concat(trial.Select(t => names[t])).ToArray());

                    independent.Add(c);
                }
                catch (ArgumentException)
                {
                }
            }

            if (independent.Count == 0)
            {
                return 1;
            }

            var fit = LinearFit.Fit(rows.Select(r => independent.Select(c => r[c]).ToArray()).ToArray(), cols[j], null,
                independent.Select(c => names[c]).ToArray(), true);

            return 1 - fit.RSquared < 1e-12 ? double.PositiveInfinity : 1 / (1 - fit.RSquared);
        }

        private static double[][]? TryInverseCorrelation(double[][] rows)
        {
            try
            {
                var correlation = rows.Correlation();
                var singular = new JaggedSingularValueDecomposition(correlation, false, false).Diagonal;

                if (singular.Any(double.IsNaN) || !(singular.Min() > singular.Max() * 1e-12))
                {
                    return null;
                }

                return correlation.Inverse();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}