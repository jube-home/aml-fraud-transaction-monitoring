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
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    public sealed class BaggingClassifierRecall
    {
        public const string ModelKind = "BaggingClassifierPipeline";
        public const string CalibratorMethod = "Platt";
        private readonly BaggingClassifierPipeline pipeline;

        private BaggingClassifierRecall(BaggingClassifierPipeline pipeline, IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }

        public static BaggingClassifierRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsBaggingClassifierPipelineSnapshot>(
                    snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        internal static BaggingClassifierRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsBaggingClassifierPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;

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

                if (calibrator.Value.Mode != CalibrationInput.RawScore)
                {
                    throw new AdvancedAnalyticsSnapshotException("Only raw score calibration is supported.");
                }
            }

            var trees = payload.Trees
                .Select(t => new BaggingTree(t.IsLeaf, t.AttributeIndex, t.Threshold, t.LeftChild, t.RightChild,
                    t.NodeProbability))
                .ToArray();

            var pipeline = new BaggingClassifierPipeline(payload.FillValues, payload.Means, payload.StandardDeviations,
                trees, calibrator);

            return new BaggingClassifierRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static BaggingClassifierRecall FromPipeline(BaggingClassifierPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new BaggingClassifierRecall(pipeline, variables);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return [pipeline.FinalProbability(input)];
        }

        public BaggingClassifierExplain Explain(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var (rootProbability, leafProbability, contributions) = pipeline.Explain(input);
            var items = new BaggingClassifierContribution[contributions.Length];

            for (var i = 0; i < contributions.Length; i++)
            {
                items[i] = new BaggingClassifierContribution { Name = Variables[i], Weight = contributions[i] };
            }

            return new BaggingClassifierExplain
            {
                RawScore = leafProbability,
                BaseValue = rootProbability,
                Contributions = items,
                Calibrated = pipeline.Calibrator is not null,
                CalibrationSlope = pipeline.Calibrator?.Slope,
                CalibrationIntercept = pipeline.Calibrator?.Intercept
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

                result[i] = pipeline.FinalProbability(inputs[i]);
            }

            return result;
        }

        public Func<double[], double[]> AsFunction()
        {
            return Predict;
        }
    }
}