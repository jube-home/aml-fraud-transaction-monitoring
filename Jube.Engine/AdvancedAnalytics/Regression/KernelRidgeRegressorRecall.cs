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
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public sealed class KernelRidgeRegressorRecall
    {
        public const string ModelKind = "KernelRidgeRegressorPipeline";
        private readonly KernelRidgeRegressorPipeline pipeline;

        private KernelRidgeRegressorRecall(KernelRidgeRegressorPipeline pipeline, IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }
        public int SupportRowCount => pipeline.SupportRowCount;
        public KernelRidgeRegressorKernel Kernel => pipeline.Kernel;

        public static KernelRidgeRegressorRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsKernelRidgeRegressorPipelineSnapshot>(
                    snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        internal static KernelRidgeRegressorRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsKernelRidgeRegressorPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;

            if (envelope.Calibrator != null)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "A kernel ridge regressor snapshot must not carry a calibrator; regression targets are never calibrated.");
            }

            KernelRidgeRegressorKernel kernel;

            try
            {
                kernel = Enum.Parse<KernelRidgeRegressorKernel>(payload.Kernel);
            }
            catch (Exception exception) when (exception is ArgumentException or OverflowException)
            {
                throw new AdvancedAnalyticsSnapshotException($"Snapshot kernel '{payload.Kernel}' is not recognised.");
            }

            var pipeline = new KernelRidgeRegressorPipeline(payload.FillValues, payload.Means,
                payload.StandardDeviations, payload.SupportRows, payload.Alpha, payload.Mean, kernel, payload.Gamma,
                payload.Degree);

            return new KernelRidgeRegressorRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static KernelRidgeRegressorRecall FromPipeline(KernelRidgeRegressorPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new KernelRidgeRegressorRecall(pipeline, variables);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return [pipeline.Predict(input)];
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