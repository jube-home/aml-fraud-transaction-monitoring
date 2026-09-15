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
using Jube.Dto.OtlpDispatchCounter;
using Jube.Dto.Payload;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.OtlpDispatchCounter;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.OtlpDispatchCounter
{
    public sealed class OtlpDispatchCounterService
    {
        private const int MaxListTake = 100000;
        private static readonly int[] permissions = [27];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly OtlpDispatchCounterRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private OtlpDispatchCounterService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new OtlpDispatchCounterRepository(dbContext);
        }

        public static Task<OtlpDispatchCounterService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<OtlpDispatchCounterService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(OtlpDispatchCounterResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("OtlpDispatchCounter.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[OtlpDispatchCounterResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"OtlpDispatchCounter.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[OtlpDispatchCounterResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new OtlpDispatchCounterService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists per-minute OTLP export dispatch counters -- one row per distinct Signal " +
                     "(\"traces\"/\"metrics\"/\"logs\") per minute, with Count/SuccessCount/FailureCount/" +
                     "ItemCount/DroppedCount and Total/Min/MaxMicroseconds for that window -- most recent " +
                     "first when no sort is given, capped at 'take' rows (max 100000, default 100000). " +
                     "Answers exactly how OTLP export to OpenTelemetryBackendEndpoint is behaving: dispatch " +
                     "volume, response times, outright failures (a dead/unreachable backend), and drops due " +
                     "to the export queue being full (queue size truncation) -- without needing an external " +
                     "OTel collector of its own. Each of from/to defaults independently to the last hour " +
                     "when omitted. Not scoped to any Model or tenant. Optionally restrict to a date range " +
                     "(by CreatedDate) and/or an exact signalId match. The response also carries the true " +
                     "Total row count (ignoring 'take') and summary Statistics for this DTO's continuous " +
                     "measured columns (Count, SuccessCount, FailureCount, ItemCount, DroppedCount, " +
                     "TotalMicroseconds, MinMicroseconds, MaxMicroseconds) over the full filtered set.")]
        [ServiceOperation("OtlpDispatchCounterList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<OtlpDispatchCounterDto>> ListAsync(
            [Description(
                "Maximum number of rows to return, most recent first when no sort is given; clamped to 100000.")]
            int take = 100000,
            [Description(
                "Only include rows with CreatedDate on or after this UTC timestamp; defaults to 1 hour before the current time when omitted.")]
            DateTime? from = null,
            [Description(
                "Only include rows with CreatedDate on or before this UTC timestamp; defaults to the current time when omitted.")]
            DateTime? to = null,
            [Description("Exact match against SignalId (1=Traces, 2=Metrics, 3=Logs); null matches every row.")]
            int? signalId = null,
            [Description(
                "Percentage (0-100) chance of including each row that already matches every other filter, evaluated independently per row. Omit for the normal most-recent-first behaviour.")]
            double? samplePercentage = null,
            [Description(
                "Column to sort by (e.g. 'createdDate', 'count', 'signalName'); unrecognised or omitted falls back to most-recent-first.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("OtlpDispatchCounter", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            var clampedSamplePercentage = samplePercentage.HasValue
                ? Math.Clamp(samplePercentage.Value, 0.0, 100.0)
                : (double?)null;
            from ??= DateTime.UtcNow.AddHours(-1);
            to ??= DateTime.UtcNow;
            if (log.IsDebugEnabled)
            {
                log.Debug($"OtlpDispatchCounter.List: entry take={clampedTake} user={userName}");
            }

            try
            {
                EnsurePermitted("OtlpDispatchCounter.List");

                var rows = await repository.GetLastAsync(clampedTake, from, to, signalId, clampedSamplePercentage,
                    sortField, sortDirection, token).ConfigureAwait(false);

                var dtos = rows.Select(r => new OtlpDispatchCounterDto(
                    r.Id, r.SignalId.GetValueOrDefault(), DescribeSignal((OtlpSignal)r.SignalId.GetValueOrDefault()),
                    r.Count.GetValueOrDefault(), r.SuccessCount.GetValueOrDefault(),
                    r.FailureCount.GetValueOrDefault(), r.ItemCount.GetValueOrDefault(),
                    r.DroppedCount.GetValueOrDefault(), r.TotalMicroseconds.GetValueOrDefault(),
                    r.MinMicroseconds.GetValueOrDefault(), r.MaxMicroseconds.GetValueOrDefault(),
                    r.CreatedDate.GetValueOrDefault(), r.Instance)).ToList();

                var total = await repository.CountAsync(from, to, signalId, clampedSamplePercentage, token)
                    .ConfigureAwait(false);

                var statistics = await repository
                    .GetStatisticsAsync(from, to, signalId, clampedSamplePercentage, MaxListTake, token)
                    .ConfigureAwait(false);

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"OtlpDispatchCounter.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<OtlpDispatchCounterDto>(dtos, total, statistics);
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
                    log.Debug($"OtlpDispatchCounter.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"OtlpDispatchCounter.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private static string DescribeSignal(OtlpSignal signal)
        {
            return signal switch
            {
                OtlpSignal.Traces => "Traces",
                OtlpSignal.Metrics => "Metrics",
                OtlpSignal.Logs => "Logs",
                _ => "Unknown"
            };
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[OtlpDispatchCounterResources.PermissionDenied], permissions);
        }
    }
}