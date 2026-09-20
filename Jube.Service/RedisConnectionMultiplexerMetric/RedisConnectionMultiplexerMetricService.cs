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

using System.ComponentModel;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Payload;
using Jube.Dto.RedisConnectionMultiplexerMetric;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.RedisConnectionMultiplexerMetric;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.RedisConnectionMultiplexerMetric
{
    public sealed class RedisConnectionMultiplexerMetricService
    {
        private const int MaxListTake = 100000;

        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RedisConnectionMultiplexerMetricRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private RedisConnectionMultiplexerMetricService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new RedisConnectionMultiplexerMetricRepository(dbContext);
        }

        public static Task<RedisConnectionMultiplexerMetricService> CreateAsync(DbContext dbContext, string? userName,
            ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<RedisConnectionMultiplexerMetricService> CreateAsync(DbContext dbContext,
            string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(RedisConnectionMultiplexerMetricResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("RedisConnectionMultiplexerMetric.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[RedisConnectionMultiplexerMetricResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"RedisConnectionMultiplexerMetric.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[RedisConnectionMultiplexerMetricResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new RedisConnectionMultiplexerMetricService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists per-minute snapshots of the application's own live Redis ConnectionMultiplexer -- " +
                     "connection health and queue depth (pending/in-flight operations) for both the " +
                     "interactive command connection and the pub/sub subscription connection -- across every " +
                     "node, most recent first when no sort is given, capped at 'take' rows (max 100000, " +
                     "default 100000). Each of from/to defaults independently to the last hour when omitted. " +
                     "Not scoped to any Model or tenant. Optionally restrict to a date range (by CreatedDate) " +
                     "and/or a case-insensitive substring search against Instance or ClientName. Optionally " +
                     "apply samplePercentage on top of every other filter to draw a random subset instead of " +
                     "the most recent rows -- useful for taking an unbiased baseline sample of activity to " +
                     "compare later, rather than only ever seeing the latest, potentially unrepresentative " +
                     "rows. The response also carries the true Total row count (ignoring 'take') and summary " +
                     "Statistics for this DTO's continuous measured columns (every numeric column except Id " +
                     "-- TimeoutMilliseconds, EndPointCount, ConnectedEndPointCount, TotalOperationCount, " +
                     "TotalOutstanding, the Interactive/Subscription queue-depth and completion counters, and " +
                     "the per-minute event counters) over the full filtered set.")]
        [ServiceOperation("RedisConnectionMultiplexerMetricList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<RedisConnectionMultiplexerMetricDto>> ListAsync(
            [Description(
                "Maximum number of rows to return, most recent first when no sort is given; clamped to 100000.")]
            int take = 100000,
            [Description(
                "Only include rows with CreatedDate on or after this UTC timestamp; defaults to 1 hour before the current time when omitted.")]
            DateTime? from = null,
            [Description(
                "Only include rows with CreatedDate on or before this UTC timestamp; defaults to the current time when omitted.")]
            DateTime? to = null,
            [Description(
                "Case-insensitive substring match against Instance (hostname); null or empty matches every row.")]
            string? search = null,
            [Description(
                "Percentage (0-100) chance of including each row that already matches every other filter, evaluated independently per row -- e.g. 10 returns roughly a random one-tenth of matching rows, not the newest tenth. Omit for the normal most-recent-first behaviour. Not surfaced in any admin UI; intended for an agent building an unbiased baseline sample of activity to compare against later, rather than for browsing.")]
            double? samplePercentage = null,
            [Description(
                "Column to sort by (e.g. 'createdDate', 'totalOutstanding', 'connectionFailedCount'); unrecognised or omitted falls back to most-recent-first.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RedisConnectionMultiplexerMetric", "List", userName, tenantRegistryId,
                auditLog,
                log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            var clampedSamplePercentage = samplePercentage.HasValue
                ? Math.Clamp(samplePercentage.Value, 0.0, 100.0)
                : (double?)null;
            from ??= DateTime.UtcNow.AddHours(-1);
            to ??= DateTime.UtcNow;
            if (log.IsDebugEnabled)
            {
                log.Debug($"RedisConnectionMultiplexerMetric.List: entry take={clampedTake} user={userName}");
            }

            try
            {
                EnsurePermitted("RedisConnectionMultiplexerMetric.List");

                var rows = await repository.GetLastAsync(clampedTake, from, to, search, clampedSamplePercentage,
                    sortField, sortDirection, token).ConfigureAwait(false);

                var dtos = rows.Select(r => new RedisConnectionMultiplexerMetricDto(
                    r.Id, r.CreatedDate.GetValueOrDefault(), r.Instance, r.ClientName,
                    r.TimeoutMilliseconds.GetValueOrDefault(), r.IsConnected.GetValueOrDefault(),
                    r.IsConnecting.GetValueOrDefault(), r.EndPointCount.GetValueOrDefault(),
                    r.ConnectedEndPointCount.GetValueOrDefault(), r.TotalOperationCount.GetValueOrDefault(),
                    r.TotalOutstanding.GetValueOrDefault(), r.InteractivePendingUnsentItems.GetValueOrDefault(),
                    r.InteractiveSentItemsAwaitingResponse.GetValueOrDefault(),
                    r.InteractiveResponsesAwaitingAsyncCompletion.GetValueOrDefault(),
                    r.InteractiveTotalOutstanding.GetValueOrDefault(),
                    r.InteractiveCompletedAsynchronously.GetValueOrDefault(),
                    r.InteractiveCompletedSynchronously.GetValueOrDefault(),
                    r.InteractiveFailedAsynchronously.GetValueOrDefault(),
                    r.InteractiveNonPreferredEndpointCount.GetValueOrDefault(),
                    r.InteractiveSocketCount.GetValueOrDefault(), r.InteractiveWriterCount.GetValueOrDefault(),
                    r.SubscriptionPendingUnsentItems.GetValueOrDefault(),
                    r.SubscriptionSentItemsAwaitingResponse.GetValueOrDefault(),
                    r.SubscriptionResponsesAwaitingAsyncCompletion.GetValueOrDefault(),
                    r.SubscriptionTotalOutstanding.GetValueOrDefault(),
                    r.SubscriptionCompletedAsynchronously.GetValueOrDefault(),
                    r.SubscriptionCompletedSynchronously.GetValueOrDefault(),
                    r.SubscriptionFailedAsynchronously.GetValueOrDefault(),
                    r.SubscriptionSocketCount.GetValueOrDefault(), r.SubscriptionCount.GetValueOrDefault(),
                    r.ConnectionFailedCount.GetValueOrDefault(), r.ConnectionRestoredCount.GetValueOrDefault(),
                    r.ErrorMessageCount.GetValueOrDefault(), r.InternalErrorCount.GetValueOrDefault(),
                    r.ConfigurationChangedCount.GetValueOrDefault(),
                    r.ConfigurationChangedBroadcastCount.GetValueOrDefault())).ToList();

                var total = await repository.CountAsync(from, to, search, clampedSamplePercentage, token)
                    .ConfigureAwait(false);

                var statistics = await repository
                    .GetStatisticsAsync(from, to, search, clampedSamplePercentage, MaxListTake, token)
                    .ConfigureAwait(false);

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"RedisConnectionMultiplexerMetric.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<RedisConnectionMultiplexerMetricDto>(dtos, total, statistics);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug($"RedisConnectionMultiplexerMetric.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"RedisConnectionMultiplexerMetric.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Landlord)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied (landlord only) user={userName}");
            }

            throw new ForbiddenException(strings[RedisConnectionMultiplexerMetricResources.PermissionDenied],
                permissions);
        }
    }
}