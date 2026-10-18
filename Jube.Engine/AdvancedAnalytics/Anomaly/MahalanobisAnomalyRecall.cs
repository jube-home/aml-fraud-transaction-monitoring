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
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;

namespace Jube.Engine.AdvancedAnalytics.Anomaly
{
    public sealed class MahalanobisAnomalyRecall
    {
        public const string ModelKind = "MahalanobisAnomalyPipeline";
        public const string CalibratorMethod = "Platt";
        private readonly MahalanobisAnomalyPipeline pipeline;

        private MahalanobisAnomalyRecall(MahalanobisAnomalyPipeline pipeline, IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        internal static MahalanobisAnomalyRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsMahalanobisAnomalyPipelineSnapshot> envelope)
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

            var pipeline = new MahalanobisAnomalyPipeline(payload.Standardise, payload.FillValues, payload.Means,
                payload.StandardDeviations, payload.ActiveInputs, payload.Centre, payload.InverseCovariance,
                calibrator);

            return new MahalanobisAnomalyRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static MahalanobisAnomalyRecall FromPipeline(MahalanobisAnomalyPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new MahalanobisAnomalyRecall(pipeline, variables);
        }

        public IReadOnlyList<string> Variables { get; }

        public static MahalanobisAnomalyRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsMahalanobisAnomalyPipelineSnapshot>(
                    snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var distance = pipeline.Distance(input);

            return pipeline.Calibrator is { } calibrator ? [calibrator.Apply(distance)] : [distance];
        }

        public MahalanobisAnomalyExplain Explain(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var explain = pipeline.Explain(input);
            var contributions = new MahalanobisAnomalyContribution[explain.Terms.Length];

            for (var i = 0; i < explain.Terms.Length; i++)
            {
                contributions[i] = new MahalanobisAnomalyContribution
                    { Name = Variables[i], Weight = explain.Terms[i] };
            }

            return new MahalanobisAnomalyExplain
            {
                Distance = explain.Distance,
                Contributions = contributions,
                Calibrated = pipeline.Calibrator is not null,
                CalibrationSlope = pipeline.Calibrator?.Slope,
                CalibrationIntercept = pipeline.Calibrator?.Intercept
            };
        }

        public double[] PredictDistances(double[][] inputs)
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
    }
}