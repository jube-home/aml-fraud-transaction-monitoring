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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Repository;
using log4net;

namespace Jube.Engine.Observability
{
    public sealed class OpenTelemetryExcludeCache(string connectionString, ILog log)
    {
        private const int PollIntervalMilliseconds = 60000;
        private const int RetryDelayMilliseconds = 5000;

        private volatile HashSet<string> excludedNames = [];

        public bool IsExcluded(string name)
        {
            return !string.IsNullOrEmpty(name) && excludedNames.Contains(name);
        }

        public async Task StartAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await RefreshAsync(token).ConfigureAwait(false);
                    await Task.Delay(PollIntervalMilliseconds, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    log.Error($"OpenTelemetryExcludeCache: Could not refresh exclude list: {ex}. Waiting for retry.");

                    try
                    {
                        await Task.Delay(RetryDelayMilliseconds, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task RefreshAsync(CancellationToken token)
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString, log);
            var repository = new OpenTelemetryExcludeRepository(dbContext);

            var rows = await repository.GetAllActiveAsync(token).ConfigureAwait(false);

            var refreshed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (!string.IsNullOrEmpty(row.Name))
                {
                    refreshed.Add(row.Name);
                }
            }

            excludedNames = refreshed;
        }
    }
}