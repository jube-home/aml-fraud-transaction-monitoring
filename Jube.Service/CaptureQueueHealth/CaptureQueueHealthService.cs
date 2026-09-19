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
using Jube.Dto.CaptureQueueHealth;
using Jube.Dto.Payload;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.CaptureQueueHealth;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.CaptureQueueHealth
{
    public sealed class CaptureQueueHealthService
    {
        private const int MaxListTake = 100000;

        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly CaptureQueueHealthRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaptureQueueHealthService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new CaptureQueueHealthRepository(dbContext);
        }

        public static Task<CaptureQueueHealthService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaptureQueueHealthService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaptureQueueHealthResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaptureQueueHealth.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaptureQueueHealthResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaptureQueueHealth.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaptureQueueHealthResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaptureQueueHealthService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists per-cycle health snapshots (queue depth and dropped-since-last-flush count) for " +
                     "every bounded in-memory capture queue in the observability suite -- ModelInvokeWarning, " +
                     "CaseCreationWarning, ArchiverWarning, RedisSentinelEvent, RedisConnectionEvent, " +
                     "OpenTelemetryMetric. One row per queue is written every ~60 seconds regardless of " +
                     "activity, so a flatline at zero is itself meaningful (everything's fine) -- watch for " +
                     "any non-zero DroppedCount, which means real data was silently discarded because a " +
                     "queue hit its capacity. Most recent first when no sort is given, capped at 'take' rows " +
                     "(max 100000, default 100000). Each of from/to defaults independently to the last hour " +
                     "when omitted. Not scoped to any tenant -- this is process-wide infrastructure health, " +
                     "not tenant data. Optionally restrict to one queue (by queueId) and/or a date range (by " +
                     "CreatedDate). Optionally apply samplePercentage on top of every other filter to draw a " +
                     "random subset instead of the most recent rows -- useful for taking an unbiased " +
                     "baseline sample to compare later, rather than only ever seeing the latest rows. The " +
                     "response also carries the true Total row count (ignoring 'take') and summary " +
                     "Statistics for this DTO's continuous measured columns (QueueDepth, DroppedCount) over " +
                     "the full filtered set.")]
        [ServiceOperation("CaptureQueueHealthList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<CaptureQueueHealthDto>> ListAsync(
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
                "Only include rows for this queue -- 1=ModelInvokeWarning, 2=CaseCreationWarning, 3=ArchiverWarning, 4=RedisSentinelEvent, 5=RedisConnectionEvent, 6=OpenTelemetryMetric; omit to include every queue.")]
            int? queueId = null,
            [Description(
                "Percentage (0-100) chance of including each row that already matches every other filter, evaluated independently per row -- e.g. 10 returns roughly a random one-tenth of matching rows, not the newest tenth. Omit for the normal most-recent-first behaviour. Not surfaced in any admin UI; intended for an agent building an unbiased baseline sample of activity to compare against later, rather than for browsing.")]
            double? samplePercentage = null,
            [Description(
                "Column to sort by (e.g. 'createdDate', 'queueDepth', 'queueName'); unrecognised or omitted falls back to most-recent-first.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaptureQueueHealth", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            var clampedSamplePercentage = samplePercentage.HasValue
                ? Math.Clamp(samplePercentage.Value, 0.0, 100.0)
                : (double?)null;
            from ??= DateTime.UtcNow.AddHours(-1);
            to ??= DateTime.UtcNow;
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaptureQueueHealth.List: entry take={clampedTake} user={userName}");
            }

            try
            {
                EnsurePermitted("CaptureQueueHealth.List");

                var rows = await repository.GetLastAsync(clampedTake, from, to, queueId, clampedSamplePercentage,
                    sortField, sortDirection, token).ConfigureAwait(false);

                var dtos = rows.Select(r => new CaptureQueueHealthDto(
                    r.Id, r.QueueId.GetValueOrDefault(),
                    DescribeQueue((CaptureQueueType)r.QueueId.GetValueOrDefault()),
                    r.QueueDepth.GetValueOrDefault(), r.DroppedCount.GetValueOrDefault(),
                    r.CreatedDate.GetValueOrDefault(), r.Instance)).ToList();

                var total = await repository.CountAsync(from, to, queueId, clampedSamplePercentage, token)
                    .ConfigureAwait(false);

                var statistics = await repository
                    .GetStatisticsAsync(from, to, queueId, clampedSamplePercentage, MaxListTake, token)
                    .ConfigureAwait(false);

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaptureQueueHealth.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<CaptureQueueHealthDto>(dtos, total, statistics);
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
                    log.Debug($"CaptureQueueHealth.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaptureQueueHealth.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private static string DescribeQueue(CaptureQueueType queueType)
        {
            return queueType switch
            {
                CaptureQueueType.ModelInvokeWarning => "Model Invoke Warning",
                CaptureQueueType.CaseCreationWarning => "Case Creation Warning",
                CaptureQueueType.ArchiverWarning => "Archiver Warning",
                CaptureQueueType.RedisSentinelEvent => "Redis Sentinel Event",
                CaptureQueueType.RedisConnectionEvent => "Redis Connection Event",
                CaptureQueueType.OpenTelemetryMetric => "OpenTelemetry Metric",
                _ => "Unknown"
            };
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

            throw new ForbiddenException(strings[CaptureQueueHealthResources.PermissionDenied], permissions);
        }
    }
}