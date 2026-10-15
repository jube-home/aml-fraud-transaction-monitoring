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
using System.Linq;

namespace Jube.Engine.Exhaustive.Preparation
{
    internal static class DatasetShaper
    {
        internal static double[][] OnlyOutputWithClassificationForData(int classCount, double[] outputs,
            double[][] data)
        {
            var filteredData = new double[classCount][];
            var j = 0;

            for (var i = 0; i < outputs.Length; i++)
            {
                if (!(Math.Abs(outputs[i] - 1) < 0.0001))
                {
                    continue;
                }

                filteredData[j] = data[i];
                j += 1;
            }

            return filteredData;
        }

        internal static void Append(double[][] existingData,
            double[][] existingDataClassification,
            out double[][] data,
            out double[] outputs)
        {
            var appendedData = new double[existingData.Length + existingDataClassification.Length][];
            existingData.CopyTo(appendedData, 0);
            existingDataClassification.CopyTo(appendedData, existingData.Length);

            var existingOutputs = Enumerable.Repeat(0, existingData.Length).ToArray();
            var existingOutputsClassification = Enumerable.Repeat(1, existingDataClassification.Length).ToArray();

            var appendedOutputs = new double[existingData.Length + existingOutputsClassification.Length];
            existingOutputs.CopyTo(appendedOutputs, 0);
            existingOutputsClassification.CopyTo(appendedOutputs, existingOutputs.Length);

            data = appendedData;
            outputs = appendedOutputs;
        }

        internal static void Shuffle(Random r, double[][] existingData,
            double[] existingOutputs,
            out double[][] newData,
            out double[] outputs)
        {
            var shuffleArray = new int[existingOutputs.Length];

            for (var i = 0; i < shuffleArray.Length; i++)
            {
                shuffleArray[i] = i;
            }

            shuffleArray = [.. shuffleArray.OrderBy(_ => r.Next())];

            var newDataAfterShuffle = new double[shuffleArray.Length][];
            var outputsAfterShuffle = new double[shuffleArray.Length];

            for (var i = 0; i < shuffleArray.Length; i++)
            {
                newDataAfterShuffle[i] = existingData[shuffleArray[i]];
                outputsAfterShuffle[i] = existingOutputs[shuffleArray[i]];
            }

            newData = newDataAfterShuffle;
            outputs = outputsAfterShuffle;
        }
    }
}