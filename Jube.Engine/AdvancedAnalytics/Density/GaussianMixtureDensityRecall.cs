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
using Jube.Engine.AdvancedAnalytics.Density.Models;

namespace Jube.Engine.AdvancedAnalytics.Density
{
    public sealed class GaussianMixtureDensityRecall
    {
        public const string ModelKind = "GaussianMixtureDensityPipeline";
        private readonly GaussianMixtureDensityPipeline pipeline;

        private GaussianMixtureDensityRecall(GaussianMixtureDensityPipeline pipeline, IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }

        public static GaussianMixtureDensityRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsGaussianMixtureDensityPipelineSnapshot>(
                    snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return [pipeline.LogDensity(input)];
        }

        public GaussianMixtureDensityExplain Explain(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var explain = pipeline.Explain(input);
            var contributions = new GaussianMixtureDensityContribution[explain.Terms.Length];

            for (var i = 0; i < explain.Terms.Length; i++)
            {
                contributions[i] =
                    new GaussianMixtureDensityContribution { Name = Variables[i], Weight = explain.Terms[i] };
            }

            return new GaussianMixtureDensityExplain
            {
                LogDensity = explain.LogDensity,
                DominantComponent = explain.DominantComponent,
                BaseValue = explain.BaseValue,
                Contributions = contributions
            };
        }

        public double[] PredictLogDensities(double[][] inputs)
        {
            ArgumentNullException.ThrowIfNull(inputs);

            var result = new double[inputs.Length];

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null)
                {
                    throw new ArgumentException($"Row {i} is null.");
                }

                result[i] = Predict(inputs[i])[0];
            }

            return result;
        }

        public Func<double[], double[]> AsFunction()
        {
            return Predict;
        }

        internal static GaussianMixtureDensityRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsGaussianMixtureDensityPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;

            var components = payload.Components.Select(c => new GaussianMixtureDensityComponent(
                c.Weight, (double[])c.Mean.Clone(),
                [.. c.InverseCovariance.Select(row => (double[])row.Clone())], c.LogDeterminant)).ToArray();

            var pipeline = new GaussianMixtureDensityPipeline(payload.Standardise, payload.FillValues, payload.Means,
                payload.StandardDeviations, payload.ActiveInputs, components);

            return new GaussianMixtureDensityRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static GaussianMixtureDensityRecall FromPipeline(GaussianMixtureDensityPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new GaussianMixtureDensityRecall(pipeline, variables);
        }
    }
}