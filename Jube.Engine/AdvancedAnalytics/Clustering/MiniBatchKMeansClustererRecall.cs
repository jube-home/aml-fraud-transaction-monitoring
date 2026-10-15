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
    public sealed class MiniBatchKMeansClustererRecall
    {
        public const string ModelKind = "MiniBatchKMeansClustererPipeline";
        private readonly MiniBatchKMeansClustererPipeline pipeline;

        private MiniBatchKMeansClustererRecall(MiniBatchKMeansClustererPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }
        public int ClusterCount => pipeline.ClusterCount;

        public static MiniBatchKMeansClustererRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope = AdvancedAnalyticsModelJson
                .Deserialise<AdvancedAnalyticsMiniBatchKMeansClustererPipelineSnapshot>(snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        internal static MiniBatchKMeansClustererRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsMiniBatchKMeansClustererPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;

            var pipeline = new MiniBatchKMeansClustererPipeline(payload.Standardise, payload.FillValues, payload.Means,
                payload.StandardDeviations, payload.Centroids);

            return new MiniBatchKMeansClustererRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static MiniBatchKMeansClustererRecall FromPipeline(MiniBatchKMeansClustererPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new MiniBatchKMeansClustererRecall(pipeline, variables);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var distancesByClusterIndex = pipeline.DistancesByClusterIndex(input);

            return WithNearestCentroidDistanceMovedToTheFront(distancesByClusterIndex);
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

        private static double[] WithNearestCentroidDistanceMovedToTheFront(double[] distancesByClusterIndex)
        {
            var nearest = 0;

            for (var c = 1; c < distancesByClusterIndex.Length; c++)
            {
                if (distancesByClusterIndex[c] < distancesByClusterIndex[nearest])
                {
                    nearest = c;
                }
            }

            var reordered = (double[])distancesByClusterIndex.Clone();

            if (nearest != 0)
            {
                (reordered[0], reordered[nearest]) = (reordered[nearest], reordered[0]);
            }

            return reordered;
        }
    }
}