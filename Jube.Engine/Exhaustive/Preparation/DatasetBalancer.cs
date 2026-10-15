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
using log4net;

namespace Jube.Engine.Exhaustive.Preparation
{
    internal class DatasetBalancer(ILog log, Random seeded)
    {
        internal void DatasetSymmetry(double[][] inData, double[] inOutputs, out double[][] outData,
            out double[] outOutputs)
        {
            var countPositive = (int)inOutputs.Sum();
            var countNegative = inOutputs.Length - countPositive;
            DatasetSymmetry(inData, inOutputs, Math.Min(countPositive, countNegative), out outData, out outOutputs);
        }

        internal void DatasetSymmetry(double[][] inData, double[] inOutputs, int targetPerClass,
            out double[][] outData, out double[] outOutputs)
        {
            var countPositive = (int)inOutputs.Sum();
            var countNegative = inOutputs.Length - countPositive;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: There are {countPositive} values in the affirmative and " +
                    $"{countNegative} values in the negative, against a target per class of {targetPerClass}.");
            }

            var remove = new List<int>();

            void RemoveExcess(double classValue, int currentCount)
            {
                var removeCount = currentCount - targetPerClass;

                if (removeCount <= 0)
                {
                    return;
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: The dataset is not symmetric and requires {removeCount} " +
                        $"values of class {classValue} to be removed.");
                }

                while (removeCount > 0)
                {
                    var randomRow = seeded.Next(0, inOutputs.Length);

                    if (!(Math.Abs(inOutputs[randomRow] - classValue) < 0.0001) || remove.Contains(randomRow))
                    {
                        continue;
                    }

                    remove.Add(randomRow);

                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Exhaustive Training: Removed Record {randomRow} from the dataset.");
                    }

                    removeCount -= 1;
                }
            }

            RemoveExcess(1, countPositive);
            RemoveExcess(0, countNegative);

            if (log.IsInfoEnabled)
            {
                log.Info($"Exhaustive Training: Is about to rebuild the array given {remove.Count} records to remove.");
            }

            var symmetricData = new List<double[]>();
            var symmetricOutputs = new List<double>();

            for (var i = 0; i < inOutputs.Length; i++)
            {
                if (remove.Contains(i))
                {
                    continue;
                }

                symmetricData.Add(inData[i]);
                symmetricOutputs.Add(inOutputs[i]);
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Has rebuilt the array and it is now {symmetricOutputs.Count} records long. Setting output variables.");
            }

            outData = [.. symmetricData];
            outOutputs = [.. symmetricOutputs];

            if (log.IsInfoEnabled)
            {
                log.Info("Exhaustive Training: Set output variables. Symmetry concluded.");
            }
        }
    }
}