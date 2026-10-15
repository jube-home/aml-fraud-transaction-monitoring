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

using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive.Stages.Models;
using Jube.Engine.Exhaustive.Statistics;
using log4net;

namespace Jube.Engine.Exhaustive.Stages
{
    internal class ClassHistogramStage(ILog log)
    {
        internal async Task<bool> ExecuteAsync(InstanceRun run)
        {
            if (run.Outputs == null || run.ClassCount <= 0 || run.Data == null ||
                run.Variables == null)
            {
                return true;
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Writing the negative and positive class histograms on the population " +
                    $"bin edges for Exhaustive Search Instance {run.Instance.Id}.");
            }

            var populationRepository = new ExhaustiveSearchInstanceVariableHistogramRepository(run.DbContext);

            await ClassHistogramWriter.WriteAllAsync(run.Variables, run.Data, run.Outputs,
                    async (variableId, token) =>
                    {
                        var bins = (await populationRepository
                            .GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(variableId, token)
                            .ConfigureAwait(false)).OrderBy(b => b.BinSequence).ToList();

                        return ([.. bins.Select(b => b.BinRangeStart.GetValueOrDefault())],
                            [.. bins.Select(b => b.BinRangeEnd.GetValueOrDefault())]);
                    },
                    new ExhaustiveSearchInstanceVariableHistogramClassRepository(run.DbContext), run.Token)
                .ConfigureAwait(false);

            return true;
        }
    }
}