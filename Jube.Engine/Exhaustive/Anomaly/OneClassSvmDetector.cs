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
using System.Threading;
using Accord.MachineLearning.VectorMachines;
using Accord.Statistics.Kernels;
using Jube.Analytics.Anomaly;
using Jube.Analytics.Anomaly.Interfaces;
using Jube.Engine.Exhaustive.Algorithms;
using log4net;

namespace Jube.Engine.Exhaustive.Anomaly
{
    internal sealed class OneClassSvmDetector(ILog log) : IAnomalyDetector
    {
        private readonly Lock gate = new();
        private SupportVectorMachine<Gaussian> model;
        public string Name => "OneClassSvm";
        public int Dimensions { get; private set; }
        public bool IsFitted => model != null;

        public void Fit(double[][] data, int seed, CancellationToken token = default)
        {
            AnomalyInput.Validate(data);
            token.ThrowIfCancellationRequested();
            var fitted = Unsupervised.Learn(data, log);
            var dimensions = data[0].Length;
            token.ThrowIfCancellationRequested();

            lock (gate)
            {
                model = fitted;
                Dimensions = dimensions;
            }
        }

        public double Score(double[] row)
        {
            return -Probability(row);
        }

        public double Probability(double[] row)
        {
            lock (gate)
            {
                if (model == null)
                {
                    throw new InvalidOperationException("The One Class Support Vector Machine has not been fitted.");
                }

                return model.Probability(row);
            }
        }
    }
}