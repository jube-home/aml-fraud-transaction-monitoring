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

using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Univariate;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    internal sealed class UnivariateModel
    {
        private readonly IUnivariateDistribution distribution;

        private UnivariateModel(DistributionEntry entry, double[] parameters, object instance)
        {
            Entry = entry;
            Parameters = parameters;
            Raw = instance;
            distribution = (IUnivariateDistribution)instance;
        }

        public DistributionEntry Entry { get; }
        public double[] Parameters { get; }
        public bool Discrete => Entry.Discrete;
        public double Mean => Safe(() => distribution.Mean);
        public double Variance => Safe(() => distribution.Variance);
        public double Median => Safe(() => distribution.Median);
        public double Mode => Safe(() => distribution.Mode);
        public double Entropy => Safe(() => distribution.Entropy);

        public double SupportLower
        {
            get
            {
                try
                {
                    return distribution.Support.Min;
                }
                catch (Exception ex) when (ex is NotSupportedException or NotImplementedException)
                {
                    return double.NaN;
                }
            }
        }

        public double SupportUpper
        {
            get
            {
                try
                {
                    return distribution.Support.Max;
                }
                catch (Exception ex) when (ex is NotSupportedException or NotImplementedException)
                {
                    return double.NaN;
                }
            }
        }

        public object Raw { get; }

        public static double[] ValidateParameters(DistributionEntry entry, double[]? parameters, string parameterName)
        {
            parameters ??= Array.Empty<double>();

            if (entry.Variadic)
            {
                if (parameters.Length < 1)
                {
                    throw new ArgumentException(
                        $"{entry.Name} needs at least one probability in {parameterName}: {string.Join(", ", entry.Parameters.Select(p => p.Name))}.",
                        parameterName);
                }
            }
            else if (parameters.Length != entry.Parameters.Length)
            {
                var expected = entry.Parameters.Length == 0
                    ? "no parameters (pass an empty array)"
                    : $"{entry.Parameters.Length} parameter(s) in this order: {string.Join(", ", entry.Parameters.Select(p => p.Name))}";

                throw new ArgumentException(
                    $"{entry.Name} needs {expected}, but {parameterName} has {parameters.Length}. Call ListDistributions for the meaning of each.",
                    parameterName);
            }

            for (var i = 0; i < parameters.Length; i++)
            {
                var spec = entry.Variadic ? entry.Parameters[0] : entry.Parameters[i];
                var label = entry.Variadic ? $"probabilities[{i}]" : spec.Name;
                var value = parameters[i];

                if (!double.IsFinite(value))
                {
                    throw new ArgumentException(
                        $"{entry.Name} parameter {label} is {value}; it must be a finite number.",
                        parameterName);
                }

                switch (spec.Domain)
                {
                    case ParameterDomain.Positive when value <= 0:
                        throw new ArgumentException(
                            $"{entry.Name} parameter {label} must be greater than 0, got {value}.", parameterName);
                    case ParameterDomain.UnitInterval when value < 0 || value > 1:
                        throw new ArgumentException(
                            $"{entry.Name} parameter {label} must be between 0 and 1, got {value}.", parameterName);
                    case ParameterDomain.Integer when Math.Abs(value - Math.Round(value)) > 1e-9:
                        throw new ArgumentException(
                            $"{entry.Name} parameter {label} must be a whole number, got {value}.", parameterName);
                    case ParameterDomain.PositiveInteger when Math.Abs(value - Math.Round(value)) > 1e-9 || value < 1:
                        throw new ArgumentException(
                            $"{entry.Name} parameter {label} must be a whole number of at least 1, got {value}.",
                            parameterName);
                    case ParameterDomain.NonNegativeInteger
                        when Math.Abs(value - Math.Round(value)) > 1e-9 || value < 0:
                        throw new ArgumentException(
                            $"{entry.Name} parameter {label} must be a whole number of at least 0, got {value}.",
                            parameterName);
                }
            }

            if (entry.Variadic)
            {
                if (parameters.Any(p => p < 0))
                {
                    throw new ArgumentException("Every probability must be 0 or more.", parameterName);
                }

                var sum = parameters.Sum();

                if (Math.Abs(sum - 1.0) > 1e-6)
                {
                    throw new ArgumentException($"The probabilities must sum to 1 but sum to {sum}.", parameterName);
                }
            }

            return parameters;
        }

        public static UnivariateModel Create(DistributionKind kind, double[]? parameters,
            string parameterName = "parameters")
        {
            var entry = DistributionCatalogue.Get(kind);
            var checkedParameters = ValidateParameters(entry, parameters, parameterName);
            object instance;

            try
            {
                instance = entry.Factory(checkedParameters);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException
                                           or IndexOutOfRangeException or OverflowException)
            {
                throw new ArgumentException(
                    $"{entry.Name} rejected the parameters ({string.Join(", ", checkedParameters)}): {FirstLine(ex.Message)} Call ListDistributions for the parameter rules.",
                    parameterName);
            }

            return new UnivariateModel(entry, (double[])checkedParameters.Clone(), instance);
        }

        public static UnivariateModel FromInstance(DistributionEntry entry, double[] parameters, object instance)
        {
            return new UnivariateModel(entry, parameters, instance);
        }

        private static string FirstLine(string message)
        {
            var index = message.IndexOfAny(new[] { '\r', '\n' });
            var line = index >= 0 ? message[..index] : message;

            return line.EndsWith('.') ? line : line + ".";
        }

        private static double Safe(Func<double> f)
        {
            try
            {
                return f();
            }
            catch (Exception ex) when (ex is NotSupportedException or NotImplementedException or ArgumentException
                                           or InvalidOperationException or OverflowException or DivideByZeroException
                                           or IndexOutOfRangeException)
            {
                return double.NaN;
            }
        }

        private static double Floor(double x)
        {
            return double.IsInfinity(x) ? x : Math.Floor(x);
        }

        public double Pdf(double x)
        {
            if (double.IsNaN(x))
            {
                return double.NaN;
            }

            if (Discrete)
            {
                if (double.IsInfinity(x) || Math.Abs(x - Math.Round(x)) > 1e-9)
                {
                    return 0.0;
                }

                return Safe(() => distribution.ProbabilityFunction(Math.Round(x)));
            }

            return Safe(() => distribution.ProbabilityFunction(x));
        }

        public double LogPdf(double x)
        {
            if (double.IsNaN(x))
            {
                return double.NaN;
            }

            if (Discrete)
            {
                if (double.IsInfinity(x) || Math.Abs(x - Math.Round(x)) > 1e-9)
                {
                    return double.NegativeInfinity;
                }

                return Safe(() => distribution.LogProbabilityFunction(Math.Round(x)));
            }

            return Safe(() => distribution.LogProbabilityFunction(x));
        }

        public double Cdf(double x)
        {
            if (double.IsNaN(x))
            {
                return double.NaN;
            }

            return Safe(() => distribution.DistributionFunction(Discrete ? Floor(x) : x));
        }

        public double Ccdf(double x)
        {
            if (double.IsNaN(x))
            {
                return double.NaN;
            }

            return Safe(() => distribution.ComplementaryDistributionFunction(Discrete ? Floor(x) : x));
        }

        public double Quantile(double p)
        {
            if (double.IsNaN(p) || p < 0 || p > 1)
            {
                return double.NaN;
            }

            return Safe(() => distribution.InverseDistributionFunction(p));
        }

        public double Hazard(double x)
        {
            if (double.IsNaN(x))
            {
                return double.NaN;
            }

            return Safe(() => distribution.HazardFunction(Discrete ? Floor(x) : x));
        }

        public double CumulativeHazard(double x)
        {
            if (double.IsNaN(x))
            {
                return double.NaN;
            }

            return Safe(() => distribution.CumulativeHazardFunction(Discrete ? Floor(x) : x));
        }

        public double Between(double a, double b)
        {
            if (double.IsNaN(a) || double.IsNaN(b))
            {
                return double.NaN;
            }

            var upper = Cdf(b);
            var lower = Discrete ? Cdf(Math.Ceiling(a) - 1) : Cdf(a);

            return upper - lower;
        }

        public double[] Generate(int count, Random random)
        {
            if (Raw is UnivariateContinuousDistribution continuous)
            {
                return continuous.Generate(count, random);
            }

            var discrete = (UnivariateDiscreteDistribution)Raw;
            var ints = discrete.Generate(count, new int[count], random);
            var result = new double[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ints[i];
            }

            return result;
        }

        public double LogLikelihood(IReadOnlyList<double> data)
        {
            var sum = 0.0;

            for (var i = 0; i < data.Count; i++)
            {
                var l = LogPdf(data[i]);

                if (double.IsNaN(l))
                {
                    return double.NaN;
                }

                sum += l;
            }

            return sum;
        }
    }
}