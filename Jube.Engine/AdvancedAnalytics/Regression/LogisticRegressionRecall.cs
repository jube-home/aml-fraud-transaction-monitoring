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
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public sealed class LogisticRegressionRecall
    {
        public const string ModelKind = "LogisticRegressionPipeline";
        public const string CalibratorMethod = "Platt";
        private readonly LogisticRegressionPipeline pipeline;

        private LogisticRegressionRecall(LogisticRegressionPipeline pipeline, IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }

        public static LogisticRegressionRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionPipelineSnapshot>(
                snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        internal static LogisticRegressionRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsLogisticRegressionPipelineSnapshot> envelope)
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

            var pipeline = new LogisticRegressionPipeline(payload.Standardise, payload.RejectMissing,
                payload.FillValues, payload.Means, payload.StandardDeviations, payload.IndicatorInputs,
                payload.Weights, payload.Intercept, calibrator, payload.TrainingPrior, payload.BaseRate,
                payload.Significances);

            return new LogisticRegressionRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static LogisticRegressionRecall FromPipeline(LogisticRegressionPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new LogisticRegressionRecall(pipeline, variables);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return [pipeline.Probability(input)];
        }

        public LogisticRegressionExplain Explain(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var explain = pipeline.Explain(input);
            var contributions = new LogisticRegressionContribution[explain.Contributions.Length];
            var indicators = pipeline.IndicatorInputs;
            var realCount = Variables.Count;
            var significances = pipeline.Significances;

            for (var i = 0; i < explain.Contributions.Length; i++)
            {
                var name = i < realCount ? Variables[i] : Variables[indicators[i - realCount]] + " (missing)";

                contributions[i] = new LogisticRegressionContribution
                {
                    Name = name,
                    Weight = explain.Contributions[i],
                    Significance = significances != null && i < significances.Length ? significances[i] : null
                };
            }

            return new LogisticRegressionExplain
            {
                RawLogit = explain.RawLogit,
                Intercept = explain.Intercept,
                Contributions = contributions,
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

                result[i] = pipeline.Probability(inputs[i]);
            }

            return result;
        }

        public Func<double[], double[]> AsFunction()
        {
            return Predict;
        }
    }
}