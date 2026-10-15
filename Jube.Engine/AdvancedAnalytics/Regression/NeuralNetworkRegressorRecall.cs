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
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public sealed class NeuralNetworkRegressorRecall
    {
        public const string ModelKind = "NeuralNetworkRegressorPipeline";
        private readonly NeuralNetworkRegressorPipeline pipeline;

        private NeuralNetworkRegressorRecall(NeuralNetworkRegressorPipeline pipeline, IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }

        public static NeuralNetworkRegressorRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope = AdvancedAnalyticsModelJson
                .Deserialise<AdvancedAnalyticsNeuralNetworkRegressorPipelineSnapshot>(snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        internal static NeuralNetworkRegressorRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsNeuralNetworkRegressorPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;

            var pipeline = new NeuralNetworkRegressorPipeline(payload.Standardise, payload.FillValues, payload.Means,
                payload.StandardDeviations, payload.InputWeights, payload.HiddenBiases, payload.OutputWeights,
                payload.OutputBias, payload.TargetLow, payload.TargetSpan);

            return new NeuralNetworkRegressorRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static NeuralNetworkRegressorRecall FromPipeline(NeuralNetworkRegressorPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new NeuralNetworkRegressorRecall(pipeline, variables);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return [pipeline.PredictedValue(input)];
        }

        public NeuralNetworkRegressorExplain Explain(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var predictedValue = pipeline.PredictedValue(input);
            var gradient = pipeline.PredictedValueGradient(input);
            var contributions = new NeuralNetworkRegressorContribution[gradient.Length];

            for (var i = 0; i < gradient.Length; i++)
            {
                contributions[i] = new NeuralNetworkRegressorContribution { Name = Variables[i], Weight = gradient[i] };
            }

            return new NeuralNetworkRegressorExplain
            {
                PredictedValue = predictedValue,
                Contributions = contributions
            };
        }

        public double[] PredictValues(double[][] inputs)
        {
            ArgumentNullException.ThrowIfNull(inputs);

            var result = new double[inputs.Length];

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null)
                {
                    throw new ArgumentException($"Row {i} is null.");
                }

                result[i] = pipeline.PredictedValue(inputs[i]);
            }

            return result;
        }

        public Func<double[], double[]> AsFunction()
        {
            return Predict;
        }
    }
}