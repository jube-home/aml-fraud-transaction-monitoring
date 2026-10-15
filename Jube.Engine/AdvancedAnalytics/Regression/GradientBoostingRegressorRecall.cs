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
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public sealed class GradientBoostingRegressorRecall
    {
        public const string ModelKind = "GradientBoostingRegressorPipeline";
        private readonly GradientBoostingRegressorPipeline pipeline;

        private GradientBoostingRegressorRecall(GradientBoostingRegressorPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }

        public static GradientBoostingRegressorRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope = AdvancedAnalyticsModelJson
                .Deserialise<AdvancedAnalyticsGradientBoostingRegressorPipelineSnapshot>(
                    snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        internal static GradientBoostingRegressorRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsGradientBoostingRegressorPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;

            var trees = payload.Trees
                .Select(t => new GradientBoostingRegressorTree(t.FeatureIndex, t.Threshold, t.Left, t.Right, t.Value))
                .ToArray();

            var pipeline = new GradientBoostingRegressorPipeline(payload.BaseValue, payload.LearningRate,
                payload.FillValues, payload.Means, payload.StandardDeviations, trees);

            return new GradientBoostingRegressorRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static GradientBoostingRegressorRecall FromPipeline(GradientBoostingRegressorPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new GradientBoostingRegressorRecall(pipeline, variables);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return [pipeline.Predict(input)];
        }

        public GradientBoostingRegressorExplain Explain(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var (predictedValue, baseValue, contributions) = pipeline.Explain(input);
            var items = new GradientBoostingRegressorContribution[contributions.Length];

            for (var i = 0; i < contributions.Length; i++)
            {
                items[i] = new GradientBoostingRegressorContribution
                    { Name = Variables[i], Weight = contributions[i] };
            }

            return new GradientBoostingRegressorExplain
            {
                PredictedValue = predictedValue,
                BaseValue = baseValue,
                Contributions = items
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

                result[i] = pipeline.Predict(inputs[i]);
            }

            return result;
        }

        public Func<double[], double[]> AsFunction()
        {
            return Predict;
        }
    }
}