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
using System.Collections.Generic;
using System.Linq;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json;

namespace Jube.Engine.AdvancedAnalytics.Sequences
{
    public sealed class AdvancedAnalyticsSequenceSet
    {
        private readonly int[] offsets;
        private readonly int[] sequenceLengths;
        private readonly double[] values;

        [JsonConstructor]
        public AdvancedAnalyticsSequenceSet(double[] values, int featureCount, int[] sequenceLengths)
        {
            if (values == null)
            {
                throw new AdvancedAnalyticsSnapshotException("A sequence set needs values.");
            }

            if (featureCount < 1)
            {
                throw new AdvancedAnalyticsSnapshotException("Feature count must be at least one.");
            }

            if (values.Length % featureCount != 0)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"There are {values.Length} values, which is not a whole number of {featureCount}-feature observations.");
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (!double.IsFinite(values[i]))
                {
                    throw new AdvancedAnalyticsSnapshotException($"Value at position {i} is not finite.");
                }
            }

            this.sequenceLengths =
                AdvancedAnalyticsSequenceLayout.CopyLengths(sequenceLengths, values.Length / featureCount,
                    "observations");

            this.values = (double[])values.Clone();
            FeatureCount = featureCount;
            offsets = AdvancedAnalyticsSequenceLayout.Offsets(this.sequenceLengths);
        }

        [JsonProperty("values")] public IReadOnlyList<double> Values => Array.AsReadOnly(values);
        [JsonProperty("featureCount")] public int FeatureCount { get; }

        [JsonProperty("sequenceLengths")]
        public IReadOnlyList<int> SequenceLengths => Array.AsReadOnly(sequenceLengths);

        [JsonIgnore] public int SequenceCount => sequenceLengths.Length;
        [JsonIgnore] public int ObservationCount => values.Length / FeatureCount;

        public int Offset(int sequence)
        {
            RequireIndex(sequence);

            return offsets[sequence];
        }

        public double[][] GetSequence(int sequence)
        {
            RequireIndex(sequence);

            var rows = new double[sequenceLengths[sequence]][];

            for (var t = 0; t < rows.Length; t++)
            {
                rows[t] = new double[FeatureCount];
                Array.Copy(values, (offsets[sequence] + t) * FeatureCount, rows[t], 0, FeatureCount);
            }

            return rows;
        }

        public double[][][] ToJagged()
        {
            var sequences = new double[sequenceLengths.Length][][];

            for (var i = 0; i < sequences.Length; i++)
            {
                sequences[i] = GetSequence(i);
            }

            return sequences;
        }

        public static AdvancedAnalyticsSequenceSet FromJagged(double[][][] sequences)
        {
            if (sequences == null || sequences.Length == 0)
            {
                throw new AdvancedAnalyticsSnapshotException("A sequence set needs at least one sequence.");
            }

            var featureCount = sequences[0].FirstOrDefault()?.Length ?? 0;
            var flat = new List<double>();
            var lengths = new int[sequences.Length];

            for (var i = 0; i < sequences.Length; i++)
            {
                var sequence = sequences[i] ??
                               throw new AdvancedAnalyticsSnapshotException($"Sequence {i} is missing.");

                lengths[i] = sequence.Length;

                foreach (var row in sequence)
                {
                    if (row == null || row.Length != featureCount)
                    {
                        throw new AdvancedAnalyticsSnapshotException(
                            $"Sequence {i} has an observation that does not have {featureCount} features.");
                    }

                    flat.AddRange(row);
                }
            }

            return new AdvancedAnalyticsSequenceSet([.. flat], featureCount, lengths);
        }

        private void RequireIndex(int sequence)
        {
            if (sequence < 0 || sequence >= sequenceLengths.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }
        }
    }
}