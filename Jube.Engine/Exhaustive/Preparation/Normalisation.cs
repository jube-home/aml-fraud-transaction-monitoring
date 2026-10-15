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

using System.Collections.Generic;
using Jube.Analytics;
using Jube.Engine.Exhaustive.Algorithms.Models;
using log4net;

namespace Jube.Engine.Exhaustive.Preparation
{
    internal class Normalisation(ILog log)
    {
        internal double[][] NormaliseData(Dictionary<int, Variable> variables, double[][] data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Starting to normalise row {i}.");
                }

                for (var j = 0; j < variables.Count; j++)
                {
                    if (variables[j].NormalisationType == 2)
                    {
                        data[i][j] = Standardisation.ZScore(data[i][j], variables[j].Mean, variables[j].Sd);
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Finished normalising row {i}.");
                }
            }

            return data;
        }

        internal static double ZScore(double value, double mean, double sd, int normalisationTypeId)
        {
            return normalisationTypeId == 2 ? Standardisation.ZScore(value, mean, sd) : value;
        }
    }
}