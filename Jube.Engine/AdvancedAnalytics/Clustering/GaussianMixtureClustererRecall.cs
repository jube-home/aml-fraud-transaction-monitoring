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
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;

namespace Jube.Engine.AdvancedAnalytics.Clustering
{
    public sealed class GaussianMixtureClustererRecall
    {
        public const string ModelKind = "GaussianMixtureClustererPipeline";
        private readonly GaussianMixtureClustererPipeline pipeline;

        private GaussianMixtureClustererRecall(GaussianMixtureClustererPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        internal static GaussianMixtureClustererRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsGaussianMixtureClustererPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;
            var mixtureWeights = payload.Components.Select(c => c.MixtureWeight).ToArray();
            var componentMeans = payload.Components.Select(c => (double[])c.Mean.Clone()).ToArray();

            var componentCovariances = payload.Components
                .Select(c => AdvancedAnalyticsGaussianMixtureClustererPipelineSnapshot.ToRectangular(c.Covariance))
                .ToArray();

            var pipeline = new GaussianMixtureClustererPipeline(payload.Standardise, payload.FillValues, payload.Means,
                payload.StandardDeviations, mixtureWeights, componentMeans, componentCovariances);

            return new GaussianMixtureClustererRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static GaussianMixtureClustererRecall FromPipeline(GaussianMixtureClustererPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new GaussianMixtureClustererRecall(pipeline, variables);
        }

        public IReadOnlyList<string> Variables { get; }
        public int ComponentCount => pipeline.ComponentCount;

        public static GaussianMixtureClustererRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope = AdvancedAnalyticsModelJson
                .Deserialise<AdvancedAnalyticsGaussianMixtureClustererPipelineSnapshot>(snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var posteriorsByComponentIndex = pipeline.PosteriorsByComponentIndex(input);

            return WithHighestPosteriorMovedToTheFront(posteriorsByComponentIndex);
        }

        public double[][] PredictPosteriors(double[][] inputs)
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

        private static double[] WithHighestPosteriorMovedToTheFront(double[] posteriorsByComponentIndex)
        {
            var best = 0;

            for (var c = 1; c < posteriorsByComponentIndex.Length; c++)
            {
                if (posteriorsByComponentIndex[c] > posteriorsByComponentIndex[best])
                {
                    best = c;
                }
            }

            var reordered = (double[])posteriorsByComponentIndex.Clone();

            if (best != 0)
            {
                (reordered[0], reordered[best]) = (reordered[best], reordered[0]);
            }

            return reordered;
        }
    }
}