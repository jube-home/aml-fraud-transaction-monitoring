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
    public sealed class KNearestNeighboursRegressorRecall
    {
        public const string ModelKind = "KNearestNeighboursRegressorPipeline";
        private readonly KNearestNeighboursRegressorPipeline pipeline;

        private KNearestNeighboursRegressorRecall(KNearestNeighboursRegressorPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }
        public int NeighbourCount => pipeline.NeighbourCount;
        public int StoredRowCount => pipeline.StoredRowCount;

        public static KNearestNeighboursRegressorRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsKNearestNeighboursRegressorPipelineSnapshot>(
                    snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        internal static KNearestNeighboursRegressorRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsKNearestNeighboursRegressorPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;

            var pipeline = new KNearestNeighboursRegressorPipeline(payload.Standardise, payload.FillValues,
                payload.Means, payload.StandardDeviations, payload.Rows, payload.Targets, payload.NeighbourCount,
                payload.TrainingMeanTarget);

            return new KNearestNeighboursRegressorRecall(pipeline,
                Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static KNearestNeighboursRegressorRecall FromPipeline(KNearestNeighboursRegressorPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new KNearestNeighboursRegressorRecall(pipeline, variables);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return [pipeline.Predict(input)];
        }

        public KNearestNeighboursRegressorExplain Explain(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var explain = pipeline.Explain(input);
            var contributions = new KNearestNeighboursRegressorContribution[explain.VariableShares.Length];

            for (var i = 0; i < explain.VariableShares.Length; i++)
            {
                contributions[i] = new KNearestNeighboursRegressorContribution
                {
                    Name = Variables[i],
                    Weight = explain.VariableShares[i]
                };
            }

            return new KNearestNeighboursRegressorExplain
            {
                PredictedValue = explain.PredictedValue,
                BaseValue = pipeline.TrainingMeanTarget,
                Contributions = contributions,
                NeighbourTargets = explain.NeighbourTargets,
                NeighbourDistances = explain.NeighbourDistances
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