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
    public class MixtureModelTools
    {
        private const int MaximumValues = 50000;
        private const string ValuesText = "The observations, at least 10 numbers, no missing values, at most 50000.";

        private const string SeedText =
            "Seed for the random restarts of the fit; reuse it to reproduce the result. Default 1.";

        [AdvancedAnalyticsTool("FitGaussianMixture1D")]
        [Description(
            "Fits a one dimensional Gaussian mixture model: the data is described as coming from several bell shaped groups, each with its own mean, standard deviation and share (weight). " +
            "Use it to find hidden sub populations, for example separate customer segments in an amount or count distribution. " +
            "It is fitted by expectation maximisation from several starting points (the first spreads the components over the data quantiles, the rest are random) and the best log-likelihood is kept. " +
            "Returns the components ordered by mean with their weight, mean, standard deviation and the number of values assigned to each, the log-likelihood, AIC and BIC (lower is better), a hard assignment of every value to its most likely component and, if requested, the full responsibilities (probability of each component per value). " +
            "Use SelectGaussianMixtureComponents to choose how many components to use.")]
        public GaussianMixtureResult FitGaussianMixture1D(
            [Description(ValuesText)] double[] values,
            [Description(
                "The number of Gaussian components, between 1 and 10 and at most a fifth of the number of values.")]
            int components,
            [Description(SeedText)] int seed = 1,
            [Description(
                "Most expectation maximisation iterations per starting point, between 10 and 2000. Default 300.")]
            int maxIterations = 300,
            [Description(
                "Relative log-likelihood change below which iteration stops, between 1e-12 and 0.1. Default 1e-7.")]
            double tolerance = 1e-7,
            [Description("Number of starting points to try, between 1 and 20. Default 5.")]
            int restarts = 5,
            [Description(
                "When true the response also contains the probability of each component for each value (values times components numbers). Default false.")]
            bool returnResponsibilities = false)
        {
            Validate(values, components, maxIterations, tolerance, restarts);
            var entry = DistributionCatalogue.Get(DistributionKind.Normal);
            var fit = MixtureEngine.Fit(entry, values, components, seed, maxIterations, tolerance, restarts);
            var responsibilities = MixtureEngine.Responsibilities(entry, fit.Weights, fit.Parameters, values);
            var assignments = MixtureEngine.Assign(responsibilities);
            var notes = new List<string>();
            var overall = HistogramEngine.StandardDeviation(values);

            var summaries = new GaussianMixtureComponent[components];

            for (var j = 0; j < components; j++)
            {
                summaries[j] = new GaussianMixtureComponent(j, fit.Weights[j], fit.Parameters[j][0],
                    fit.Parameters[j][1],
                    assignments.Count(a => a == j));

                if (fit.Parameters[j][1] < 1e-4 * overall)
                {
                    notes.Add(
                        $"Component {j} has almost no spread: it may be fitting a few repeated values. Try fewer components.");
                }
            }

            if (!fit.Converged)
            {
                notes.Add(
                    "The best fit hit the iteration limit before converging; raise maxIterations for a tighter result.");
            }

            var k = 3 * components - 1;
            var n = values.Length;

            return new GaussianMixtureResult(
                n,
                components,
                summaries,
                fit.LogLikelihood,
                k,
                2 * k - 2 * fit.LogLikelihood,
                k * Math.Log(n) - 2 * fit.LogLikelihood,
                assignments,
                returnResponsibilities ? responsibilities : null,
                fit.Converged,
                notes.ToArray());
        }

        [AdvancedAnalyticsTool("SelectGaussianMixtureComponents")]
        [Description(
            "Fits Gaussian mixtures with 1, 2, 3 and so on up to a maximum number of components and reports the log-likelihood, AIC and BIC of each so you can choose how many groups the data really has. " +
            "The best number by BIC (which penalises complexity more and usually recommends fewer components) and by AIC are returned; when they disagree prefer BIC unless you have plenty of data. " +
            "A big fall in BIC from k to k+1 followed by little change suggests k+1 groups. Rows whose fit failed are omitted with a note.")]
        public MixtureSelectionResult SelectGaussianMixtureComponents(
            [Description(ValuesText)] double[] values,
            [Description("The largest number of components to try, between 1 and 10.")]
            int maximumComponents = 5,
            [Description(SeedText)] int seed = 1,
            [Description("Number of starting points per fit, between 1 and 20. Default 3.")]
            int restarts = 3)
        {
            DistributionGuard.Values(values, nameof(values), 10);

            if (maximumComponents < 1 || maximumComponents > 10)
            {
                throw new ArgumentException($"maximumComponents must be between 1 and 10, got {maximumComponents}.",
                    nameof(maximumComponents));
            }

            var rows = new List<MixtureSelectionRow>();
            var notes = new List<string>();

            for (var k = 1; k <= maximumComponents; k++)
            {
                if (k > values.Length / 5)
                {
                    notes.Add($"Stopped at {k - 1} components because there are too few values for more.");
                    break;
                }

                try
                {
                    Validate(values, k, 300, 1e-7, restarts);
                    var entry = DistributionCatalogue.Get(DistributionKind.Normal);
                    var fit = MixtureEngine.Fit(entry, values, k, seed, 300, 1e-7, restarts);
                    var p = 3 * k - 1;

                    rows.Add(new MixtureSelectionRow(k, fit.LogLikelihood, p, 2 * p - 2 * fit.LogLikelihood,
                        p * Math.Log(values.Length) - 2 * fit.LogLikelihood, fit.Converged));
                }
                catch (ArgumentException ex)
                {
                    notes.Add($"{k} components: {ex.Message}");
                }
            }

            if (rows.Count == 0)
            {
                throw new ArgumentException("No mixture could be fitted to these values. " + string.Join(" ", notes),
                    nameof(values));
            }

            return new MixtureSelectionResult(
                rows.ToArray(),
                rows.OrderBy(r => r.Bic).First().Components,
                rows.OrderBy(r => r.Aic).First().Components,
                notes.ToArray());
        }

        [AdvancedAnalyticsTool("FitMixtureModel")]
        [Description(
            "Fits a mixture of several components of the same chosen non-Gaussian family, for example a mixture of Exponentials, Gamma or Lognormal distributions, using expectation maximisation. " +
            "Use it for positive or skewed data that looks like a blend of groups, where a Gaussian mixture would put components at negative values. " +
            "The family must be one that can be fitted directly (Normal, Lognormal, Exponential, Gamma, Weibull, Rayleigh, Cauchy, Gumbel, InverseGaussian, Pareto, Nakagami and similar; ListDistributions marks them and the data requirements are enforced). " +
            "Returns the weights, the parameters of each component in ListDistributions order, the log-likelihood, AIC and BIC, and a hard assignment of each value to its most likely component. Components are ordered by their first parameter.")]
        public MixtureModelResult FitMixtureModel(
            [Description(ValuesText)] double[] values,
            [Description(
                "The family of every component, for example Gamma or Lognormal. It must be fittable directly (not one that needs a numerical search).")]
            DistributionKind componentDistribution,
            [Description("The number of components, between 1 and 8 and at most a fifth of the number of values.")]
            int components,
            [Description(SeedText)] int seed = 1,
            [Description(
                "Most expectation maximisation iterations per starting point, between 10 and 2000. Default 300.")]
            int maxIterations = 300,
            [Description(
                "Relative log-likelihood change below which iteration stops, between 1e-12 and 0.1. Default 1e-7.")]
            double tolerance = 1e-7,
            [Description("Number of starting points to try, between 1 and 20. Default 5.")]
            int restarts = 5)
        {
            var entry = DistributionCatalogue.Get(componentDistribution);

            if (entry.Fit != FitMode.Library || entry.Discrete || entry.Extract == null)
            {
                throw new ArgumentException(
                    $"{entry.Name} cannot be used as a mixture component here: choose a continuous distribution that can be fitted directly, such as Normal, Lognormal, Exponential, Gamma, Weibull or Rayleigh.",
                    nameof(componentDistribution));
            }

            Validate(values, components, maxIterations, tolerance, restarts, 8);
            var problem = DistributionFitter.DataProblem(entry, values);

            if (problem != null)
            {
                throw new ArgumentException(problem, nameof(values));
            }

            var fit = MixtureEngine.Fit(entry, values, components, seed, maxIterations, tolerance, restarts);
            var responsibilities = MixtureEngine.Responsibilities(entry, fit.Weights, fit.Parameters, values);
            var perComponent = entry.Parameters.Length;
            var k = components * perComponent + components - 1;
            var notes = new List<string>();

            if (!fit.Converged)
            {
                notes.Add(
                    "The best fit hit the iteration limit before converging; raise maxIterations for a tighter result.");
            }

            return new MixtureModelResult(
                entry.Name,
                values.Length,
                fit.Weights,
                fit.Parameters,
                entry.Parameters.Select(p => p.Name).ToArray(),
                fit.LogLikelihood,
                k,
                2 * k - 2 * fit.LogLikelihood,
                k * Math.Log(values.Length) - 2 * fit.LogLikelihood,
                MixtureEngine.Assign(responsibilities),
                notes.ToArray());
        }

        [AdvancedAnalyticsTool("EvaluateMixtureDistribution")]
        [Description(
            "Evaluates a mixture of components of the same distribution family whose weights and parameters you already have, for example from FitMixtureModel or FitGaussianMixture1D. " +
            "For each point it returns the mixture density, the mixture cumulative probability and the responsibilities: the probability that the point belongs to each component given its value (the rows sum to 1). " +
            "Use it to score new values against a fitted mixture or to classify them into groups. A density is null where undefined.")]
        public MixtureEvaluation EvaluateMixtureDistribution(
            [Description(
                "The family of every component, for example Normal or Gamma. Call ListDistributions for the choices and parameter order.")]
            DistributionKind componentDistribution,
            [Description("The mixing weights, one per component, each between 0 and 1 and summing to 1.")]
            double[] weights,
            [Description(
                "The parameters of each component in ListDistributions order: one array per component, same order as weights.")]
            double[][] componentParameters,
            [Description("The points to evaluate, any real numbers, no missing values.")]
            double[] points)
        {
            var entry = DistributionCatalogue.Get(componentDistribution);
            DistributionGuard.Values(weights, nameof(weights));
            DistributionGuard.Points(points, nameof(points));

            if (weights.Any(w => w < 0 || w > 1) || Math.Abs(weights.Sum() - 1.0) > 1e-6)
            {
                throw new ArgumentException(
                    $"weights must each be between 0 and 1 and sum to 1 (they sum to {weights.Sum()}).",
                    nameof(weights));
            }

            if (componentParameters == null || componentParameters.Length != weights.Length)
            {
                throw new ArgumentException(
                    $"componentParameters needs one array per weight ({weights.Length}) but has {componentParameters?.Length ?? 0}.",
                    nameof(componentParameters));
            }

            var models = componentParameters
                .Select(p => UnivariateModel.Create(componentDistribution, p, nameof(componentParameters)))
                .ToArray();

            var density = new double[points.Length];
            var cumulative = new double[points.Length];

            for (var i = 0; i < points.Length; i++)
            {
                for (var j = 0; j < models.Length; j++)
                {
                    var d = models[j].Pdf(points[i]);
                    var c = models[j].Cdf(points[i]);
                    density[i] += weights[j] * (double.IsFinite(d) ? d : 0.0);
                    cumulative[i] += weights[j] * (double.IsFinite(c) ? c : 0.0);
                }
            }

            return new MixtureEvaluation(
                points,
                DistributionGuard.N(density),
                DistributionGuard.N(cumulative),
                MixtureEngine.Responsibilities(entry, weights, componentParameters, points));
        }

        private static void Validate(double[] values, int components, int maxIterations, double tolerance, int restarts,
            int maximumComponents = 10)
        {
            DistributionGuard.Values(values, nameof(values), 10);

            if (values.Length > MaximumValues)
            {
                throw new ArgumentException(
                    $"values has {values.Length} entries; at most {MaximumValues} are accepted. Sample the data down first.",
                    nameof(values));
            }

            if (components < 1 || components > maximumComponents)
            {
                throw new ArgumentException($"components must be between 1 and {maximumComponents}, got {components}.",
                    nameof(components));
            }

            if (components > values.Length / 5)
            {
                throw new ArgumentException(
                    $"{values.Length} values are too few for {components} components; use at most {values.Length / 5}.",
                    nameof(components));
            }

            if (maxIterations < 10 || maxIterations > 2000)
            {
                throw new ArgumentException($"maxIterations must be between 10 and 2000, got {maxIterations}.",
                    nameof(maxIterations));
            }

            if (!(tolerance >= 1e-12 && tolerance <= 0.1))
            {
                throw new ArgumentException($"tolerance must be between 1e-12 and 0.1, got {tolerance}.",
                    nameof(tolerance));
            }

            if (restarts < 1 || restarts > 20)
            {
                throw new ArgumentException($"restarts must be between 1 and 20, got {restarts}.", nameof(restarts));
            }

            if (HistogramEngine.StandardDeviation(values) <= 0)
            {
                throw new ArgumentException(
                    "All values are identical, so there is nothing to separate into components.",
                    nameof(values));
            }
        }
    }
}