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

namespace Jube.Engine.AdvancedAnalytics.DimensionReduction
{
    public sealed class PrincipalComponentRecall
    {
        public const string ModelKind = "PrincipalComponentPipeline";
        private readonly PrincipalComponentPipeline pipeline;

        private PrincipalComponentRecall(PrincipalComponentPipeline pipeline, IReadOnlyList<string> variables)
        {
            this.pipeline = pipeline;
            Variables = variables;
        }

        public IReadOnlyList<string> Variables { get; }
        public int ComponentCount => pipeline.ComponentCount;

        public static PrincipalComponentRecall FromSnapshot(string snapshotJson)
        {
            ArgumentNullException.ThrowIfNull(snapshotJson);

            var envelope = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsPrincipalComponentPipelineSnapshot>(
                snapshotJson, ModelKind);

            return FromEnvelope(envelope);
        }

        public double[] Predict(double[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return pipeline.Project(input);
        }

        public double[][] PredictBatch(double[][] inputs)
        {
            ArgumentNullException.ThrowIfNull(inputs);

            var result = new double[inputs.Length][];

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null)
                {
                    throw new ArgumentException($"Row {i} is null.");
                }

                result[i] = pipeline.Project(inputs[i]);
            }

            return result;
        }

        public Func<double[], double[]> AsFunction()
        {
            return Predict;
        }

        internal static PrincipalComponentRecall FromEnvelope(
            AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsPrincipalComponentPipelineSnapshot> envelope)
        {
            var payload = envelope.Payload;
            var components = payload.Components.Select(c => (double[])c.Eigenvector.Clone()).ToArray();

            var pipeline = new PrincipalComponentPipeline(payload.Standardise, payload.FillValues, payload.Means,
                payload.StandardDeviations, components);

            return new PrincipalComponentRecall(pipeline, Array.AsReadOnly([.. envelope.VariablesInOrder]));
        }

        internal static PrincipalComponentRecall FromPipeline(PrincipalComponentPipeline pipeline,
            IReadOnlyList<string> variables)
        {
            return new PrincipalComponentRecall(pipeline, variables);
        }
    }
}