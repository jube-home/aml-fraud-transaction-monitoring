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
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    public sealed class DecisionTreeClassifierRecall
    {
        public const string ModelKind = "DecisionTreeClassifierPipeline";
        public const string CalibratorMethod = "Platt";
        private readonly Calibrator? calibrator;
        private readonly DecisionTreeClassifierPipeline pipeline;

        private DecisionTreeClassifierRecall(DecisionTreeClassifierPipeline pipeline, Calibrator? calibrator,
            IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            this.calibrator = calibrator;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }
        public bool Calibrated => calibrator.HasValue;

        public static DecisionTreeClassifierRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsDecisionTreeClassifierPipelineSnapshot>(
                    snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        internal static DecisionTreeClassifierRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsDecisionTreeClassifierPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;

            var pipeline = new DecisionTreeClassifierPipeline(payload.FillValues, payload.IsLeaf,
                payload.AttributeIndex, payload.Threshold, payload.LeftChild, payload.RightChild,
                payload.NodeProbability);

            Calibrator? calibrator = null;

            if (envelope.Calibrator != null)
            {
                if (!string.Equals(envelope.Calibrator.Method, CalibratorMethod, StringComparison.Ordinal))
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Calibrator method '{envelope.Calibrator.Method}' is not supported.");
                }

                try
                {
                    calibrator = Calibrator.FromPrimitives(envelope.Calibrator.Parameters);
                }
                catch (ArgumentException exception)
                {
                    throw new AdvancedAnalyticsSnapshotException(exception.Message);
                }

                if (calibrator.Value.Mode != CalibrationInput.LogitOfBoundedScore)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        "Only logit-of-bounded-score calibration is supported.");
                }
            }

            return new DecisionTreeClassifierRecall(pipeline, calibrator,
                Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static DecisionTreeClassifierRecall FromPipeline(DecisionTreeClassifierPipeline pipeline,
            Calibrator? calibrator, IReadOnlyList<string> variables)
        {
            return new DecisionTreeClassifierRecall(pipeline, calibrator, variables);
        }

        public double Probability(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var leafProbability = pipeline.LeafProbability(input);

            return calibrator?.Apply(leafProbability) ?? leafProbability;
        }

        public double[] Predict(double[] input)
        {
            return [Probability(input)];
        }

        public DecisionTreeClassifierExplain Explain(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var prepared = pipeline.Prepare(input);
            var path = pipeline.Path(input);
            var attributeIndex = pipeline.AttributeIndex;
            var threshold = pipeline.Threshold;
            var nodeProbability = pipeline.NodeProbability;
            var contributions = new List<DecisionTreeContribution>();

            for (var depth = 0; depth < path.Count - 1; depth++)
            {
                var node = path[depth];
                var child = path[depth + 1];
                var variable = attributeIndex[node];
                var wentRight = child == pipeline.RightChild[node];
                var delta = (nodeProbability[child] - nodeProbability[node]) / (depth + 1);

                contributions.Add(new DecisionTreeContribution
                {
                    Name = Variables[variable],
                    Weight = delta,
                    Depth = depth,
                    Threshold = threshold[node],
                    InputValue = prepared[variable],
                    WentRight = wentRight
                });
            }

            return new DecisionTreeClassifierExplain
            {
                RootProbability = nodeProbability[0],
                LeafProbability = nodeProbability[path[^1]],
                Contributions = contributions,
                Calibrated = Calibrated,
                CalibrationSlope = calibrator?.Slope,
                CalibrationIntercept = calibrator?.Intercept
            };
        }

        public double[] PredictProbabilities(double[][] inputs)
        {
            ArgumentNullException.ThrowIfNull(inputs);

            var result = new double[inputs.Length];

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null)
                {
                    throw new ArgumentException($"Row {i} is null.");
                }

                result[i] = Probability(inputs[i]);
            }

            return result;
        }

        public Func<double[], double[]> AsFunction()
        {
            return Predict;
        }
    }
}