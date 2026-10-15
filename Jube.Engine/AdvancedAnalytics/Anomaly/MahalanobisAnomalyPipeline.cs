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
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;

namespace Jube.Engine.AdvancedAnalytics.Anomaly
{
    internal sealed class MahalanobisAnomalyPipeline
    {
        private readonly int[] activeInputs;
        private readonly double[] centre;
        private readonly double[] fill;
        private readonly double[][] inverseCovariance;
        private readonly double[] means;
        private readonly double[] standardDeviations;

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

        public MahalanobisAnomalyPipeline(bool standardise, double[] fill, double[] means, double[] standardDeviations,
            int[] activeInputs, double[] centre, double[][] inverseCovariance, Calibrator? calibrator)
        {
            Standardise = standardise;
            this.fill = (double[])fill.Clone();
            this.means = (double[])means.Clone();
            this.standardDeviations = (double[])standardDeviations.Clone();
            this.activeInputs = (int[])activeInputs.Clone();
            this.centre = (double[])centre.Clone();
            this.inverseCovariance = [.. inverseCovariance.Select(row => (double[])row.Clone())];
            Calibrator = calibrator;
        }

        public int InputCount => fill.Length;
        public bool Standardise { get; }
        public double[] Fill => (double[])fill.Clone();
        public double[] Means => (double[])means.Clone();
        public double[] StandardDeviations => (double[])standardDeviations.Clone();
        public int[] ActiveInputs => (int[])activeInputs.Clone();
        public double[] Centre => (double[])centre.Clone();
        public double[][] InverseCovariance => [.. inverseCovariance.Select(row => (double[])row.Clone())];
        public Calibrator? Calibrator { get; }

        public MahalanobisAnomalyRawExplain Explain(double[] row)
        {
            var count = fill.Length;

            if (row.Length != count)
            {
                throw new ArgumentException($"A row has {row.Length} values but the model expects {count}.");
            }

            var values = Standardised(row);
            var d = activeInputs.Length;
            var centred = new double[d];

            for (var i = 0; i < d; i++)
            {
                centred[i] = values[activeInputs[i]] - centre[i];
            }

            var terms = new double[count];

            for (var i = 0; i < d; i++)
            {
                terms[activeInputs[i]] = centred[i] * inverseCovariance[i][i] * centred[i];
            }

            var squared = 0d;

            for (var a = 0; a < d; a++)
            for (var b = 0; b < d; b++)
            {
                squared += centred[a] * inverseCovariance[a][b] * centred[b];
            }

            return new MahalanobisAnomalyRawExplain(Math.Sqrt(Math.Max(squared, 0d)), terms);
        }

        public double Distance(double[] row)
        {
            return Explain(row).Distance;
        }
    }
}