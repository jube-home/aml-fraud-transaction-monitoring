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

using Accord.Math;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Univariate;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    internal static class DistributionCatalogue
    {
        private static readonly Lazy<IReadOnlyList<DistributionEntry>> lazy = new(Build);
        public static IReadOnlyList<DistributionEntry> All => lazy.Value;

        public static DistributionEntry Get(DistributionKind kind)
        {
            return All.First(e => e.Kind == kind);
        }

        private static ParameterSpec P(string name, string description, ParameterDomain domain, double example)
        {
            return new ParameterSpec(name, description, domain, example);
        }

        private static int I(double value)
        {
            return (int)Math.Round(value);
        }

        private static IReadOnlyList<DistributionEntry> Build()
        {
            var list = new List<DistributionEntry>();

            list.Add(new DistributionEntry(DistributionKind.Normal, false,
                "The Gaussian bell curve, symmetric around its mean, fully described by mean and standard deviation.",
                "Every real number.",
                "Measurement error, aggregates of many small effects, z-score based thresholds, residuals; the default reference for outlier scoring.",
                new[]
                {
                    P("mean", "Centre of the distribution (any real number).", ParameterDomain.Real, 0),
                    P("standardDeviation", "Spread of the distribution (greater than 0).", ParameterDomain.Positive, 1)
                },
                p => new NormalDistribution(p[0], p[1]))
            {
                Fit = FitMode.Library,
                Extract = d => new[] { ((NormalDistribution)d).Mean, ((NormalDistribution)d).StandardDeviation }
            });

            list.Add(new DistributionEntry(DistributionKind.Lognormal, false,
                "The distribution of a variable whose natural logarithm is normal; right skewed with a long tail and strictly positive values.",
                "Values greater than 0.",
                "Transaction amounts, incomes, balances, durations and any positive quantity built from multiplicative effects.",
                new[]
                {
                    P("location", "Mean of the natural logarithm of the variable (any real number).",
                        ParameterDomain.Real,
                        0),
                    P("shape", "Standard deviation of the natural logarithm of the variable (greater than 0).",
                        ParameterDomain.Positive, 1)
                },
                p => new LognormalDistribution(p[0], p[1]))
            {
                Fit = FitMode.Library,
                DataDomain = DataDomain.Positive,
                Extract = d => new[] { ((LognormalDistribution)d).Location, ((LognormalDistribution)d).Shape }
            });

            list.Add(new DistributionEntry(DistributionKind.Exponential, false,
                "The memoryless waiting time between events that occur at a constant rate; strictly decreasing density.",
                "Values of 0 or more.",
                "Time between transactions, time to failure with constant hazard, inter-arrival times.",
                new[]
                {
                    P("rate", "Events per unit of time (greater than 0); the mean is 1 divided by the rate.",
                        ParameterDomain.Positive, 1)
                },
                p => new ExponentialDistribution(p[0]))
            {
                Fit = FitMode.Library,
                DataDomain = DataDomain.NonNegative,
                Extract = d => new[] { ((ExponentialDistribution)d).Rate }
            });

            list.Add(new DistributionEntry(DistributionKind.Gamma, false,
                "A flexible right skewed distribution on positive values; the sum of shape many exponential waiting times when shape is a whole number.",
                "Values greater than 0.",
                "Aggregated claim or transaction amounts, waiting times for several events, rainfall totals, positive skewed measurements.",
                new[]
                {
                    P("scale", "Scale theta (greater than 0); the mean is shape times scale.", ParameterDomain.Positive,
                        1),
                    P("shape", "Shape k (greater than 0); values near 1 look exponential, large values look normal.",
                        ParameterDomain.Positive, 2)
                },
                p => new GammaDistribution(p[0], p[1]))
            {
                Fit = FitMode.Library,
                DataDomain = DataDomain.Positive,
                Extract = d => new[] { ((GammaDistribution)d).Scale, ((GammaDistribution)d).Shape }
            });

            list.Add(new DistributionEntry(DistributionKind.Beta, false,
                "A flexible distribution on the unit interval, used for proportions and probabilities.",
                "Values between 0 and 1.",
                "Rates, proportions, conversion or default probabilities, Bayesian priors for a probability.",
                new[]
                {
                    P("alpha", "First shape parameter (greater than 0); larger values push mass toward 1.",
                        ParameterDomain.Positive, 2),
                    P("beta", "Second shape parameter (greater than 0); larger values push mass toward 0.",
                        ParameterDomain.Positive, 5)
                },
                p => new BetaDistribution(p[0], p[1]))
            {
                Fit = FitMode.Library,
                DataDomain = DataDomain.OpenUnit,
                Extract = d => new[] { ((BetaDistribution)d).Alpha, ((BetaDistribution)d).Beta }
            });

            list.Add(new DistributionEntry(DistributionKind.GeneralizedBeta, false,
                "The beta distribution rescaled to any interval between a minimum and a maximum.",
                "Values between minimum and maximum.",
                "Bounded quantities that are not proportions, such as scores between fixed limits or expert estimates.",
                new[]
                {
                    P("alpha", "First shape parameter (greater than 0).", ParameterDomain.Positive, 2),
                    P("beta", "Second shape parameter (greater than 0).", ParameterDomain.Positive, 5),
                    P("minimum", "Lower limit of the support (any real number, below maximum).", ParameterDomain.Real,
                        0),
                    P("maximum", "Upper limit of the support (above minimum).", ParameterDomain.Real, 1)
                },
                p => new GeneralizedBetaDistribution(p[0], p[1], p[2], p[3]))
            {
                Fit = FitMode.Library,
                LibraryOptions = () => new GeneralizedBetaOptions { FixMin = false, FixMax = false },
                Extract = d =>
                {
                    var g = (GeneralizedBetaDistribution)d;

                    return new[] { g.Alpha, g.Beta, g.Min, g.Max };
                }
            });

            list.Add(new DistributionEntry(DistributionKind.BetaPrime, false,
                "The distribution of the odds when a probability is beta distributed; heavy right tail on positive values.",
                "Values greater than 0.",
                "Ratios and odds of proportions, positive skewed ratios with heavy tails.",
                new[]
                {
                    P("alpha", "First shape parameter (greater than 0).", ParameterDomain.Positive, 2),
                    P("beta", "Second shape parameter (greater than 0); mean exists only when beta exceeds 1.",
                        ParameterDomain.Positive, 3)
                },
                p => new BetaPrimeDistribution(p[0], p[1]))
            {
                Fit = FitMode.Numeric,
                DataDomain = DataDomain.Positive,
                Start = _ => new[] { 2.0, 3.0 }
            });

            list.Add(new DistributionEntry(DistributionKind.BirnbaumSaunders, false,
                "The fatigue life distribution: time until cumulative damage crosses a threshold; positive and right skewed.",
                "Values greater than location.",
                "Time to failure under wear, durations of accumulating processes.",
                new[]
                {
                    P("location", "Lower limit of the support (any real number); usually 0.", ParameterDomain.Real, 0),
                    P("scale", "Scale beta, which is also the median (greater than 0).", ParameterDomain.Positive, 1),
                    P("shape", "Shape gamma (greater than 0); larger values mean more skew.", ParameterDomain.Positive,
                        1)
                },
                p => new BirnbaumSaundersDistribution(p[0], p[1], p[2]))
            {
                Fit = FitMode.Numeric,
                DataDomain = DataDomain.Positive,
                Start = s => new[] { 0.0, s.Median, 0.5 },
                HeldByDefault = new[] { 0 },
                FitNote = "The location is held at 0 unless you supply it in fixedParameters."
            });

            list.Add(new DistributionEntry(DistributionKind.Cauchy, false,
                "A symmetric, extremely heavy tailed distribution with no finite mean or variance.",
                "Every real number.",
                "Robust modelling of data with wild outliers, ratios of normal variables, resonance behaviour.",
                new[]
                {
                    P("location", "Median and mode of the distribution (any real number).", ParameterDomain.Real, 0),
                    P("scale", "Half width at half maximum (greater than 0).", ParameterDomain.Positive, 1)
                },
                p => new CauchyDistribution(p[0], p[1]))
            {
                Fit = FitMode.Numeric,
                Start = s => new[] { s.Median, Math.Max(s.InterquartileRange / 2.0, 1e-6) }
            });

            list.Add(new DistributionEntry(DistributionKind.ChiSquare, false,
                "The distribution of a sum of squared standard normal variables; positive and right skewed.",
                "Values of 0 or more.",
                "Sampling distribution of variance estimates and of many test statistics.",
                new[]
                {
                    P("degreesOfFreedom", "Number of squared normals summed (a whole number of at least 1).",
                        ParameterDomain.PositiveInteger, 3)
                },
                p => new ChiSquareDistribution(I(p[0]))));

            list.Add(new DistributionEntry(DistributionKind.F, false,
                "The distribution of the ratio of two scaled chi-square variables; positive and right skewed.",
                "Values of 0 or more.",
                "Ratio of variances, ANOVA and regression significance tests.",
                new[]
                {
                    P("numeratorDegreesOfFreedom", "Degrees of freedom of the numerator (whole number of at least 1).",
                        ParameterDomain.PositiveInteger, 5),
                    P("denominatorDegreesOfFreedom",
                        "Degrees of freedom of the denominator (whole number of at least 1).",
                        ParameterDomain.PositiveInteger, 10)
                },
                p => new FDistribution(I(p[0]), I(p[1]))));

            list.Add(new DistributionEntry(DistributionKind.FoldedNormal, false,
                "The distribution of the absolute value of a normal variable.",
                "Values of 0 or more.",
                "Magnitudes of errors or deviations whose sign is discarded.",
                new[]
                {
                    P("mean", "Mean of the underlying normal before folding (any real number).", ParameterDomain.Real,
                        0),
                    P("standardDeviation", "Standard deviation of the underlying normal (greater than 0).",
                        ParameterDomain.Positive, 1)
                },
                p => new FoldedNormalDistribution(p[0], p[1]))
            {
                Fit = FitMode.Numeric,
                DataDomain = DataDomain.NonNegative,
                Start = s => new[] { s.Mean, Math.Max(s.StandardDeviation, 1e-6) }
            });

            list.Add(new DistributionEntry(DistributionKind.GeneralizedNormal, false,
                "A symmetric family that includes the Laplace (shape 1), normal (shape 2) and near uniform (large shape) distributions.",
                "Every real number.",
                "Modelling the tail weight of symmetric residuals, robust alternatives to the normal.",
                new[]
                {
                    P("location", "Centre of the distribution (any real number).", ParameterDomain.Real, 0),
                    P("scale", "Scale alpha (greater than 0).", ParameterDomain.Positive, 1),
                    P("shape", "Shape beta (greater than 0); smaller is heavier tailed, 2 is normal.",
                        ParameterDomain.Positive, 2)
                },
                p => new GeneralizedNormalDistribution(p[0], p[1], p[2]))
            {
                Fit = FitMode.Numeric,
                Start = s => new[] { s.Median, Math.Max(s.StandardDeviation * Math.Sqrt(2), 1e-6), 2.0 }
            });

            list.Add(new DistributionEntry(DistributionKind.GeneralizedPareto, false,
                "The distribution of excesses over a high threshold; the standard model for the tail of a distribution.",
                "Values of location or more (bounded above when shape is negative).",
                "Extreme value analysis, modelling the size of very large transactions or losses beyond a threshold.",
                new[]
                {
                    P("location", "Threshold above which the tail is modelled (any real number).", ParameterDomain.Real,
                        0),
                    P("scale", "Scale sigma (greater than 0).", ParameterDomain.Positive, 1),
                    P("shape",
                        "Tail index xi: 0 is exponential tail, positive is a heavy power tail, negative is a bounded tail.",
                        ParameterDomain.Real, 0.2)
                },
                p => new GeneralizedParetoDistribution(p[0], p[1], p[2]))
            {
                Fit = FitMode.Numeric,
                Start = s => new[] { s.Minimum, Math.Max(s.Mean - s.Minimum, 1e-6), 0.1 },
                HeldByDefault = new[] { 0 },
                FitNote =
                    "The location (threshold) is held at the smallest observation unless you supply it in fixedParameters."
            });

            list.Add(new DistributionEntry(DistributionKind.Gompertz, false,
                "A right skewed lifetime distribution whose hazard grows exponentially with age.",
                "Values of 0 or more.",
                "Mortality and ageing processes, time to failure with increasing hazard.",
                new[]
                {
                    P("eta", "Shape eta (greater than 0); smaller means more mass at early times.",
                        ParameterDomain.Positive, 1),
                    P("b", "Scale b (greater than 0); the rate at which hazard grows.", ParameterDomain.Positive, 1)
                },
                p => new GompertzDistribution(p[0], p[1]))
            {
                Fit = FitMode.Numeric,
                DataDomain = DataDomain.NonNegative,
                Start = s => new[] { 1.0, 1.0 / Math.Max(s.Mean, 1e-6) }
            });

            list.Add(new DistributionEntry(DistributionKind.Gumbel, false,
                "The extreme value (type I) distribution of the maximum of many observations; right skewed.",
                "Every real number.",
                "Modelling the largest value in a period such as the maximum daily transaction.",
                new[]
                {
                    P("location", "Mode of the distribution (any real number).", ParameterDomain.Real, 0),
                    P("scale", "Scale beta (greater than 0).", ParameterDomain.Positive, 1)
                },
                p => new GumbelDistribution(p[0], p[1]))
            {
                Fit = FitMode.Library,
                Extract = d => new[] { ((GumbelDistribution)d).Location, ((GumbelDistribution)d).Shape }
            });

            list.Add(new DistributionEntry(DistributionKind.HyperbolicSecant, false,
                "A symmetric distribution with heavier tails than the normal and no parameters; standard deviation 1.",
                "Every real number.",
                "A parameter free heavy tailed symmetric reference curve.",
                Array.Empty<ParameterSpec>(),
                _ => new HyperbolicSecantDistribution()));

            list.Add(new DistributionEntry(DistributionKind.InverseChiSquare, false,
                "The distribution of the reciprocal of a chi-square variable; positive and heavy right tailed.",
                "Values greater than 0.",
                "Bayesian priors for a variance.",
                new[]
                {
                    P("degreesOfFreedom", "Degrees of freedom (whole number of at least 1).",
                        ParameterDomain.PositiveInteger, 5)
                },
                p => new InverseChiSquareDistribution(I(p[0]))));

            list.Add(new DistributionEntry(DistributionKind.InverseGamma, false,
                "The distribution of the reciprocal of a gamma variable; positive with a heavy right tail.",
                "Values greater than 0.",
                "Bayesian priors for a variance or scale, heavy tailed positive data.",
                new[]
                {
                    P("shape", "Shape alpha (greater than 0); mean exists above 1, variance above 2.",
                        ParameterDomain.Positive, 3),
                    P("scale", "Scale beta (greater than 0).", ParameterDomain.Positive, 1)
                },
                p => new InverseGammaDistribution(p[0], p[1]))
            {
                Fit = FitMode.Numeric,
                DataDomain = DataDomain.Positive,
                Start = s =>
                {
                    var variance = Math.Max(s.StandardDeviation * s.StandardDeviation, 1e-12);
                    var shape = 2.0 + s.Mean * s.Mean / variance;

                    return new[] { shape, Math.Max(s.Mean * (shape - 1), 1e-9) };
                }
            });

            list.Add(new DistributionEntry(DistributionKind.InverseGaussian, false,
                "The distribution of the time for a drifting random walk to reach a level; positive and right skewed.",
                "Values greater than 0.",
                "Positive skewed durations and amounts, first passage times.",
                new[]
                {
                    P("mean", "Mean of the distribution (greater than 0).", ParameterDomain.Positive, 1),
                    P("shape", "Shape lambda (greater than 0); larger values look more normal.",
                        ParameterDomain.Positive,
                        1)
                },
                p => new InverseGaussianDistribution(p[0], p[1]))
            {
                Fit = FitMode.Library,
                DataDomain = DataDomain.Positive,
                Extract = d => new[] { ((InverseGaussianDistribution)d).Mean, ((InverseGaussianDistribution)d).Shape }
            });

            list.Add(new DistributionEntry(DistributionKind.Kumaraswamy, false,
                "A bounded distribution on 0 to 1 similar to the beta but with a closed form cumulative function.",
                "Values between 0 and 1.",
                "Proportions and rates where a simple closed form quantile function is wanted.",
                new[]
                {
                    P("a", "First shape parameter (greater than 0).", ParameterDomain.Positive, 2),
                    P("b", "Second shape parameter (greater than 0).", ParameterDomain.Positive, 3)
                },
                p => new KumaraswamyDistribution(p[0], p[1]))
            {
                Fit = FitMode.Numeric,
                DataDomain = DataDomain.OpenUnit,
                Start = _ => new[] { 2.0, 2.0 }
            });

            list.Add(new DistributionEntry(DistributionKind.Laplace, false,
                "The double exponential: a symmetric distribution with a sharp peak and heavier tails than the normal.",
                "Every real number.",
                "Differences and residuals with heavy tails, robust regression errors.",
                new[]
                {
                    P("location", "Centre and median (any real number).", ParameterDomain.Real, 0),
                    P("scale", "Scale b (greater than 0); mean absolute deviation equals the scale.",
                        ParameterDomain.Positive, 1)
                },
                p => new LaplaceDistribution(p[0], p[1]))
            {
                Fit = FitMode.Numeric,
                Start = s => new[] { s.Median, Math.Max(s.MeanAbsoluteDeviationFromMedian, 1e-9) }
            });

            list.Add(new DistributionEntry(DistributionKind.Levy, false,
                "A very heavy tailed stable distribution on values above its location; infinite mean and variance.",
                "Values greater than location.",
                "Extreme heavy tailed positive phenomena, first passage of Brownian motion.",
                new[]
                {
                    P("location", "Lower limit of the support (any real number).", ParameterDomain.Real, 0),
                    P("scale", "Scale c (greater than 0).", ParameterDomain.Positive, 1)
                },
                p => new LevyDistribution(p[0], p[1])));

            list.Add(new DistributionEntry(DistributionKind.Logistic, false,
                "A symmetric bell shaped distribution similar to the normal with slightly heavier tails and a closed form cumulative function.",
                "Every real number.",
                "Growth curves, the latent variable behind logistic regression, robust normal alternative.",
                new[]
                {
                    P("location", "Mean and median (any real number).", ParameterDomain.Real, 0),
                    P("scale", "Scale s (greater than 0); standard deviation is scale times pi divided by root 3.",
                        ParameterDomain.Positive, 1)
                },
                p => new LogisticDistribution(p[0], p[1]))
            {
                Fit = FitMode.Numeric,
                Start = s => new[] { s.Mean, Math.Max(s.StandardDeviation * Math.Sqrt(3) / Math.PI, 1e-9) }
            });

            list.Add(new DistributionEntry(DistributionKind.LogLogistic, false,
                "A positive distribution whose logarithm is logistic; unimodal and heavy tailed, with a closed form quantile function.",
                "Values of 0 or more.",
                "Survival times, incomes, hydrology; a heavy tailed alternative to the lognormal.",
                new[]
                {
                    P("scale", "Scale alpha, which is also the median (greater than 0).", ParameterDomain.Positive, 1),
                    P("shape", "Shape beta (greater than 0); larger means less spread, mean exists above 1.",
                        ParameterDomain.Positive, 3)
                },
                p => new LogLogisticDistribution(p[0], p[1]))
            {
                Fit = FitMode.Numeric,
                DataDomain = DataDomain.Positive,
                Start = s => new[] { Math.Max(s.Median, 1e-9), 2.0 }
            });

            list.Add(new DistributionEntry(DistributionKind.Nakagami, false,
                "A positive distribution used for the amplitude of fading signals; generalises the Rayleigh.",
                "Values of 0 or more.",
                "Magnitudes and signal amplitudes, positive skewed measurements.",
                new[]
                {
                    P("shape", "Shape m (at least 0.5); 1 gives the Rayleigh.", ParameterDomain.Positive, 1),
                    P("spread", "Spread omega (greater than 0), the mean of the squared value.",
                        ParameterDomain.Positive,
                        1)
                },
                p => new NakagamiDistribution(p[0], p[1]))
            {
                Fit = FitMode.Library,
                DataDomain = DataDomain.NonNegative,
                Extract = d => new[] { ((NakagamiDistribution)d).Shape, ((NakagamiDistribution)d).Spread }
            });

            list.Add(new DistributionEntry(DistributionKind.NoncentralT, false,
                "The Student t distribution shifted by a noncentrality parameter; asymmetric.",
                "Every real number.",
                "Power calculations of t tests and effect size intervals.",
                new[]
                {
                    P("degreesOfFreedom", "Degrees of freedom (greater than 0).", ParameterDomain.Positive, 10),
                    P("noncentrality", "Noncentrality delta (any real number).", ParameterDomain.Real, 1)
                },
                p => new NoncentralTDistribution(p[0], p[1])));

            list.Add(new DistributionEntry(DistributionKind.Pareto, false,
                "The power law distribution: a heavy right tail starting at a minimum scale value (the 80/20 rule).",
                "Values of scale or more.",
                "Wealth and transaction size tails, city sizes, rare very large events.",
                new[]
                {
                    P("scale", "Minimum possible value xm (greater than 0).", ParameterDomain.Positive, 1),
                    P("shape", "Tail index alpha (greater than 0); smaller means a heavier tail, mean exists above 1.",
                        ParameterDomain.Positive, 3)
                },
                p => new ParetoDistribution(p[0], p[1]))
            {
                Fit = FitMode.Library,
                DataDomain = DataDomain.Positive,
                Extract = d => new[] { ((ParetoDistribution)d).Scale, ((ParetoDistribution)d).Alpha }
            });

            list.Add(new DistributionEntry(DistributionKind.PowerLognormal, false,
                "A lognormal generalisation with an extra power parameter that adds asymmetry.",
                "Values of 0 or more.",
                "Lifetime and reliability data where the lognormal is close but not flexible enough.",
                new[]
                {
                    P("power", "Power p (greater than 0); 1 gives the lognormal.", ParameterDomain.Positive, 1),
                    P("shape", "Shape sigma (greater than 0).", ParameterDomain.Positive, 1)
                },
                p => new PowerLognormalDistribution(p[0], p[1])));

            list.Add(new DistributionEntry(DistributionKind.PowerNormal, false,
                "A normal generalisation with a power parameter that adds asymmetry.",
                "Every real number.",
                "Slightly skewed symmetric like data.",
                new[]
                {
                    P("power", "Power p (greater than 0); 1 gives the standard normal.", ParameterDomain.Positive, 2)
                },
                p => new PowerNormalDistribution(p[0])));

            list.Add(new DistributionEntry(DistributionKind.Rayleigh, false,
                "The distribution of the length of a two dimensional normal vector; positive and right skewed.",
                "Values of 0 or more.",
                "Distances, wind speed, signal magnitudes.",
                new[] { P("sigma", "Scale sigma (greater than 0); the mode is sigma.", ParameterDomain.Positive, 1) },
                p => new RayleighDistribution(p[0]))
            {
                Fit = FitMode.Library,
                DataDomain = DataDomain.NonNegative,
                Extract = d => new[] { ((RayleighDistribution)d).Scale }
            });

            list.Add(new DistributionEntry(DistributionKind.ShiftedLogLogistic, false,
                "A three parameter logistic based distribution used in extreme value analysis of skewed data.",
                "Bounded on one side depending on shape.",
                "Flood and extreme event frequency analysis, skewed heavy tailed data.",
                new[]
                {
                    P("location", "Location (any real number).", ParameterDomain.Real, 0),
                    P("scale", "Scale (greater than 0).", ParameterDomain.Positive, 1),
                    P("shape", "Shape (any real number); 0 gives the logistic.", ParameterDomain.Real, 0.2)
                },
                p => new ShiftedLogLogisticDistribution(p[0], p[1], p[2]))
            {
                Fit = FitMode.Numeric,
                Start = s => new[] { s.Median, Math.Max(s.StandardDeviation * Math.Sqrt(3) / Math.PI, 1e-9), 0.05 }
            });

            list.Add(new DistributionEntry(DistributionKind.SkewNormal, false,
                "A normal generalisation with a shape parameter that controls skewness.",
                "Every real number.",
                "Mildly skewed data where a normal is nearly right but the tails are asymmetric.",
                new[]
                {
                    P("location", "Location xi (any real number).", ParameterDomain.Real, 0),
                    P("scale", "Scale omega (greater than 0).", ParameterDomain.Positive, 1),
                    P("shape",
                        "Shape alpha (any real number); 0 is normal, positive is right skewed, negative is left skewed.",
                        ParameterDomain.Real, 2)
                },
                p => new SkewNormalDistribution(p[0], p[1], p[2]))
            {
                Fit = FitMode.Numeric,
                Start = s => new[] { s.Mean, Math.Max(s.StandardDeviation, 1e-9), s.Skewness >= 0 ? 1.0 : -1.0 }
            });

            list.Add(new DistributionEntry(DistributionKind.StudentT, false,
                "The standard Student t distribution: a symmetric bell curve with heavier tails than the normal when degrees of freedom are small.",
                "Every real number.",
                "Sampling distribution of standardised means with estimated variance, heavy tailed standardised residuals.",
                new[]
                {
                    P("degreesOfFreedom", "Degrees of freedom (at least 1); large values approach the standard normal.",
                        ParameterDomain.Positive, 5)
                },
                p => new TDistribution(p[0])));

            list.Add(new DistributionEntry(DistributionKind.Trapezoidal, false,
                "A distribution that rises to a flat top and falls again, with shaped edges; a generalisation of the triangular.",
                "Values between a and d.",
                "Expert judgement of a most likely range, bounded uncertain quantities with a plateau.",
                new[]
                {
                    P("a", "Minimum value where the density starts (any real number, below b).", ParameterDomain.Real,
                        0),
                    P("b", "Start of the flat top (between a and c).", ParameterDomain.Real, 1),
                    P("c", "End of the flat top (between b and d).", ParameterDomain.Real, 2),
                    P("d", "Maximum value where the density ends (above c).", ParameterDomain.Real, 3),
                    P("n1",
                        "Growth exponent of the rising edge (greater than 0); 2 gives a straight line edge, 1 a flat step, larger values a convex curve.",
                        ParameterDomain.Positive, 2),
                    P("n3",
                        "Decay exponent of the falling edge (greater than 0); 2 gives a straight line edge, 1 a flat step, larger values a convex curve.",
                        ParameterDomain.Positive, 2)
                },
                p => new TrapezoidalDistribution(p[0], p[1], p[2], p[3], p[4], p[5])));

            list.Add(new DistributionEntry(DistributionKind.Triangular, false,
                "A bounded distribution with a straight line rise to a most likely value and a straight line fall.",
                "Values between minimum and maximum.",
                "Simple bounded estimates of uncertain quantities from a minimum, most likely and maximum value.",
                new[]
                {
                    P("minimum", "Lowest possible value.", ParameterDomain.Real, 0),
                    P("maximum", "Highest possible value (above minimum).", ParameterDomain.Real, 1),
                    P("mode", "Most likely value (between minimum and maximum).", ParameterDomain.Real, 0.5)
                },
                p => new TriangularDistribution(p[0], p[1], p[2]))
            {
                Fit = FitMode.Library,
                LibraryOptions = () => new TriangularOptions { FixMin = false, FixMax = false },
                Extract = d =>
                {
                    var t = (TriangularDistribution)d;

                    return new[] { t.Min, t.Max, t.Mode };
                }
            });

            list.Add(new DistributionEntry(DistributionKind.TukeyLambda, false,
                "A parameter controlled family whose tail weight changes smoothly from heavy (negative lambda) through logistic (0) and near normal (0.14) to uniform (2).",
                "Every real number for lambda of 0 or below, bounded for positive lambda.",
                "Probability plot correlation checks of symmetric distribution shape.",
                new[] { P("lambda", "Shape lambda (any real number).", ParameterDomain.Real, 0.14) },
                p => new TukeyLambdaDistribution(p[0])));

            list.Add(new DistributionEntry(DistributionKind.UniformContinuous, false,
                "Every value between a lower and an upper bound is equally likely.",
                "Values between minimum and maximum.",
                "Random numbers, complete ignorance over a range, comparing against the null of no structure.",
                new[]
                {
                    P("minimum", "Lower bound (any real number).", ParameterDomain.Real, 0),
                    P("maximum", "Upper bound (above minimum).", ParameterDomain.Real, 1)
                },
                p => new UniformContinuousDistribution(p[0], p[1]))
            {
                Fit = FitMode.Library,
                Extract = d => new[]
                    { ((UniformContinuousDistribution)d).Minimum, ((UniformContinuousDistribution)d).Maximum }
            });

            list.Add(new DistributionEntry(DistributionKind.UQuadratic, false,
                "A bounded distribution shaped like a U: most mass near both ends of its interval.",
                "Values between a and b.",
                "Bimodal quantities pushed against two limits.",
                new[]
                {
                    P("a", "Lower limit (0 or more).", ParameterDomain.Real, 0),
                    P("b", "Upper limit (above a).", ParameterDomain.Positive, 1)
                },
                p => new UQuadraticDistribution(p[0], p[1])));

            list.Add(new DistributionEntry(DistributionKind.VonMises, false,
                "The circular normal: a bell curve on an angle measured in radians.",
                "Angles between the mean minus pi and the mean plus pi.",
                "Time of day or day of year seasonality, angular data; convert clock time to radians first.",
                new[]
                {
                    P("mean", "Mean direction in radians.", ParameterDomain.Real, 0),
                    P("concentration", "Concentration kappa (greater than 0); larger means tighter around the mean.",
                        ParameterDomain.Positive, 1)
                },
                p => new VonMisesDistribution(p[0], p[1]))
            {
                Fit = FitMode.Library,
                Extract = d => new[] { ((VonMisesDistribution)d).Mean, ((VonMisesDistribution)d).Concentration }
            });

            list.Add(new DistributionEntry(DistributionKind.Weibull, false,
                "The classic lifetime distribution: hazard falls for shape below 1, is constant at 1 and rises above 1.",
                "Values of 0 or more.",
                "Time to failure or churn, reliability, wear out and infant mortality analysis.",
                new[]
                {
                    P("shape", "Shape k (greater than 0); below 1 hazard decreases, above 1 it increases.",
                        ParameterDomain.Positive, 1.5),
                    P("scale", "Scale lambda (greater than 0); the 63.2nd percentile.", ParameterDomain.Positive, 1)
                },
                p => new WeibullDistribution(p[0], p[1]))
            {
                Fit = FitMode.Numeric,
                DataDomain = DataDomain.Positive,
                Start = s =>
                {
                    var cv = Math.Max(s.StandardDeviation, 1e-9) / Math.Max(s.Mean, 1e-9);
                    var shape = Math.Min(Math.Max(Math.Pow(cv, -1.086), 0.2), 50);

                    return new[] { shape, Math.Max(s.Mean / Gamma.Function(1.0 + 1.0 / shape), 1e-9) };
                }
            });

            list.Add(new DistributionEntry(DistributionKind.WrappedCauchy, false,
                "The Cauchy distribution wrapped around a circle; a heavy tailed circular distribution.",
                "Angles between 0 and 2 pi.",
                "Angular data with outliers. Only the density is available: cumulative, quantile, moments and sampling return null or fail.",
                new[]
                {
                    P("mu", "Direction in radians.", ParameterDomain.Real, 0),
                    P("gamma", "Concentration gamma (greater than 0).", ParameterDomain.Positive, 1)
                },
                p => new WrappedCauchyDistribution(p[0], p[1])));

            list.Add(new DistributionEntry(DistributionKind.KolmogorovSmirnovStatistic, false,
                "The null distribution of the Kolmogorov-Smirnov statistic D for a sample size; use it to convert a D value into a p value.",
                "Values between 0 and 1.",
                "Looking up critical values and p values of a goodness of fit statistic. The density is not available (null), the cumulative and quantile functions are.",
                new[] { P("samples", "Sample size (greater than 0).", ParameterDomain.Positive, 20) },
                p => new KolmogorovSmirnovDistribution(p[0])));

            list.Add(new DistributionEntry(DistributionKind.ShapiroWilkStatistic, false,
                "The null distribution of the Shapiro-Wilk W statistic for a sample size.",
                "Values between 0 and 1.",
                "Looking up p values of a normality test statistic.",
                new[]
                {
                    P("samples", "Sample size (whole number of at least 4).", ParameterDomain.PositiveInteger, 20)
                },
                p => new ShapiroWilkDistribution(I(p[0]))));

            list.Add(new DistributionEntry(DistributionKind.GrubbsStatistic, false,
                "The null distribution of the Grubbs outlier statistic for a sample size.",
                "Values of 0 or more.",
                "Looking up critical values of the Grubbs outlier test. The density is not available (null), the cumulative and quantile functions are.",
                new[]
                {
                    P("samples", "Sample size (whole number of at least 3).", ParameterDomain.PositiveInteger, 20)
                },
                p => new GrubbDistribution(I(p[0]))));

            list.Add(new DistributionEntry(DistributionKind.WilcoxonSignedRankStatistic, false,
                "The null distribution of the Wilcoxon signed rank statistic W for a number of pairs.",
                "Values between 0 and n times n plus 1 divided by 2.",
                "Looking up p values of the paired non-parametric test.",
                new[] { P("n", "Number of pairs (whole number of at least 1).", ParameterDomain.PositiveInteger, 10) },
                p => new WilcoxonDistribution(I(p[0]))));

            list.Add(new DistributionEntry(DistributionKind.MannWhitneyStatistic, false,
                "The null distribution of the Mann-Whitney U statistic for two sample sizes.",
                "Values between 0 and n1 times n2.",
                "Looking up p values of the two sample rank sum test.",
                new[]
                {
                    P("n1", "Size of the first sample (whole number of at least 1).", ParameterDomain.PositiveInteger,
                        8),
                    P("n2", "Size of the second sample (whole number of at least 1).", ParameterDomain.PositiveInteger,
                        10)
                },
                p => new MannWhitneyDistribution(I(p[0]), I(p[1]))));

            list.Add(new DistributionEntry(DistributionKind.Bernoulli, true,
                "A single yes or no trial: 1 with a given probability, otherwise 0.",
                "The whole numbers 0 and 1.",
                "Fraud or default flags, any binary outcome.",
                new[] { P("probability", "Probability of a 1 (between 0 and 1).", ParameterDomain.UnitInterval, 0.3) },
                p => new BernoulliDistribution(p[0]))
            {
                Fit = FitMode.Library,
                DataDomain = DataDomain.BinaryZeroOne,
                Extract = d => new[] { ((BernoulliDistribution)d).Mean }
            });

            list.Add(new DistributionEntry(DistributionKind.Binomial, true,
                "The number of successes in a fixed number of independent trials with the same success probability.",
                "Whole numbers from 0 to trials.",
                "Counts of flagged cases out of a known number of cases, defect counts, conversion counts.",
                new[]
                {
                    P("trials", "Number of trials (whole number of at least 1).", ParameterDomain.PositiveInteger, 10),
                    P("probability", "Success probability of each trial (between 0 and 1).",
                        ParameterDomain.UnitInterval,
                        0.5)
                },
                p => new BinomialDistribution(I(p[0]), p[1]))
            {
                Fit = FitMode.Numeric,
                DataDomain = DataDomain.NonNegativeInteger,
                Start = s => new[]
                    { Math.Max(s.Maximum, 1.0), Math.Min(Math.Max(s.Mean / Math.Max(s.Maximum, 1.0), 1e-6), 1 - 1e-6) },
                FitNote =
                    "The number of trials is not estimated: it is taken from fixedParameters, or the largest observation when not given."
            });

            list.Add(new DistributionEntry(DistributionKind.Degenerate, true,
                "A constant: every draw is the same whole number.",
                "A single whole number.",
                "Representing a certain outcome inside larger comparisons.",
                new[] { P("value", "The constant whole number.", ParameterDomain.Integer, 0) },
                p => new DegenerateDistribution(I(p[0]))));

            list.Add(new DistributionEntry(DistributionKind.Geometric, true,
                "The number of failures before the first success in repeated independent trials.",
                "Whole numbers of 0 or more.",
                "Number of transactions before the first alert, number of failed attempts before a first success.",
                new[]
                {
                    P("probability", "Success probability of each trial (greater than 0 and at most 1).",
                        ParameterDomain.UnitInterval, 0.3)
                },
                p => new GeometricDistribution(p[0]))
            {
                Fit = FitMode.Library,
                DataDomain = DataDomain.NonNegativeInteger,
                Extract = d => new[] { ((GeometricDistribution)d).ProbabilityOfSuccess }
            });

            list.Add(new DistributionEntry(DistributionKind.Hypergeometric, true,
                "The number of successes when drawing without replacement from a finite population that contains a known number of successes.",
                "Whole numbers between the larger of 0 and samples plus successes minus population, and the smaller of samples and successes.",
                "Sampling audits, enrichment tests, Fisher exact test probabilities.",
                new[]
                {
                    P("populationSize", "Total number of items in the population (whole number of at least 1).",
                        ParameterDomain.PositiveInteger, 50),
                    P("successes", "Number of success items in the population (whole number of at least 0).",
                        ParameterDomain.NonNegativeInteger, 10),
                    P("samples", "Number of items drawn without replacement (whole number of at least 1).",
                        ParameterDomain.PositiveInteger, 15)
                },
                p => new HypergeometricDistribution(I(p[0]), I(p[1]), I(p[2]))));

            list.Add(new DistributionEntry(DistributionKind.NegativeBinomial, true,
                "The number of successes before a fixed number of failures occurs; an overdispersed count distribution.",
                "Whole numbers of 0 or more.",
                "Overdispersed counts where the variance exceeds the mean.",
                new[]
                {
                    P("failures", "Number of failures at which counting stops (whole number of at least 1).",
                        ParameterDomain.PositiveInteger, 5),
                    P("probability", "Success probability of each trial (between 0 and 1).",
                        ParameterDomain.UnitInterval,
                        0.5)
                },
                p => new NegativeBinomialDistribution(I(p[0]), p[1])));

            list.Add(new DistributionEntry(DistributionKind.Poisson, true,
                "The number of events in a fixed interval when events occur independently at a constant average rate.",
                "Whole numbers of 0 or more.",
                "Counts of transactions, alerts or arrivals per period; rare event counts.",
                new[]
                {
                    P("lambda",
                        "Average number of events per interval (greater than 0); it is both the mean and the variance.",
                        ParameterDomain.Positive, 4)
                },
                p => new PoissonDistribution(p[0]))
            {
                Fit = FitMode.Library,
                DataDomain = DataDomain.NonNegativeInteger,
                Extract = d => new[] { ((PoissonDistribution)d).Lambda }
            });

            list.Add(new DistributionEntry(DistributionKind.SymmetricGeometric, true,
                "The difference of two independent geometric counts: a symmetric distribution over all whole numbers.",
                "Every whole number.",
                "Differences between counts, integer valued noise. Only the probability mass and sampling are available; cumulative and quantile functions return null.",
                new[]
                {
                    P("probability", "Success probability of the underlying geometric variables (between 0 and 1).",
                        ParameterDomain.UnitInterval, 0.5)
                },
                p => new SymmetricGeometricDistribution(p[0])));

            list.Add(new DistributionEntry(DistributionKind.UniformDiscrete, true,
                "Every whole number between a lower and an upper bound is equally likely (a fair die is 1 to 6).",
                "Whole numbers between minimum and maximum inclusive.",
                "Fair dice, random indices, categorical codes with no preference.",
                new[]
                {
                    P("minimum", "Smallest whole number.", ParameterDomain.Integer, 1),
                    P("maximum", "Largest whole number (at least minimum).", ParameterDomain.Integer, 6)
                },
                p => new UniformDiscreteDistribution(I(p[0]), I(p[1])))
            {
                Fit = FitMode.Library,
                DataDomain = DataDomain.Integer,
                Extract = d => new[]
                    { ((UniformDiscreteDistribution)d).Minimum, ((UniformDiscreteDistribution)d).Maximum }
            });

            list.Add(new DistributionEntry(DistributionKind.GeneralDiscrete, true,
                "An arbitrary probability mass function over the whole numbers 0, 1, 2 and so on, given as a list of probabilities.",
                "Whole numbers from 0 to the number of probabilities minus 1.",
                "Any custom categorical distribution, such as a fitted frequency table of counts.",
                new[]
                {
                    P("probabilities",
                        "Probability of value 0, then value 1, then value 2 and so on; must be at least 0 and sum to 1.",
                        ParameterDomain.Real, 0.5)
                },
                p => new GeneralDiscreteDistribution(p))
            {
                Variadic = true
            });

            list.Add(new DistributionEntry(DistributionKind.Rademacher, true,
                "A fair coin flip on the values -1 and +1.",
                "The whole numbers -1 and 1.",
                "Random sign flips, symmetric random walks.",
                Array.Empty<ParameterSpec>(),
                _ => new RademacherDistribution()));

            return list;
        }
    }
}