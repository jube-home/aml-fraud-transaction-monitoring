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

using Accord.MachineLearning.VectorMachines;
using Accord.Statistics.Kernels;
using log4net;

namespace Jube.Engine.Exhaustive.Anomaly
{
    internal class AnomalyClassifier(ILog log)
    {
        internal double[] GetClassVariableByAnomaly(SupportVectorMachine<Gaussian> model,
            double[][] data,
            double probability)
        {
            var outputs = new double[data.Length];

            for (var i = 0; i < data.Length; i++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Processing row {i}.");
                }

                var p = model.Probability(data[i]);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Processed row {i} and returned probability {p}.");
                }

                outputs[i] = p < probability ? 1 : 0;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Processed row {i} and created class {outputs[i]}.");
                }
            }

            return outputs;
        }
    }
}