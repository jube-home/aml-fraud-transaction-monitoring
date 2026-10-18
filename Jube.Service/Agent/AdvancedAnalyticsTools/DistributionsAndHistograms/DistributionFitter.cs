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

using Accord.Math.Optimization;
using Accord.Statistics.Distributions.Univariate;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    internal sealed record FitOutcome(
        UnivariateModel Model,
        double[] Parameters,
        bool[] Estimated,
        string Method,
        double LogLikelihood);

    internal static class DistributionFitter
    {
        private const double Penalty = 1e300;
        private const double LogRange = 9.0;
        private const int NumericSampleLimit = 5000;

        public static string? DataProblem(DistributionEntry entry, double[] data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                var v = data[i];

                var ok = entry.DataDomain switch
                {
                    DataDomain.Any => true,
                    DataDomain.Positive => v > 0,
                    DataDomain.NonNegative => v >= 0,
                    DataDomain.OpenUnit => v > 0 && v < 1,
                    DataDomain.NonNegativeInteger => v >= 0 && Math.Abs(v - Math.Round(v)) < 1e-9,
                    DataDomain.BinaryZeroOne => v == 0 || v == 1,
                    DataDomain.PositiveInteger => v >= 1 && Math.Abs(v - Math.Round(v)) < 1e-9,
                    DataDomain.Integer => Math.Abs(v - Math.Round(v)) < 1e-9,
                    _ => true
                };

                if (!ok)
                {
                    var need = entry.DataDomain switch
                    {
                        DataDomain.Positive => "strictly greater than 0",
                        DataDomain.NonNegative => "0 or more",
                        DataDomain.OpenUnit => "strictly between 0 and 1",
                        DataDomain.NonNegativeInteger => "whole numbers of 0 or more",
                        DataDomain.BinaryZeroOne => "exactly 0 or 1",
                        DataDomain.PositiveInteger => "whole numbers of 1 or more",
                        DataDomain.Integer => "whole numbers",
                        _ => "valid"
                    };

                    return $"values[{i}] = {v}, but a {entry.Name} distribution needs every value {need}.";
                }
            }

            return null;
        }

        public static bool CanFit(DistributionEntry entry)
        {
            return entry.Fit != FitMode.None;
        }

        public static FitOutcome Fit(DistributionEntry entry, double[] data, double[]? fixedParameters,
            string valuesName = "values")
        {
            if (!CanFit(entry))
            {
                throw new ArgumentException(
                    $"Fitting is not available for {entry.Name}: its parameters cannot be estimated from data by this tool. Supply the parameters directly. Call ListDistributions to see which distributions can be fitted.",
                    "distribution");
            }

            var problem = DataProblem(entry, data);

            if (problem != null)
            {
                throw new ArgumentException(problem, valuesName);
            }

            var anyFixed = fixedParameters != null && fixedParameters.Any(v => !double.IsNaN(v));

            if (fixedParameters != null && fixedParameters.Length != entry.Parameters.Length)
            {
                throw new ArgumentException(
                    $"fixedParameters must have {entry.Parameters.Length} entries in the order {string.Join(", ", entry.Parameters.Select(p => p.Name))} (NaN to estimate a parameter), but has {fixedParameters.Length}.",
                    "fixedParameters");
            }

            if (entry.Fit == FitMode.Library && !anyFixed)
            {
                return FitWithLibrary(entry, data, valuesName);
            }

            if (entry.Start == null)
            {
                throw new ArgumentException(
                    $"{entry.Name} cannot be fitted with some parameters held fixed. Leave fixedParameters empty or all NaN.",
                    "fixedParameters");
            }

            return FitNumerically(entry, data, fixedParameters);
        }

        private static FitOutcome FitWithLibrary(DistributionEntry entry, double[] data, string valuesName)
        {
            var start = UnivariateModel.Create(entry.Kind, entry.Examples);
            var raw = start.Raw;
            var copy = (double[])data.Clone();
            var options = entry.LibraryOptions?.Invoke();

            try
            {
                switch (raw)
                {
                    case UnivariateContinuousDistribution continuous:
                        continuous.Fit(copy, (double[]?)null!, options!);
                        break;
                    case UnivariateDiscreteDistribution discrete:
                        discrete.Fit(copy, (double[]?)null!, options!);
                        break;
                }
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException
                                           or NotImplementedException or OverflowException or IndexOutOfRangeException
                                           or DivideByZeroException)
            {
                throw new ArgumentException(
                    $"{entry.Name} could not be fitted to these values: {FirstLine(ex.Message)} Check that the values suit the distribution (see ListDistributions) and that they are not all identical.",
                    valuesName);
            }

            var parameters = entry.Extract!(raw);

            if (parameters.Any(v => !double.IsFinite(v)))
            {
                throw new ArgumentException(
                    $"{entry.Name} could not be fitted: the estimated parameters are not finite ({string.Join(", ", parameters)}). The values may be constant or unsuitable for this distribution.",
                    valuesName);
            }

            UnivariateModel model;

            try
            {
                model = UnivariateModel.Create(entry.Kind, parameters);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException(
                    $"{entry.Name} could not be fitted: the estimated parameters ({string.Join(", ", parameters)}) are not a valid distribution. The values may be constant or too few.",
                    valuesName);
            }

            var ll = model.LogLikelihood(data);
            var estimated = Enumerable.Repeat(true, parameters.Length).ToArray();

            return new FitOutcome(model, parameters, estimated,
                "Library estimator (maximum likelihood or method of moments as implemented for this distribution)", ll);
        }

        private static string FirstLine(string message)
        {
            var index = message.IndexOfAny(new[] { '\r', '\n' });
            var line = index >= 0 ? message[..index] : message;

            return line.EndsWith('.') ? line : line + ".";
        }

        private static double ToConstrained(ParameterDomain domain, double z)
        {
            return domain switch
            {
                ParameterDomain.Positive => Math.Exp(Math.Clamp(z, -300, 300)),
                ParameterDomain.UnitInterval => 1.0 / (1.0 + Math.Exp(-Math.Clamp(z, -40, 40))),
                _ => z
            };
        }

        private static double ToUnconstrained(ParameterDomain domain, double value)
        {
            switch (domain)
            {
                case ParameterDomain.Positive:
                    return Math.Log(Math.Max(value, 1e-300));
                case ParameterDomain.UnitInterval:
                    var clamped = Math.Min(Math.Max(value, 1e-9), 1 - 1e-9);

                    return Math.Log(clamped / (1 - clamped));
                default:
                    return value;
            }
        }

        private static double[] Thin(double[] data)
        {
            if (data.Length <= NumericSampleLimit)
            {
                return data;
            }

            var stride = (double)data.Length / NumericSampleLimit;
            var result = new double[NumericSampleLimit];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = data[(int)(i * stride)];
            }

            return result;
        }

        private static FitOutcome FitNumerically(DistributionEntry entry, double[] fullData, double[]? fixedParameters)
        {
            var data = Thin(fullData);
            var profile = new SampleProfile(fullData);
            var start = (double[])entry.Start!(profile).Clone();
            var count = entry.Parameters.Length;
            var estimated = new bool[count];
            var values = (double[])start.Clone();

            for (var i = 0; i < count; i++)
            {
                var given = fixedParameters != null && !double.IsNaN(fixedParameters[i]);
                var domain = entry.Parameters[i].Domain;

                var integerLike = domain is ParameterDomain.Integer or ParameterDomain.PositiveInteger
                    or ParameterDomain.NonNegativeInteger;

                var held = entry.HeldByDefault.Contains(i);

                if (given)
                {
                    values[i] = fixedParameters![i];
                    estimated[i] = false;
                }
                else if (integerLike || held)
                {
                    estimated[i] = false;
                }
                else
                {
                    estimated[i] = true;
                }
            }

            var free = Enumerable.Range(0, count).Where(i => estimated[i]).ToArray();

            if (free.Length == 0)
            {
                throw new ArgumentException(
                    "Every parameter is held fixed so there is nothing to estimate. Leave at least one NaN in fixedParameters.",
                    "fixedParameters");
            }

            UnivariateModel.ValidateParameters(entry,
                values.Select((v, i) => estimated[i] ? entry.Examples[i] : v).ToArray(),
                "fixedParameters");

            var z0 = free.Select(i => ToUnconstrained(entry.Parameters[i].Domain, values[i])).ToArray();

            var lower = free.Select((i, j) =>
                    entry.Parameters[i].Domain == ParameterDomain.Positive ? z0[j] - LogRange : double.NegativeInfinity)
                .ToArray();

            var upper = free.Select((i, j) =>
                    entry.Parameters[i].Domain == ParameterDomain.Positive ? z0[j] + LogRange : double.PositiveInfinity)
                .ToArray();

            double Objective(double[] z)
            {
                var p = (double[])values.Clone();

                for (var j = 0; j < free.Length; j++)
                {
                    p[free[j]] = ToConstrained(entry.Parameters[free[j]].Domain, Math.Clamp(z[j], lower[j], upper[j]));
                }

                try
                {
                    var model = UnivariateModel.Create(entry.Kind, p);
                    var ll = model.LogLikelihood(data);

                    return double.IsFinite(ll) ? -ll : Penalty;
                }
                catch (ArgumentException)
                {
                    return Penalty;
                }
            }

            var best = (double[])z0.Clone();
            var bestValue = Objective(best);

            for (var pass = 0; pass < 3; pass++)
            {
                var optimiser = new NelderMead(free.Length, Objective);

                for (var j = 0; j < free.Length; j++)
                {
                    optimiser.StepSize[j] = pass == 0 ? 0.3 : 0.05;
                }

                optimiser.Convergence.MaximumEvaluations = 4000 * free.Length;
                optimiser.Convergence.RelativeFunctionTolerance = 1e-12;
                optimiser.Convergence.AbsoluteFunctionTolerance = 1e-12;
                optimiser.Convergence.RelativeParameterTolerance = 1e-10;
                optimiser.Minimize((double[])best.Clone());

                if (optimiser.Value < bestValue)
                {
                    bestValue = optimiser.Value;
                    best = (double[])optimiser.Solution.Clone();
                }
            }

            if (bestValue >= Penalty)
            {
                throw new ArgumentException(
                    $"{entry.Name} could not be fitted: no valid parameters gave a finite likelihood for these values. Try a different distribution or check the values.",
                    "values");
            }

            var final = (double[])values.Clone();

            for (var j = 0; j < free.Length; j++)
            {
                final[free[j]] = ToConstrained(entry.Parameters[free[j]].Domain,
                    Math.Clamp(best[j], lower[j], upper[j]));
            }

            var fitted = UnivariateModel.Create(entry.Kind, final);
            var method = "Numerical maximum likelihood (Nelder-Mead on the log-likelihood)";

            if (data.Length < fullData.Length)
            {
                method +=
                    $"; parameters searched on an evenly spaced subset of {data.Length} of the {fullData.Length} values";
            }

            return new FitOutcome(fitted, final, estimated, method, fitted.LogLikelihood(fullData));
        }
    }
}