/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.Observability.Interfaces;
using Jube.Data.Context;
using Jube.Dto.Repository.PreservationSnapshot;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.PreservationSnapshot;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.AskJoobyConversation
{
    internal sealed class AskJoobyAutoSnapshot(
        string connectionString,
        string userName,
        ILog log,
        IStringLocalizerFactory stringLocalizerFactory,
        IServiceChangeBus serviceChangeBus)
        : IAutoSnapshot
    {
        private readonly SemaphoreSlim gate = new(1, 1);
        private readonly List<AutoSnapshotRecord> taken = [];

        public IReadOnlyList<AutoSnapshotRecord> Taken
        {
            get
            {
                lock (taken)
                {
                    return taken.ToArray();
                }
            }
        }

        public async Task<AutoSnapshotRecord?> TakeBeforeWriteAsync(string toolName, CancellationToken token)
        {
            await gate.WaitAsync(token).ConfigureAwait(false);

            try
            {
                await using var dbContext = DataConnectionDbContext
                    .GetResilientDbContextDataConnection(connectionString, log);

                var service = await PreservationSnapshotService
                    .CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, token)
                    .ConfigureAwait(false);

                var snapshot = await service.CreateForProcessAsync(new PreservationSnapshotRequestDto
                {
                    Name = $"Before {toolName}",
                    SnapshotSourceId = PreservationSnapshotSource.AskJooby,
                    Lists = true,
                    Dictionaries = true,
                    Visualisations = true,
                    Roles = true,
                    AdvancedAnalytics = true
                }, token).ConfigureAwait(false);

                var record = new AutoSnapshotRecord(snapshot.Id, snapshot.Guid, toolName, DateTimeOffset.UtcNow);

                lock (taken)
                {
                    taken.Add(record);
                }

                return record;
            }
            finally
            {
                gate.Release();
            }
        }
    }
}