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
using Jube.Dto.DockerContainerMetric;
using Jube.Dto.Payload;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.DockerContainerMetric;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.DockerContainerMetric
{
    public sealed class DockerContainerMetricService
    {
        private const int MaxListTake = 100000;

        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly DockerContainerMetricRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private DockerContainerMetricService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new DockerContainerMetricRepository(dbContext);
        }

        public static Task<DockerContainerMetricService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<DockerContainerMetricService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(DockerContainerMetricResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("DockerContainerMetric.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[DockerContainerMetricResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"DockerContainerMetric.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[DockerContainerMetricResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new DockerContainerMetricService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists per-minute, per-container Docker metrics captured by the Jube.Monitoring sidecar " +
                     "-- state, health, restart count, CPU/memory/network/block-io usage and pids -- across " +
                     "every host, most recent first when no sort is given, capped at 'take' rows (max " +
                     "100000, default 100000). Each of from/to defaults independently to the last hour when " +
                     "omitted. Not scoped to any Model or tenant. Optionally restrict to a date range (by " +
                     "OccurredDate) and/or a case-insensitive substring search against Name, Image, State or " +
                     "Status. Optionally apply samplePercentage on top of every other filter to draw a " +
                     "random subset instead of the most recent rows -- useful for taking an unbiased " +
                     "baseline sample of activity to compare later, rather than only ever seeing the latest, " +
                     "potentially unrepresentative rows. The response also carries the true Total row count " +
                     "(ignoring 'take') and summary Statistics for this DTO's continuous measured columns " +
                     "(RestartCount, CpuUsagePercent, OnlineCpus, MemoryUsageBytes, MemoryLimitBytes, " +
                     "MemoryPercent, NetworkRxBytes, NetworkTxBytes, BlockReadBytes, BlockWriteBytes, " +
                     "PidsCurrent, PidsLimit) over the full filtered set.")]
        [ServiceOperation("DockerContainerMetricList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<DockerContainerMetricDto>> ListAsync(
            [Description(
                "Maximum number of rows to return, most recent first when no sort is given; clamped to 100000.")]
            int take = 100000,
            [Description(
                "Only include rows with OccurredDate on or after this UTC timestamp; defaults to 1 hour before the current time when omitted.")]
            DateTime? from = null,
            [Description(
                "Only include rows with OccurredDate on or before this UTC timestamp; defaults to the current time when omitted.")]
            DateTime? to = null,
            [Description(
                "Case-insensitive substring match against Name, Image, State or Status; null or empty matches every row.")]
            string? search = null,
            [Description(
                "Percentage (0-100) chance of including each row that already matches every other filter, evaluated independently per row -- e.g. 10 returns roughly a random one-tenth of matching rows, not the newest tenth. Omit for the normal most-recent-first behaviour. Not surfaced in any admin UI; intended for an agent building an unbiased baseline sample of activity to compare against later, rather than for browsing.")]
            double? samplePercentage = null,
            [Description(
                "Column to sort by (e.g. 'occurredDate', 'cpuUsagePercent', 'memoryUsageBytes'); unrecognised or omitted falls back to most-recent-first.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("DockerContainerMetric", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            var clampedSamplePercentage = samplePercentage.HasValue
                ? Math.Clamp(samplePercentage.Value, 0.0, 100.0)
                : (double?)null;
            from ??= DateTime.UtcNow.AddHours(-1);
            to ??= DateTime.UtcNow;
            if (log.IsDebugEnabled)
            {
                log.Debug($"DockerContainerMetric.List: entry take={clampedTake} user={userName}");
            }

            try
            {
                EnsurePermitted("DockerContainerMetric.List");

                var rows = await repository.GetLastAsync(clampedTake, from, to, search, clampedSamplePercentage,
                    sortField, sortDirection, token).ConfigureAwait(false);

                var dtos = rows.Select(r => new DockerContainerMetricDto(
                    r.Id, r.OccurredDate.GetValueOrDefault(), r.ContainerId, r.Name, r.Image, r.State, r.Status,
                    r.RestartCount.GetValueOrDefault(), r.OomKilled.GetValueOrDefault(),
                    r.ExitCode.GetValueOrDefault(), r.StartedAt, r.HealthStatus, r.CpuUsagePercent, r.OnlineCpus,
                    r.MemoryUsageBytes, r.MemoryLimitBytes, r.MemoryPercent, r.NetworkRxBytes, r.NetworkTxBytes,
                    r.BlockReadBytes, r.BlockWriteBytes, r.PidsCurrent, r.PidsLimit,
                    r.CreatedDate.GetValueOrDefault(), r.Instance)).ToList();

                var total = await repository.CountAsync(from, to, search, clampedSamplePercentage, token)
                    .ConfigureAwait(false);

                var statistics = await repository
                    .GetStatisticsAsync(from, to, search, clampedSamplePercentage, MaxListTake, token)
                    .ConfigureAwait(false);

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"DockerContainerMetric.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<DockerContainerMetricDto>(dtos, total, statistics);
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
                    log.Debug($"DockerContainerMetric.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"DockerContainerMetric.List: unexpected failure user={userName}", ex);
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

            throw new ForbiddenException(strings[DockerContainerMetricResources.PermissionDenied], permissions);
        }
    }
}