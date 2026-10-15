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
    public sealed class MultipleLinearRegressionRecall
    {
        public const string ModelKind = "MultipleLinearRegressionPipeline";
        private readonly LinearRegressionPipeline pipeline;

        private MultipleLinearRegressionRecall(LinearRegressionPipeline pipeline, IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }

        public static MultipleLinearRegressionRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLinearRegressionPipelineSnapshot>(
                snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        internal static MultipleLinearRegressionRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsLinearRegressionPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;

            var pipeline = new LinearRegressionPipeline(payload.Standardise, payload.RejectMissing,
                payload.FillValues, payload.Means, payload.StandardDeviations, payload.IndicatorInputs,
                payload.Weights, payload.Intercept);

            return new MultipleLinearRegressionRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static MultipleLinearRegressionRecall FromPipeline(LinearRegressionPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new MultipleLinearRegressionRecall(pipeline, variables);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return [pipeline.Predict(input)];
        }

        public MultipleLinearRegressionExplain Explain(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var explain = pipeline.Explain(input);
            var contributions = new LinearRegressionContribution[explain.Contributions.Length];
            var indicators = pipeline.IndicatorInputs;
            var realCount = Variables.Count;

            for (var i = 0; i < explain.Contributions.Length; i++)
            {
                var name = i < realCount ? Variables[i] : Variables[indicators[i - realCount]] + " (missing)";

                contributions[i] = new LinearRegressionContribution
                {
                    Name = name,
                    Weight = explain.Contributions[i]
                };
            }

            return new MultipleLinearRegressionExplain
            {
                PredictedValue = explain.PredictedValue,
                Intercept = explain.Intercept,
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