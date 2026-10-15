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

using Accord.Statistics.Distributions.Fitting;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    internal enum ParameterDomain
    {
        Real,
        Positive,
        UnitInterval,
        Integer,
        PositiveInteger,
        NonNegativeInteger
    }

    internal enum FitMode
    {
        None,
        Library,
        Numeric
    }

    internal enum DataDomain
    {
        Any,
        Positive,
        NonNegative,
        OpenUnit,
        NonNegativeInteger,
        BinaryZeroOne,
        PositiveInteger,
        Integer
    }

    internal sealed record ParameterSpec(string Name, string Description, ParameterDomain Domain, double Example);

    internal sealed class DistributionEntry
    {
        public DistributionEntry(
            DistributionKind kind,
            bool discrete,
            string description,
            string support,
            string uses,
            ParameterSpec[] parameters,
            Func<double[], object> factory)
        {
            Kind = kind;
            Discrete = discrete;
            Description = description;
            Support = support;
            Uses = uses;
            Parameters = parameters;
            Factory = factory;
        }

        public DistributionKind Kind { get; }
        public bool Discrete { get; }
        public string Description { get; }
        public string Support { get; }
        public string Uses { get; }
        public ParameterSpec[] Parameters { get; }
        public Func<double[], object> Factory { get; }
        public bool Variadic { get; init; }
        public FitMode Fit { get; init; } = FitMode.None;
        public Func<object, double[]>? Extract { get; init; }
        public Func<IFittingOptions>? LibraryOptions { get; init; }
        public Func<SampleProfile, double[]>? Start { get; init; }
        public DataDomain DataDomain { get; init; } = DataDomain.Any;
        public int[] HeldByDefault { get; init; } = Array.Empty<int>();
        public string FitNote { get; init; } = string.Empty;
        public string Name => Kind.ToString();

        public double[] Examples
        {
            get
            {
                var examples = new double[Parameters.Length];

                for (var i = 0; i < examples.Length; i++)
                {
                    examples[i] = Parameters[i].Example;
                }

                return examples;
            }
        }
    }
}