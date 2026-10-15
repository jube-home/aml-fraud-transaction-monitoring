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
    public sealed class KModesClustererRecall
    {
        public const string ModelKind = "KModesClustererPipeline";
        private readonly KModesClustererPipeline pipeline;

        private KModesClustererRecall(KModesClustererPipeline pipeline, IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        internal static KModesClustererRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsKModesClustererPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;
            var pipeline = new KModesClustererPipeline(payload.FillValues, payload.Modes);

            return new KModesClustererRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static KModesClustererRecall FromPipeline(KModesClustererPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new KModesClustererRecall(pipeline, variables);
        }

        public IReadOnlyList<string> Variables { get; }
        public int ClusterCount => pipeline.ClusterCount;

        public static KModesClustererRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsKModesClustererPipelineSnapshot>(
                snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var distancesByClusterIndex = pipeline.DistancesByClusterIndex(input);

            return WithNearestModeDistanceMovedToTheFront(distancesByClusterIndex);
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

        private static double[] WithNearestModeDistanceMovedToTheFront(double[] distancesByClusterIndex)
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