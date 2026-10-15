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

using Accord.Statistics;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal static class DiagnosticsCore
    {
        public static DiagnosticContext Prepare(double[][] inputs, double[] outputs, string[]? names, bool intercept,
            bool drop, int minimumRows = 5)
        {
            var data = RegressionInput.Prepare(inputs, outputs, names, null, drop, minimumRows);

            return new DiagnosticContext
                { Data = data, Fit = LinearFit.Fit(data.X, data.Y, null, data.Names, intercept) };
        }

        public static (double R2, double Lm, int Columns) Auxiliary(double[] response, double[][] columns)
        {
            var fit = LinearFit.Fit(columns, response, null,
                Enumerable.Range(1, columns[0].Length).Select(i => "a" + i).ToArray(), true);

            return (fit.RSquared, fit.N * fit.RSquared, columns[0].Length);
        }

        public static DiagnosticTestResult Lm(string test, string nullHypothesis, double lm, int df, double r2, int n,
            string rejectText, string keepText)
        {
            var p = RegressionStats.ChiSquareUpperTail(lm, df);

            return new DiagnosticTestResult
            {
                Test = test,
                Statistic = lm,
                DegreesOfFreedom = df,
                DegreesOfFreedom2 = double.NaN,
                PValue = p,
                Distribution = "ChiSquare",
                NullHypothesis = nullHypothesis,
                Conclusion = p < 0.05 ? rejectText : keepText,
                Observations = n,
                AuxiliaryRSquared = r2,
                Estimate1 = double.NaN,
                Estimate2 = double.NaN
            };
        }

        public static double[] StandardisedResiduals(LinearFit fit)
        {
            return fit.Residuals.Select((e, i) => e / (fit.Sigma * Math.Sqrt(Math.Max(1e-12, 1 - fit.Leverage[i]))))
                .ToArray();
        }

        public static double[] ExternallyStudentised(LinearFit fit)
        {
            var result = new double[fit.N];

            for (var i = 0; i < fit.N; i++)
            {
                var oneMinusH = Math.Max(1e-12, 1 - fit.Leverage[i]);
                var s2 = (fit.Rss - fit.Residuals[i] * fit.Residuals[i] / oneMinusH) / (fit.DfResidual - 1);

                result[i] = fit.DfResidual > 1 && s2 > 0
                    ? fit.Residuals[i] / Math.Sqrt(s2 * oneMinusH)
                    : double.NaN;
            }

            return result;
        }

        public static double[] CooksDistance(LinearFit fit)
        {
            var standardised = StandardisedResiduals(fit);

            return standardised
                .Select((r, i) => r * r * fit.Leverage[i] / (fit.K * Math.Max(1e-12, 1 - fit.Leverage[i])))
                .ToArray();
        }

        public static double FirstOrderAutocorrelation(double[] residuals)
        {
            var denominator = residuals.Sum(e => e * e);
            var numerator = 0d;

            for (var i = 1; i < residuals.Length; i++)
            {
                numerator += residuals[i] * residuals[i - 1];
            }

            return denominator > 0 ? numerator / denominator : double.NaN;
        }

        public static (double Statistic, double P, int N, double Skew, double Kurt) JarqueBera(double[] values)
        {
            var n = values.Length;
            var skew = values.Skewness(false);
            var kurt = values.Kurtosis(false);
            var jb = n / 6d * (skew * skew + kurt * kurt / 4);

            return (jb, RegressionStats.ChiSquareUpperTail(jb, 2), n, skew, kurt);
        }

        public static DiagnosticTestResult BreuschPagan(DiagnosticContext context, HeteroscedasticityVariant variant)
        {
            var fit = context.Fit;
            var squared = fit.Residuals.Select(e => e * e).ToArray();
            var columns = context.Data.X;
            var aux = LinearFit.Fit(columns, squared, null, context.Data.Names, true);
            var df = columns[0].Length;
            double lm;

            if (variant == HeteroscedasticityVariant.Classical)
            {
                var sigma2 = fit.Rss / fit.N;
                var explained = aux.Tss - aux.Rss;
                lm = explained / (2 * sigma2 * sigma2);
            }
            else
            {
                lm = fit.N * aux.RSquared;
            }

            return Lm(
                variant == HeteroscedasticityVariant.Classical
                    ? "Breusch-Pagan"
                    : "Breusch-Pagan (Koenker studentised)",
                "The residual variance is constant (homoscedastic).", lm, df, aux.RSquared, fit.N,
                "Evidence that the residual variance depends on the predictors (heteroscedasticity); consider robust standard errors, a transformation of the outcome or weighted least squares.",
                "No significant evidence that the residual variance depends on the predictors.");
        }
    }
}