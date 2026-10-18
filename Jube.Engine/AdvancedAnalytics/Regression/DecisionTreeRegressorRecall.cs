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
    public sealed class DecisionTreeRegressorRecall
    {
        public const string ModelKind = "DecisionTreeRegressorPipeline";
        private readonly DecisionTreeRegressorPipeline pipeline;

        private DecisionTreeRegressorRecall(DecisionTreeRegressorPipeline pipeline, IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }

        public static DecisionTreeRegressorRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsDecisionTreeRegressorPipelineSnapshot>(
                    snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        internal static DecisionTreeRegressorRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsDecisionTreeRegressorPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;

            var pipeline = new DecisionTreeRegressorPipeline(payload.FillValues, payload.IsLeaf,
                payload.AttributeIndex, payload.Threshold, payload.LeftChild, payload.RightChild,
                payload.NodeValue);

            return new DecisionTreeRegressorRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static DecisionTreeRegressorRecall FromPipeline(DecisionTreeRegressorPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new DecisionTreeRegressorRecall(pipeline, variables);
        }

        public double Value(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return pipeline.LeafValue(input);
        }

        public double[] Predict(double[] input)
        {
            return [Value(input)];
        }

        public DecisionTreeRegressorExplain Explain(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var prepared = pipeline.Prepare(input);
            var path = pipeline.Path(input);
            var attributeIndex = pipeline.AttributeIndex;
            var threshold = pipeline.Threshold;
            var nodeValue = pipeline.NodeValue;
            var contributions = new List<DecisionTreeRegressorContribution>();

            for (var depth = 0; depth < path.Count - 1; depth++)
            {
                var node = path[depth];
                var child = path[depth + 1];
                var variable = attributeIndex[node];
                var wentRight = child == pipeline.RightChild[node];
                var delta = (nodeValue[child] - nodeValue[node]) / (depth + 1);

                contributions.Add(new DecisionTreeRegressorContribution
                {
                    Name = Variables[variable],
                    Weight = delta,
                    Depth = depth,
                    Threshold = threshold[node],
                    InputValue = prepared[variable],
                    WentRight = wentRight
                });
            }

            return new DecisionTreeRegressorExplain
            {
                RootValue = nodeValue[0],
                LeafValue = nodeValue[path[^1]],
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

                result[i] = Value(inputs[i]);
            }

            return result;
        }

        public Func<double[], double[]> AsFunction()
        {
            return Predict;
        }
    }
}