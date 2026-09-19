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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;
using Npgsql;
using Fixture = Jube.Test.Infrastructure.DatabaseFixture.DatabaseFixture;

namespace Jube.Test.Infrastructure.ModelScaffolding
{
    public static class ModelSync
    {
        public static async Task<ModelSyncResult> SyncAsync(ModelEngineHost engine, ModelScaffold scaffold,
            TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var limit = timeout ?? TimeSpan.FromSeconds(60);
            var started = DateTime.UtcNow;
            var before = await SynchronisedDateAsync(scaffold.TenantRegistryId).ConfigureAwait(false);
            var errorsBefore = await ErrorRowsAsync().ConfigureAwait(false);

            var now = DateTime.UtcNow;
            var scheduleDate = before is { } last && last < now ? last + (now - last) / 2 : now.AddSeconds(-5);

            int scheduleId;
            await using (var dbContext =
                         DataConnectionDbContext.GetResilientDbContextDataConnection(ModelScaffold.ConnectionString,
                             TestLog.NoOp))
            {
                scheduleId = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelSynchronisationSchedule
                {
                    CreatedDate = DateTime.UtcNow,
                    ScheduleDate = scheduleDate,
                    TenantRegistryId = scaffold.TenantRegistryId,
                    CreatedUser = Fixture.Prefix
                }, token: cancellationToken).ConfigureAwait(false);
            }

            scaffold.TrackSchedule(scheduleId);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var current = await SynchronisedDateAsync(scaffold.TenantRegistryId).ConfigureAwait(false);
                var advanced = current != null && (before == null || current > before);
                var model = engine.FindActiveModel(scaffold.ModelGuid);
                var hasCallers = !scaffold.Options.CreateCallers || (model != null &&
                                                                     model.Collections.Users
                                                                         .Contains(scaffold.UserName));

                if (advanced && model != null && hasCallers)
                {
                    return new ModelSyncResult(DateTime.UtcNow - started, current,
                        await ErrorRowsAsync().ConfigureAwait(false) - errorsBefore, scheduleId);
                }

                if (DateTime.UtcNow - started > limit)
                {
                    throw new ModelSyncTimeoutException(
                        $"Model synchronisation did not report within {limit.TotalSeconds:0}s. Waiting for the node status " +
                        $"entry of tenant {scaffold.TenantRegistryId} on instance '{Dns.GetHostName()}' to move past " +
                        $"{(before?.ToString("O") ?? "(none)")}; it is now {(current?.ToString("O") ?? "(none)")}. " +
                        $"Schedule row {scheduleId} was inserted; {await PendingSchedulesAsync(scaffold.TenantRegistryId).ConfigureAwait(false)} " +
                        $"schedule row(s) exist for the tenant. The engine {(model != null ? "holds" : "does not hold")} model " +
                        $"{scaffold.ModelGuid}" +
                        (model != null && scaffold.Options.CreateCallers
                            ? $" and {(hasCallers ? "lists" : "does not list")} caller '{scaffold.UserName}'"
                            : string.Empty) +
                        $". {await ErrorRowsAsync().ConfigureAwait(false) - errorsBefore} synchronisation error row(s) were " +
                        $"added meanwhile. Recent engine errors: {engine.RecentErrors(5)}");
                }

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task<DateTime?> SynchronisedDateAsync(int tenantRegistryId)
        {
            await using var connection = new NpgsqlConnection(ModelScaffold.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new NpgsqlCommand(
                "SELECT \"SynchronisedDate\" FROM \"EntityAnalysisModelSynchronisationNodeStatusEntry\" " +
                "WHERE \"TenantRegistryId\" = @tenant AND \"Instance\" = @instance", connection);
            command.Parameters.AddWithValue("tenant", tenantRegistryId);
            command.Parameters.AddWithValue("instance", Dns.GetHostName());
            var value = await command.ExecuteScalarAsync().ConfigureAwait(false);
            return value is DateTime date ? date : null;
        }

        public static async Task<long> ErrorRowsAsync()
        {
            await using var connection = new NpgsqlConnection(ModelScaffold.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new NpgsqlCommand(
                "SELECT count(*) FROM \"EntityAnalysisModelSynchronisationError\"", connection);
            return (long)(await command.ExecuteScalarAsync().ConfigureAwait(false)).Required();
        }

        private static async Task<long> PendingSchedulesAsync(int tenantRegistryId)
        {
            await using var connection = new NpgsqlConnection(ModelScaffold.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new NpgsqlCommand(
                "SELECT count(*) FROM \"EntityAnalysisModelSynchronisationSchedule\" WHERE \"TenantRegistryId\" = @tenant",
                connection);
            command.Parameters.AddWithValue("tenant", tenantRegistryId);
            return (long)(await command.ExecuteScalarAsync().ConfigureAwait(false)).Required();
        }
    }
}