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
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    public enum NonlinearCurveFamily
    {
        Exponential = 0,
        AsymptoticExponential = 1,
        SaturatingExponential = 2,
        Power = 3,
        Logarithmic = 4,
        Logistic3 = 5,
        Logistic4 = 6,
        Gompertz = 7,
        MichaelisMenten = 8,
        Hill = 9,
        GaussianPeak = 10,
        Reciprocal = 11
    }

    [AdvancedAnalyticsToolGroup("RelationshipsAndRegression")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class NonlinearCurveTools
    {
        [AdvancedAnalyticsTool("FitNonlinearCurve")]
        [Description(
            "Fits a named non-linear curve y = f(x) to data by non-linear least squares (Levenberg-Marquardt) and reports parameter estimates with inference. " +
            "Choose family by the shape: Exponential (growth or decay, scale*exp(rate*x)), AsymptoticExponential (approach to a ceiling or floor: asymptote - amplitude*exp(-rate*x)), SaturatingExponential (maximum*(1-exp(-rate*x)) through the origin), Power (scale*x^exponent, x above 0), Logarithmic (intercept + slope*ln x), Logistic3 (S-curve with maximum, steepness and midpoint, from 0 to the maximum), Logistic4 (S-curve between a lower and an upper level), Gompertz (asymmetric S-curve), MichaelisMenten (saturating hyperbola maximumRate*x/(halfSaturation+x)), Hill (steeper saturating curve with a coefficient), GaussianPeak (bell-shaped peak) and Reciprocal (1/(intercept+slope*x)). The Formula field shows the exact equation. " +
            "Each parameter row has the estimate, standard error, t statistic, p-value and confidence interval (from the Gauss-Newton covariance sigma^2 (J'J)^-1, an approximation that is poor in small or strongly curved samples). Also returned: residual sum of squares, residual standard error, R squared, adjusted R squared, Gaussian AIC and BIC, a Converged flag and Warnings. " +
            "Starting values are found automatically; supply startValues (in the order of the parameter names in the Formula) if the fit fails or lands on a poor local minimum. Non-linear fits can have several local minima, so plot or compare families with CompareNonlinearCurveFamilies. Optionally give predictAt to get predictions with delta-method intervals.")]
        public NonlinearCurveResult FitNonlinearCurve(
            [Description(RegressionText.X)] double[] x,
            [Description(RegressionText.Y)] double[] y,
            [Description("The curve shape to fit; see the tool description for what each family means.")]
            NonlinearCurveFamily family,
            [Description(
                "Optional starting values for the parameters, in the order shown in the Formula. Leave empty for automatic starts.")]
            double[]? startValues = null,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description(
                "Optional x values at which to predict, returned in Predictions with intervals. Leave empty for none.")]
            double[]? predictAt = null,
            [Description("Maximum optimiser iterations, from 10 to 100000. Default 1000.")]
            int maximumIterations = 1000,
            [Description(RegressionText.Series)] bool includeFittedAndResiduals = false,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));

            if (maximumIterations < 10 || maximumIterations > 100000)
            {
                throw new ArgumentException("maximumIterations must be between 10 and 100000.",
                    nameof(maximumIterations));
            }

            var definition = CurveDefinition.For(family);
            RegressionInput.RequireSameLength(x, y, nameof(x), nameof(y));

            var data = RegressionInput.Prepare(x.Select(v => new[] { v }).ToArray(), y, new[] { "x" }, null,
                dropIncompleteRows, 3, nameof(x), nameof(y));

            var xs = data.X.Select(r => r[0]).ToArray();

            return CurveFitting.Fit(definition, xs, data.Y, startValues, maximumIterations, confidenceLevel,
                includeFittedAndResiduals, predictAt, data.Dropped);
        }

        [AdvancedAnalyticsTool("CompareNonlinearCurveFamilies")]
        [Description(
            "Fits several named curve families to the same x and y and ranks them, to find which shape describes the data best. " +
            "For each family it returns whether it could be fitted (with a reason when not, for example x must be above 0 for Power and Hill), R squared, residual standard error, AIC, BIC, the Akaike weight (relative support among the fitted families, summing to 1) and the fitted parameters; Rank 1 is the lowest AIC. " +
            "BestByAic and BestByBic name the winners; a difference in AIC under about 2 means the families are practically indistinguishable, so prefer the simpler or more plausible one. Automatic starting values are used, so a family may fail or find a local minimum; refit the finalists with FitNonlinearCurve. Leave families empty to try every family that suits the data.")]
        public CurveFamilyComparisonResult CompareNonlinearCurveFamilies(
            [Description(RegressionText.X)] double[] x,
            [Description(RegressionText.Y)] double[] y,
            [Description("The families to compare. Leave empty to try all twelve.")]
            NonlinearCurveFamily[]? families = null,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            RegressionInput.RequireSameLength(x, y, nameof(x), nameof(y));

            var data = RegressionInput.Prepare(x.Select(v => new[] { v }).ToArray(), y, new[] { "x" }, null,
                dropIncompleteRows, 4, nameof(x), nameof(y));

            var xs = data.X.Select(r => r[0]).ToArray();

            var chosen = families == null || families.Length == 0
                ? Enum.GetValues<NonlinearCurveFamily>()
                : families.Distinct().ToArray();

            var rows = new List<CurveFamilyComparisonRow>();

            foreach (var family in chosen)
            {
                var definition = CurveDefinition.For(family);

                try
                {
                    var fit = CurveFitting.Fit(definition, xs, data.Y, null, 2000, 0.95, false, null, 0);

                    rows.Add(new CurveFamilyComparisonRow
                    {
                        Family = family.ToString(),
                        Fitted = true,
                        Note = fit.Converged ? string.Empty : "Did not fully converge; treat with caution.",
                        RSquared = fit.RSquared,
                        ResidualStandardError = fit.ResidualStandardError,
                        Aic = fit.Aic,
                        Bic = fit.Bic,
                        Parameters = fit.Parameters.Select(q => q.Estimate).ToArray(),
                        ParameterNames = definition.Parameters
                    });
                }
                catch (ArgumentException e)
                {
                    rows.Add(new CurveFamilyComparisonRow
                    {
                        Family = family.ToString(),
                        Fitted = false,
                        Note = e.Message,
                        RSquared = double.NaN,
                        ResidualStandardError = double.NaN,
                        Aic = double.NaN,
                        Bic = double.NaN,
                        ParameterNames = definition.Parameters
                    });
                }
            }

            var ok = rows.Where(r => r.Fitted && !double.IsNaN(r.Aic)).ToList();

            if (ok.Count == 0)
            {
                throw new ArgumentException(
                    "None of the requested families could be fitted to this data. Check the range of x and y, or use FitNonlinearCurve with startValues.",
                    nameof(families));
            }

            var best = ok.Min(r => r.Aic);
            var weights = ok.ToDictionary(r => r.Family, r => Math.Exp(-(r.Aic - best) / 2));
            var total = weights.Values.Sum();
            var ranked = rows.OrderBy(r => r.Fitted && !double.IsNaN(r.Aic) ? r.Aic : double.MaxValue).ToList();

            return new CurveFamilyComparisonResult
            {
                Families = ranked.Select((r, i) => r with
                {
                    Rank = r.Fitted && !double.IsNaN(r.Aic) ? i + 1 : 0,
                    AkaikeWeight = weights.TryGetValue(r.Family, out var w) ? w / total : 0
                }).ToArray(),
                BestByAic = ok.OrderBy(r => r.Aic).First().Family,
                BestByBic = ok.OrderBy(r => r.Bic).First().Family,
                Observations = data.N
            };
        }
    }
}