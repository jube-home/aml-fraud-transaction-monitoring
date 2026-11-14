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

using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.Preservation;
using log4net;

namespace Jube.Service.Repository.PreservationSnapshot
{
    internal static class PreservationSnapshotRetention
    {
        internal static async Task RunAsync(DbContext dbContext, string userName, int tenantRegistryId,
            PreservationLimits limits, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            if (limits.MaximumSnapshotsPerTenant <= 0 && limits.MaximumSnapshotAgeDays <= 0)
            {
                return;
            }

            try
            {
                var pruned = await new PreservationSnapshotRepository(dbContext, userName)
                    .PruneAsync(limits.MaximumSnapshotsPerTenant, limits.MaximumSnapshotAgeDays, DateTime.UtcNow, token)
                    .ConfigureAwait(false);

                if (pruned == 0)
                {
                    return;
                }

                using var op = OperationScope.Start("PreservationSnapshot", "Prune", userName, tenantRegistryId,
                    auditLog, log, serviceChangeBus);

                op.Rows(pruned);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"PreservationSnapshot.Prune: removed {pruned} snapshot bodies by retention user={userName}");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PreservationSnapshot.Prune: retention failed user={userName}", ex);
                }
            }
        }
    }
}
