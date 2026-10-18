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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload;
using Jube.Engine.Helpers;
using log4net;

namespace Jube.Engine.Exhaustive.Storage
{
    internal class MockDataLoader(ILog log, JsonSerializationHelper jsonSerializationHelper)
    {
        internal async Task LoadMockDataAsync(DbContext dbContext, int entityAnalysisModelId,
            CancellationToken token = default)
        {
            var repository = new MockArchiveRepository(dbContext);

            if (log.IsInfoEnabled)
            {
                log.Info("Exhaustive Training: Will delete the MockArchive table for mock data.");
            }

            var count = await repository.GetLoadedCountAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            if (count > 0)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info($"Exhaustive Training: Data Already Loaded for ModelID: {entityAnalysisModelId}.");
                }

                return;
            }

            var variables = new List<string>();

            var iRow = 1;

            var lines = Mock.Get().Split("\n");

            foreach (var line in lines)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info($"Exhaustive Training:Found line data {line}.");
                }

                var splitsString = line.Split(",");

                if (iRow > 1)
                {
                    try
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Exhaustive Training:Found line at line {iRow} {splitsString.Length} splits.");
                        }

                        var splitsDouble = new double[splitsString.Length];

                        var row = new EntityAnalysisModelInstanceEntryPayload
                        {
                            Tag = [],
                            Abstraction = new PooledDictionary<string, double>(splitsString.Length)
                        };

                        for (var i = 0; i < splitsString.Length; i++)
                        {
                            splitsDouble[i] = double.Parse(splitsString[i]);

                            if (i == 0)
                            {
                                row.Tag = splitsDouble[i] > 0 ? ["Fraud"] : [];
                            }
                            else
                            {
                                row.Abstraction.Add(variables[i], splitsDouble[i]);
                            }
                        }

                        var model = new MockArchive
                        {
                            EntityAnalysisModelId = entityAnalysisModelId,
                            EntryKeyValue = iRow.ToString(),
                            ResponseElevation = 0,
                            EntityAnalysisModelActivationRuleId = 0,
                            ActivationRuleCount = 0,
                            CreatedDate = DateTime.UtcNow,
                            ReferenceDate = DateTime.UtcNow,
                            EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                            Json = Encoding.UTF8.GetString(BuildJsonResponses.BuildFullJson(row,
                                jsonSerializationHelper.ArchiveJsonSerializer))
                        };

                        await repository.InsertAsync(model, token).ConfigureAwait(false);

                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Exhaustive Training:Found line {line} has been added to the array.");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.Error($"Exhaustive Training: Line {iRow} has caused an exception {ex}.");
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Exhaustive Training:Found header data {line}.");
                    }

                    foreach (var split in line.Split(","))
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info("Exhaustive Training:Adding variable.");
                        }

                        variables.Add(split);
                    }
                }

                iRow += 1;

                if (log.IsInfoEnabled)
                {
                    log.Info($"Exhaustive Training:Concluded line {line}.");
                }
            }

            switch (log.IsInfoEnabled)
            {
                case false:
                    return;
                case true:
                    log.Info(
                        $"Exhaustive Training:Concluded and created array of length {iRow} will cast and return array.");

                    break;
            }
        }
    }
}