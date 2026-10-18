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
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    public enum GlmFamily
    {
        Gaussian = 0,
        Poisson = 1,
        Binomial = 2,
        Gamma = 3,
        InverseGaussian = 4
    }

    public enum GlmLink
    {
        Identity = 0,
        Log = 1,
        Logit = 2,
        Probit = 3,
        ComplementaryLogLog = 4,
        Cauchit = 5,
        Inverse = 6
    }

    [AdvancedAnalyticsToolGroup("RelationshipsAndRegression")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class GeneralisedLinearModelTools
    {
        [AdvancedAnalyticsTool("FitGeneralisedLinearModel")]
        [Description(
            "Fits a generalised linear model by iteratively reweighted least squares (Fisher scoring), which extends regression to outcomes that are counts, positive skewed amounts, binary or normal. " +
            "Pick the family for the outcome (Gaussian: any number, Poisson: counts of 0 or more, Binomial: 0/1 outcomes, Gamma: positive skewed amounts such as claim sizes, InverseGaussian: positive, heavier right tail) and a link connecting the mean to the linear predictor (Identity, Log, Logit, Probit, ComplementaryLogLog, Cauchit, Inverse). Leave the link empty for the family default: Gaussian Identity, Poisson Log, Binomial Logit, Gamma and InverseGaussian Log. " +
            "The result has a coefficient table (estimate on the link scale, standard error, Wald z or t statistic and p-value, confidence interval, and the exponentiated estimate with interval, which is a rate ratio for Poisson with Log link, an odds ratio for Binomial with Logit, and a multiplicative effect for Gamma with Log), the deviance and null deviance, the deviance based pseudo R squared 1 - deviance/null deviance, the Pearson dispersion (1 means the fixed-variance families fit; well above 1 means overdispersion), log-likelihood, AIC, BIC, a likelihood ratio test of the whole model against intercept only, Converged and Warnings. " +
            "Poisson and Binomial have fixed dispersion 1 unless estimateDispersion is true (quasi-likelihood, which scales standard errors by the Pearson dispersion); the other families always estimate it and use t statistics. An offset (for example the log of exposure time for rates) is added to the linear predictor with coefficient fixed at 1. An intercept is always fitted. For an ordinary logistic model with regularisation and calibration use TrainLogisticRegression instead.")]
        public GeneralisedLinearModelResult FitGeneralisedLinearModel(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(
                "The outcome for each row: counts for Poisson, 0/1 for Binomial, positive numbers for Gamma and InverseGaussian, any number for Gaussian. NaN marks a missing value.")]
            double[] outputs,
            [Description(
                "The distribution family of the outcome: Gaussian, Poisson, Binomial, Gamma or InverseGaussian.")]
            GlmFamily family,
            [Description("The link function; leave empty for the family default described in the tool description.")]
            GlmLink? link = null,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(
                "Optional prior weight per row (for example 1 for a normal row, or a case weight). Each must be greater than zero.")]
            double[]? weights = null,
            [Description(
                "Optional offset per row added to the linear predictor, on the link scale, for example the natural log of exposure for Poisson rates. NaN marks a missing value.")]
            double[]? offset = null,
            [Description(
                "True scales standard errors by the Pearson dispersion for Poisson or Binomial (quasi-likelihood, use for overdispersed data). Other families always estimate it. Default false.")]
            bool estimateDispersion = false,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description("Maximum fitting iterations, from 5 to 1000. Default 100.")]
            int maximumIterations = 100,
            [Description("True also returns fitted means and deviance residuals for every row. Default false.")]
            bool includeFittedAndResiduals = false,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            return FitCore(inputs, outputs, family, link ?? GlmLinkFunction.DefaultFor(family), variableNames, weights,
                offset, estimateDispersion, confidenceLevel, maximumIterations, includeFittedAndResiduals,
                dropIncompleteRows);
        }

        [AdvancedAnalyticsTool("FitPoissonRegression")]
        [Description(
            "Fits a Poisson regression with a log link for a count outcome (events per customer, alerts per day, claims per policy) and reports each predictor's effect as a rate ratio. " +
            "In the coefficient table ExponentiatedEstimate is the multiplicative change in the expected count per one-unit increase in the predictor with the others held fixed (1.10 means 10 percent more events), with its confidence interval; a p-value below 0.05 means the effect is distinguishable from no effect. " +
            "Give exposure (time at risk, population, number of opportunities; every value above 0) to model a rate instead of a raw count, which adds its natural log as an offset. Dispersion (Pearson chi-square over residual degrees of freedom) well above 1 means overdispersion, so the standard errors are too small: refit with estimateDispersion true. Also returned: deviance, null deviance, pseudo R squared, AIC, BIC and a likelihood ratio test. See FitGeneralisedLinearModel for other families.")]
        public GeneralisedLinearModelResult FitPoissonRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description("The count outcome for each row, whole numbers of 0 or more. NaN marks a missing value.")]
            double[] counts,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(
                "Optional exposure per row (time at risk, population size), each above 0; the model then describes counts per unit of exposure. Leave empty for raw counts.")]
            double[]? exposure = null,
            [Description(
                "True scales standard errors by the Pearson dispersion (quasi-Poisson) for overdispersed counts. Default false.")]
            bool estimateDispersion = false,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description("True also returns fitted means and deviance residuals for every row. Default false.")]
            bool includeFittedAndResiduals = false,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            double[]? offset = null;

            if (exposure != null)
            {
                if (exposure.Any(e => !double.IsNaN(e) && !(e > 0)))
                {
                    throw new ArgumentException("Every exposure must be greater than 0.", nameof(exposure));
                }

                offset = exposure.Select(e => Math.Log(e)).ToArray();
            }

            return FitCore(inputs, counts, GlmFamily.Poisson, GlmLink.Log, variableNames, null, offset,
                estimateDispersion, confidenceLevel, 100, includeFittedAndResiduals, dropIncompleteRows, "counts",
                nameof(exposure));
        }

        [AdvancedAnalyticsTool("FitGammaRegression")]
        [Description(
            "Fits a Gamma regression for a strictly positive, right-skewed outcome whose spread grows with its mean (claim or transaction amounts, durations, costs). The standard deviation is assumed proportional to the mean. " +
            "With the default Log link, ExponentiatedEstimate is the multiplicative change in the expected outcome per one-unit increase in the predictor (1.05 means 5 percent higher). The Inverse link is the canonical Gamma link (the mean is 1 over the linear predictor) and Identity models additive effects but can produce non-positive means, which stops the fit. " +
            "The dispersion (Pearson chi-square over residual degrees of freedom, equal to the squared coefficient of variation) is estimated and t statistics are used. Also returned: deviance, pseudo R squared, AIC, BIC and Warnings. Every outcome must be above 0.")]
        public GeneralisedLinearModelResult FitGammaRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description("The strictly positive outcome for each row. NaN marks a missing value.")]
            double[] outputs,
            [Description("Log (default), Inverse (canonical) or Identity link; other links are also accepted.")]
            GlmLink link = GlmLink.Log,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description("True also returns fitted means and deviance residuals for every row. Default false.")]
            bool includeFittedAndResiduals = false,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            return FitCore(inputs, outputs, GlmFamily.Gamma, link, variableNames, null, null, true, confidenceLevel,
                100,
                includeFittedAndResiduals, dropIncompleteRows);
        }

        [AdvancedAnalyticsTool("FitProbitRegression")]
        [Description(
            "Fits a probit regression, a binary-outcome model in which the probability is the standard normal distribution function of a linear predictor: P(y=1) = Phi(intercept + sum of coefficient times predictor). " +
            "Coefficients are on the probit (z-score) scale, so a positive value means the predictor raises the probability; they are roughly 0.55 times the corresponding logistic coefficients. Each row reports the estimate, standard error, Wald z, p-value and confidence interval; the model also gets deviance, null deviance, pseudo R squared, AIC, BIC, a likelihood ratio test and warnings if the classes are perfectly separated. " +
            "Probit and logit rarely differ in fit; use it when a latent normal threshold interpretation is wanted. No held-out evaluation or calibration is done here; for a scored, evaluated model use TrainLogisticRegression.")]
        public GeneralisedLinearModelResult FitProbitRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description("The outcome for each row: 1 for the event and 0 otherwise. NaN marks a missing value.")]
            double[] outcomes,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description("True also returns fitted probabilities and deviance residuals for every row. Default false.")]
            bool includeFittedAndResiduals = false,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            return FitCore(inputs, outcomes, GlmFamily.Binomial, GlmLink.Probit, variableNames, null, null, false,
                confidenceLevel, 100, includeFittedAndResiduals, dropIncompleteRows, "outcomes");
        }

        [AdvancedAnalyticsTool("FitComplementaryLogLogRegression")]
        [Description(
            "Fits a complementary log-log regression for a binary outcome: P(y=1) = 1 - exp(-exp(linear predictor)). Unlike logit and probit the curve is asymmetric, approaching 1 more slowly than it leaves 0, which suits rare events and discrete-time hazard or 'at least one event occurred' data. " +
            "Coefficients are on the log-log scale; exp(coefficient) is a hazard ratio for the event over the observation window. Each row reports the estimate, standard error, Wald z, p-value, confidence interval and exponentiated estimate; the model also gets deviance, null deviance, pseudo R squared, AIC, BIC, a likelihood ratio test and warnings. " +
            "Use it when the event is rare or the probabilities near 0 and 1 behave asymmetrically; compare its AIC with FitProbitRegression and TrainLogisticRegression.")]
        public GeneralisedLinearModelResult FitComplementaryLogLogRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description("The outcome for each row: 1 for the event and 0 otherwise. NaN marks a missing value.")]
            double[] outcomes,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description("True also returns fitted probabilities and deviance residuals for every row. Default false.")]
            bool includeFittedAndResiduals = false,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            return FitCore(inputs, outcomes, GlmFamily.Binomial, GlmLink.ComplementaryLogLog, variableNames, null, null,
                false, confidenceLevel, 100, includeFittedAndResiduals, dropIncompleteRows, "outcomes");
        }

        [AdvancedAnalyticsTool("PredictWithGeneralisedLinearModel")]
        [Description(
            "Fits a generalised linear model to the training rows and predicts for new rows with uncertainty. " +
            "For each new row it returns the linear predictor and its standard error, the predicted mean on the outcome scale (expected count, expected amount or probability) and a confidence interval for that mean obtained by transforming the interval on the link scale, which keeps probabilities within 0 and 1 and counts positive. The interval is for the mean response, not for an individual outcome. " +
            "Family, link, weights, offset and estimateDispersion have the meaning given in FitGeneralisedLinearModel; supply newOffset (same scale as offset) when the model was fitted with one. The model is refitted on every call and nothing is stored.")]
        public GlmPredictionResult PredictWithGeneralisedLinearModel(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(
                "The outcome for each training row, in the form the family needs (counts, 0/1, positive amounts). NaN marks a missing value.")]
            double[] outputs,
            [Description(
                "New rows to predict for: one array per case with the same columns and order as the training predictors.")]
            double[][] newInputs,
            [Description(
                "The distribution family of the outcome: Gaussian, Poisson, Binomial, Gamma or InverseGaussian.")]
            GlmFamily family,
            [Description("The link function; leave empty for the family default.")]
            GlmLink? link = null,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description("Optional prior weight per training row; each must be greater than zero.")]
            double[]? weights = null,
            [Description("Optional offset per training row on the link scale, for example the log of exposure.")]
            double[]? offset = null,
            [Description(
                "Optional offset per new row on the link scale; required when offset was given, otherwise leave empty.")]
            double[]? newOffset = null,
            [Description(
                "True scales standard errors by the Pearson dispersion for Poisson or Binomial. Default false.")]
            bool estimateDispersion = false,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));
            var chosenLink = link ?? GlmLinkFunction.DefaultFor(family);

            var input = PrepareGlm(inputs, outputs, variableNames, weights, offset, dropIncompleteRows, "outputs",
                nameof(offset));

            var p = input.Data.P;

            if (newInputs == null || newInputs.Length == 0)
            {
                throw new ArgumentException("newInputs is required: at least one row of predictor values.",
                    nameof(newInputs));
            }

            for (var i = 0; i < newInputs.Length; i++)
            {
                if (newInputs[i] == null || newInputs[i].Length != p ||
                    newInputs[i].Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new ArgumentException(
                        $"newInputs row {i} must have {p} finite value(s), the same columns as the training predictors.",
                        nameof(newInputs));
                }
            }

            if (input.Offset != null && (newOffset == null || newOffset.Length != newInputs.Length))
            {
                throw new ArgumentException(
                    "The model was fitted with an offset, so newOffset must give one value per new row.",
                    nameof(newOffset));
            }

            if (newOffset != null && newOffset.Length != newInputs.Length)
            {
                throw new ArgumentException("newOffset must have one value per new row.", nameof(newOffset));
            }

            var solution = GlmCore.Fit(input.Data.X, input.Data.Y, input.Data.Names, input.Data.Weights, input.Offset,
                family, chosenLink, estimateDispersion, 100);

            var linkFunction = GlmLinkFunction.For(chosenLink);

            var quantile = solution.DispersionEstimated
                ? RegressionStats.TQuantile(0.5 + confidenceLevel / 2, solution.N - solution.K)
                : RegressionStats.NormalQuantile(0.5 + confidenceLevel / 2);

            var rows = new GlmPredictionRow[newInputs.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var z = new[] { 1d }.Concat(newInputs[i]).ToArray();
                var eta = z.Zip(solution.Beta, (a, b) => a * b).Sum() + (newOffset?[i] ?? 0);
                var q = 0d;

                for (var a = 0; a < z.Length; a++)
                {
                    for (var b = 0; b < z.Length; b++)
                    {
                        q += z[a] * solution.Covariance[a][b] * z[b];
                    }
                }

                var se = Math.Sqrt(Math.Max(0, q));
                var lower = linkFunction.Inverse(eta - quantile * se);
                var upper = linkFunction.Inverse(eta + quantile * se);

                rows[i] = new GlmPredictionRow
                {
                    Input = newInputs[i],
                    LinearPredictor = eta,
                    LinearPredictorStandardError = se,
                    Mean = linkFunction.Inverse(eta),
                    MeanLower = Math.Min(lower, upper),
                    MeanUpper = Math.Max(lower, upper)
                };
            }

            return new GlmPredictionResult
            {
                Predictions = rows,
                Family = family.ToString(),
                Link = chosenLink.ToString(),
                ConfidenceLevel = confidenceLevel,
                Converged = solution.Converged
            };
        }

        [AdvancedAnalyticsTool("ApplyLinkFunction")]
        [Description(
            "Transforms a list of numbers with one of the regression link functions, or undoes the transform. " +
            "With inverse false the values are means or probabilities and the result is the link (linear predictor) scale: Identity x, Log ln x (x above 0), Logit ln(x/(1-x)), Probit the standard normal quantile, Cauchit tan(pi*(x-0.5)) and ComplementaryLogLog ln(-ln(1-x)) (all with x strictly between 0 and 1), Inverse 1/x (x not 0). " +
            "With inverse true the values are linear-predictor scores and the result is the mean scale: Log gives exp(x), Logit gives 1/(1+exp(-x)) (the logistic function), Probit gives the standard normal distribution function, ComplementaryLogLog gives 1-exp(-exp(x)), Cauchit gives atan(x)/pi + 0.5. " +
            "Values outside the domain become NaN and are counted in InvalidCount. Use it to convert a linear score to a probability or to see what a coefficient means on the outcome scale.")]
        public LinkTransformResult ApplyLinkFunction(
            [Description(
                "The numbers to transform, either means or probabilities (inverse false) or linear-predictor scores (inverse true).")]
            double[] values,
            [Description("Identity, Log, Logit, Probit, ComplementaryLogLog, Cauchit or Inverse.")]
            GlmLink link,
            [Description(
                "False applies the link (mean to score); true applies its inverse (score to mean). Default false.")]
            bool inverse = false)
        {
            RegressionInput.RequireArray(values, nameof(values), 1);

            if (!Enum.IsDefined(typeof(GlmLink), link))
            {
                throw new ArgumentException(
                    "Unknown link. Use Identity, Log, Logit, Probit, ComplementaryLogLog, Cauchit or Inverse.",
                    nameof(link));
            }

            var function = GlmLinkFunction.For(link);
            var invalid = 0;
            var result = new double[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i];

                var domainOk = inverse
                    ? !double.IsNaN(v) && !double.IsInfinity(v)
                    : link switch
                    {
                        GlmLink.Identity => !double.IsNaN(v) && !double.IsInfinity(v),
                        GlmLink.Log => v > 0 && !double.IsInfinity(v),
                        GlmLink.Inverse => v != 0 && !double.IsNaN(v) && !double.IsInfinity(v),
                        _ => v > 0 && v < 1
                    };

                var value = domainOk ? inverse ? function.Inverse(v) : function.Link(v) : double.NaN;

                if (!domainOk || double.IsNaN(value) || double.IsInfinity(value))
                {
                    result[i] = double.NaN;
                    invalid++;
                }
                else
                {
                    result[i] = value;
                }
            }

            return new LinkTransformResult
            {
                Link = link.ToString(),
                Direction = inverse ? "Inverse (score to mean)" : "Link (mean to score)",
                Values = result,
                InvalidCount = invalid
            };
        }

        private static GlmInput PrepareGlm(double[][] inputs, double[] outputs, string[]? names, double[]? weights,
            double[]? offset, bool drop, string outputsName, string offsetName)
        {
            if (offset != null && outputs != null && offset.Length != outputs.Length)
            {
                throw new ArgumentException(
                    $"{offsetName} has {offset.Length} value(s) but there are {outputs.Length} row(s). Supply one per row.",
                    offsetName);
            }

            var rowIndexed = outputs == null
                ? outputs
                : outputs.Select((v, i) => offset != null && double.IsNaN(offset[i]) ? double.NaN : v).ToArray();

            var data = RegressionInput.Prepare(inputs, rowIndexed, names, weights, drop, 4, nameof(inputs),
                outputsName);

            var kept = offset == null ? null : data.KeptRows.Select(i => offset[i]).ToArray();

            if (kept != null && kept.Any(double.IsInfinity))
            {
                throw new ArgumentException($"{offsetName} contains an infinite value.", offsetName);
            }

            return new GlmInput { Data = data, Offset = kept };
        }

        private static GeneralisedLinearModelResult FitCore(double[][] inputs, double[] outputs, GlmFamily family,
            GlmLink link, string[]? names, double[]? weights, double[]? offset, bool estimateDispersion, double level,
            int maximumIterations, bool includeSeries, bool drop, string outputsName = "outputs",
            string offsetName = "offset")
        {
            RegressionInput.RequireLevel(level, "confidenceLevel");

            if (maximumIterations < 5 || maximumIterations > 1000)
            {
                throw new ArgumentException("maximumIterations must be between 5 and 1000.", nameof(maximumIterations));
            }

            if (!Enum.IsDefined(typeof(GlmLink), link))
            {
                throw new ArgumentException(
                    "Unknown link. Use Identity, Log, Logit, Probit, ComplementaryLogLog, Cauchit or Inverse.",
                    nameof(link));
            }

            var input = PrepareGlm(inputs, outputs, names, weights, offset, drop, outputsName, offsetName);

            var solution = GlmCore.Fit(input.Data.X, input.Data.Y, input.Data.Names, input.Data.Weights, input.Offset,
                family, link, estimateDispersion, maximumIterations, true, outputsName);

            return GlmCore.Report(solution, family, link, level, includeSeries, input.Data.Dropped);
        }
    }
}