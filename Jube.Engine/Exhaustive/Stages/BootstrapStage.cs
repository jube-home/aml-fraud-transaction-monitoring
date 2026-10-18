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
using System.Threading.Tasks;
using Jube.Analytics.Bootstrap;
using Jube.Engine.Exhaustive.Stages.Models;
using log4net;

namespace Jube.Engine.Exhaustive.Stages
{
    internal class BootstrapStage(ILog log, Random seeded)
    {
        internal const string Method = "DVineCopula";

        internal async Task<bool> ExecuteAsync(InstanceRun run)
        {
            var exhaustiveSearchInstance = run.Instance;

            if (!exhaustiveSearchInstance.Bootstrap || run.ClassCount <= 0)
            {
                return true;
            }

            var data = run.Data;
            var outputs = run.Outputs;
            var variableCount = data.Length > 0 ? data[0].Length : 0;

            if (variableCount == 0)
            {
                return true;
            }

            var positiveIndices = Enumerable.Range(0, outputs.Length).Where(i => outputs[i] > 0.5).ToArray();
            var negativeIndices = Enumerable.Range(0, outputs.Length).Where(i => outputs[i] <= 0.5).ToArray();

            var minorityLabel = positiveIndices.Length <= negativeIndices.Length ? 1d : 0d;
            var minorityIndices = minorityLabel > 0.5 ? positiveIndices : negativeIndices;

            var targetPerClass = run.BootstrapTargetPerClass;

            if (minorityIndices.Length >= targetPerClass || minorityIndices.Length < 2)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Exhaustive Search Instance ID {exhaustiveSearchInstance.Id} has " +
                        $"{minorityIndices.Length} minority class rows against a target of {targetPerClass}; " +
                        "bootstrapping is not required.");
                }

                return true;
            }

            var deficit = targetPerClass - minorityIndices.Length;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Exhaustive Search Instance ID {exhaustiveSearchInstance.Id} has only " +
                    $"{minorityIndices.Length} minority class rows against a target of {targetPerClass}; " +
                    $"fitting a vine copula on the minority class to synthesise {deficit} additional training rows.");
            }

            var minorityColumns = new double[variableCount][];

            for (var v = 0; v < variableCount; v++)
            {
                var column = minorityIndices.Select(i => data[i][v]).ToArray();

                var finite = column.Where(double.IsFinite).ToArray();

                if (finite.Length != column.Length)
                {
                    var fillValue = finite.Length > 0 ? finite.Average() : 0d;

                    for (var r = 0; r < column.Length; r++)
                    {
                        if (!double.IsFinite(column[r]))
                        {
                            column[r] = fillValue;
                        }
                    }
                }

                minorityColumns[v] = column;
            }

            double[][] synthetic;

            try
            {
                var vine = DVineCopula.Fit(minorityColumns);
                synthetic = vine.Sample(deficit, new Random(seeded.Next()));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error(
                    $"Exhaustive Training: Exhaustive Search Instance ID {exhaustiveSearchInstance.Id} could not " +
                    $"fit a bootstrap vine copula on the minority class as {ex}; proceeding without bootstrapping.");

                return true;
            }

            if (synthetic.Any(row => row.Any(value => double.IsNaN(value) || double.IsInfinity(value))))
            {
                log.Error(
                    $"Exhaustive Training: Exhaustive Search Instance ID {exhaustiveSearchInstance.Id} bootstrap " +
                    "vine copula sampled non-finite values (likely a zero-variance minority column); " +
                    "proceeding without bootstrapping.");

                return true;
            }

            run.SyntheticData = synthetic;
            run.SyntheticOutputs = [.. Enumerable.Repeat(minorityLabel, deficit)];
            run.SyntheticWeight = Math.Clamp(exhaustiveSearchInstance.BootstrapWeightFactor ?? 1, 0, 1);
            run.BootstrapRowsAdded = deficit;
            run.BootstrapMethod = Method;

            await run.RepositoryInstance.UpdateBootstrapAsync(exhaustiveSearchInstance.Id, deficit, Method,
                run.Token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Exhaustive Search Instance ID {exhaustiveSearchInstance.Id} has " +
                    $"synthesised {deficit} minority class training rows via {Method}.");
            }

            return true;
        }
    }
}