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
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.AdvancedAnalytics.Models
{
    public sealed class AdvancedAnalyticsGaussianMixtureClustererPipelineSnapshot : AdvancedAnalyticsSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public bool Standardise { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double[] FillValues { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Means { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] StandardDeviations { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public GaussianMixtureSnapshotComponent[] Components { get; set; } =
            [];

        public override void Validate(int inputCount)
        {
            RequireLength(nameof(FillValues), FillValues, inputCount);
            RequireLength(nameof(Means), Means, inputCount);
            RequireLength(nameof(StandardDeviations), StandardDeviations, inputCount);

            if (StandardDeviations.Any(v => v < 0))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot standard deviations must not be negative.");
            }

            foreach (var (name, values) in new[]
                     {
                         (nameof(FillValues), FillValues), (nameof(Means), Means),
                         (nameof(StandardDeviations), StandardDeviations)
                     })
            {
                if (values.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException($"Snapshot array '{name}' must be finite.");
                }
            }

            if (Components == null || Components.Length == 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot has no components.");
            }

            var weightSum = 0d;

            for (var i = 0; i < Components.Length; i++)
            {
                var component = Components[i];

                if (component == null)
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot 'Components' contains a missing entry.");
                }

                if (component.Index < 0 || component.Index >= Components.Length)
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot component index is out of range.");
                }

                if (double.IsNaN(component.MixtureWeight) || double.IsInfinity(component.MixtureWeight)
                                                          || component.MixtureWeight < 0 || component.MixtureWeight > 1)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        "Snapshot component mixture weight must be between 0 and 1.");
                }

                weightSum += component.MixtureWeight;

                if (component.Mean == null || component.Mean.Length != inputCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Every component mean must have {inputCount} elements.");
                }

                if (component.Mean.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot component means must be finite.");
                }

                if (component.Covariance == null || component.Covariance.Length != inputCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Every component covariance must have {inputCount} rows.");
                }

                foreach (var row in component.Covariance)
                {
                    if (row == null || row.Length != inputCount)
                    {
                        throw new AdvancedAnalyticsSnapshotException(
                            $"Every component covariance row must have {inputCount} elements.");
                    }

                    if (row.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                    {
                        throw new AdvancedAnalyticsSnapshotException("Snapshot component covariances must be finite.");
                    }
                }
            }

            if (Math.Abs(weightSum - 1d) > 1e-6)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot component mixture weights must sum to 1.");
            }

            if (Components.Select(c => c.Index).Distinct().Count() != Components.Length)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot 'Components' indices must be distinct.");
            }
        }

        public static double[][] ToJagged(double[,] rectangular)
        {
            var rows = rectangular.GetLength(0);
            var columns = rectangular.GetLength(1);
            var jagged = new double[rows][];

            for (var i = 0; i < rows; i++)
            {
                jagged[i] = new double[columns];

                for (var j = 0; j < columns; j++)
                {
                    jagged[i][j] = rectangular[i, j];
                }
            }

            return jagged;
        }

        public static double[,] ToRectangular(double[][] jagged)
        {
            var rows = jagged.Length;
            var columns = rows == 0 ? 0 : jagged[0].Length;
            var rectangular = new double[rows, columns];

            for (var i = 0; i < rows; i++)
            for (var j = 0; j < columns; j++)
            {
                rectangular[i, j] = jagged[i][j];
            }

            return rectangular;
        }
    }
}