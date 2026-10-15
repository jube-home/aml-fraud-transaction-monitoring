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
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;

namespace Jube.Engine.AdvancedAnalytics.Clustering
{
    public sealed class MeanShiftClustererRecall
    {
        public const string ModelKind = "MeanShiftClustererPipeline";
        private readonly MeanShiftClustererPipeline pipeline;

        private MeanShiftClustererRecall(MeanShiftClustererPipeline pipeline, IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }
        public int ClusterCount => pipeline.ClusterCount;

        public static MeanShiftClustererRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsMeanShiftClustererPipelineSnapshot>(
                snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        internal static MeanShiftClustererRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsMeanShiftClustererPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;

            var pipeline = new MeanShiftClustererPipeline(payload.Standardise, payload.FillValues, payload.Means,
                payload.StandardDeviations, payload.Modes);

            return new MeanShiftClustererRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static MeanShiftClustererRecall FromPipeline(MeanShiftClustererPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new MeanShiftClustererRecall(pipeline, variables);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var distancesByModeIndex = pipeline.DistancesByModeIndex(input);

            return WithNearestModeDistanceMovedToTheFront(distancesByModeIndex);
        }

        public double[][] PredictDistances(double[][] inputs)
        {
            ArgumentNullException.ThrowIfNull(inputs);

            var result = new double[inputs.Length][];

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null)
                {
                    throw new ArgumentException($"Row {i} is null.");
                }

                result[i] = Predict(inputs[i]);
            }

            return result;
        }

        public Func<double[], double[]> AsFunction()
        {
            return Predict;
        }

        private static double[] WithNearestModeDistanceMovedToTheFront(double[] distancesByModeIndex)
        {
            var nearest = 0;

            for (var c = 1; c < distancesByModeIndex.Length; c++)
            {
                if (distancesByModeIndex[c] < distancesByModeIndex[nearest])
                {
                    nearest = c;
                }
            }

            var reordered = (double[])distancesByModeIndex.Clone();

            if (nearest != 0)
            {
                (reordered[0], reordered[nearest]) = (reordered[nearest], reordered[0]);
            }

            return reordered;
        }
    }
}