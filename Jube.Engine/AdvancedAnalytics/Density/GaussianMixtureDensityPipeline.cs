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

using System;
using System.Linq;
using Jube.Analytics;
using Jube.Engine.AdvancedAnalytics.Density.Models;

namespace Jube.Engine.AdvancedAnalytics.Density
{
    internal sealed class GaussianMixtureDensityPipeline(
        bool standardise,
        double[] fill,
        double[] means,
        double[] standardDeviations,
        int[] activeInputs,
        GaussianMixtureDensityComponent[] components)
    {
        private const double LogTwoPi = 1.8378770664093454835606594728112;
        private readonly int[] activeInputs = (int[])activeInputs.Clone();
        private readonly double[] fill = (double[])fill.Clone();
        private readonly double[] means = (double[])means.Clone();
        private readonly double[] standardDeviations = (double[])standardDeviations.Clone();

        public int InputCount => fill.Length;
        public bool Standardise { get; } = standardise;
        public double[] Fill => (double[])fill.Clone();
        public double[] Means => (double[])means.Clone();
        public double[] StandardDeviations => (double[])standardDeviations.Clone();
        public int[] ActiveInputs => (int[])activeInputs.Clone();
        public int ComponentCount => components.Length;

        public double LogDensity(double[] row)
        {
            var count = fill.Length;

            if (row.Length != count)
            {
                throw new ArgumentException($"A row has {row.Length} values but the model expects {count}.");
            }

            var active = Active(Standardised(row));
            var terms = new double[components.Length];

            for (var k = 0; k < components.Length; k++)
            {
                var centred = new double[active.Length];

                for (var i = 0; i < active.Length; i++)
                {
                    centred[i] = active[i] - components[k].Mean[i];
                }

                terms[k] = Math.Log(components[k].Weight) + ComponentLogPdf(components[k], centred);
            }

            return LogSumExp(terms);
        }

        public GaussianMixtureDensityRawExplain Explain(double[] row)
        {
            var count = fill.Length;

            if (row.Length != count)
            {
                throw new ArgumentException($"A row has {row.Length} values but the model expects {count}.");
            }

            var standardised = Standardised(row);
            var active = Active(standardised);
            var d = active.Length;
            var componentTerms = new double[components.Length];
            var centredByComponent = new double[components.Length][];

            for (var k = 0; k < components.Length; k++)
            {
                var centred = new double[d];

                for (var i = 0; i < d; i++)
                {
                    centred[i] = active[i] - components[k].Mean[i];
                }

                centredByComponent[k] = centred;
                componentTerms[k] = Math.Log(components[k].Weight) + ComponentLogPdf(components[k], centred);
            }

            var logDensity = LogSumExp(componentTerms);
            var dominant = 0;

            for (var k = 1; k < componentTerms.Length; k++)
            {
                if (componentTerms[k] > componentTerms[dominant])
                {
                    dominant = k;
                }
            }

            var dominantComponent = components[dominant];
            var dominantCentred = centredByComponent[dominant];

            var baseValue = Math.Log(dominantComponent.Weight) -
                            0.5 * (d * LogTwoPi + dominantComponent.LogDeterminant);

            var terms = new double[count];

            for (var i = 0; i < d; i++)
            {
                terms[activeInputs[i]] = -0.5 * dominantComponent.InverseCovariance[i][i] *
                                         dominantCentred[i] * dominantCentred[i];
            }

            return new GaussianMixtureDensityRawExplain(logDensity, dominant, baseValue, terms);
        }

        private double[] Standardised(double[] row)
        {
            var count = fill.Length;
            var values = new double[count];

            for (var j = 0; j < count; j++)
            {
                var value = double.IsFinite(row[j]) ? row[j] : fill[j];
                values[j] = Standardise ? Standardisation.ZScore(value, means[j], standardDeviations[j]) : value;
            }

            return values;
        }

        private double[] Active(double[] standardised)
        {
            var d = activeInputs.Length;
            var active = new double[d];

            for (var i = 0; i < d; i++)
            {
                active[i] = standardised[activeInputs[i]];
            }

            return active;
        }

        private static double ComponentLogPdf(GaussianMixtureDensityComponent component, double[] centred)
        {
            var d = centred.Length;
            var quadratic = 0d;

            for (var a = 0; a < d; a++)
            for (var b = 0; b < d; b++)
            {
                quadratic += centred[a] * component.InverseCovariance[a][b] * centred[b];
            }

            return -0.5 * (d * LogTwoPi + component.LogDeterminant + quadratic);
        }

        private static double LogSumExp(double[] terms)
        {
            var max = terms.Max();

            if (double.IsNegativeInfinity(max))
            {
                return double.NegativeInfinity;
            }

            var sum = terms.Sum(t => Math.Exp(t - max));

            return max + Math.Log(sum);
        }
    }
}