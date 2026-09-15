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
using Jube.Dto.EntityAnalysisModelTaskPerformanceCounter;
using Jube.Dto.Payload;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelTaskPerformanceCounter;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.TaskCancellation.TaskHelper;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelTaskPerformanceCounter
{
    public sealed class EntityAnalysisModelTaskPerformanceCounterService
    {
        private const int MaxListTake = 100000;
        private static readonly int[] permissions = [27];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelTaskPerformanceCounterRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisModelTaskPerformanceCounterService(DbContext dbContext, string userName,
            int tenantRegistryId, PermissionValidation permissionValidation, ILog log, ILog auditLog,
            IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new EntityAnalysisModelTaskPerformanceCounterRepository(dbContext, userName);
        }

        public static Task<EntityAnalysisModelTaskPerformanceCounterService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelTaskPerformanceCounterService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelTaskPerformanceCounterResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelTaskPerformanceCounter.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelTaskPerformanceCounterResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelTaskPerformanceCounter.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelTaskPerformanceCounterResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelTaskPerformanceCounterService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the most recent Task Performance Counter rows across every Model, most recent " +
                     "first when no sort is given, capped at 'take' rows (max 100000, default 100000). Each " +
                     "of from/to defaults independently to the last hour when omitted. Each row is one async " +
                     "read/write task's aggregated compute time and allocation for a roughly one-minute " +
                     "interval -- useful for cloud compute-cost attribution. Optionally restrict to a date " +
                     "range (by CreatedDate), an exact directionId, and/or an exact taskTypeId match. " +
                     "Landlord callers see rows for every tenant; other callers only see rows for Models in " +
                     "their own tenant. Optionally apply samplePercentage on top of every other filter to " +
                     "draw a random subset instead of the most recent rows -- useful for taking an unbiased " +
                     "baseline sample of activity to compare later, rather than only ever seeing the latest, " +
                     "potentially unrepresentative rows. The response also carries the true Total row count " +
                     "(ignoring 'take') and summary Statistics for this DTO's continuous measured columns " +
                     "(TotalMicroseconds, MinMicroseconds, MaxMicroseconds, TotalAllocatedBytes, " +
                     "MinAllocatedBytes, MaxAllocatedBytes, InvokeCount) over the full filtered set.")]
        [ServiceOperation("EntityAnalysisModelTaskPerformanceCounterList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<EntityAnalysisModelTaskPerformanceCounterDto>> ListAsync(
            [Description(
                "Maximum number of rows to return, most recent first when no sort is given; clamped to 100000.")]
            int take = 100000,
            [Description(
                "Only include rows with CreatedDate on or after this UTC timestamp; defaults to 1 hour before the current time when omitted.")]
            DateTime? from = null,
            [Description(
                "Only include rows with CreatedDate on or before this UTC timestamp; defaults to the current time when omitted.")]
            DateTime? to = null,
            [Description("Exact match against DirectionId (1=Read, 2=Write); null matches every row.")]
            int? directionId = null,
            [Description(
                "Exact match against TaskTypeId, using Jube.TaskCancellation.TaskHelper.TaskType's numeric values (e.g. 1=SanctionsAsync, 2=TtlCountersAsync); null matches every row.")]
            int? taskTypeId = null,
            [Description(
                "Percentage (0-100) chance of including each row that already matches every other filter, evaluated independently per row -- e.g. 10 returns roughly a random one-tenth of matching rows, not the newest tenth. Omit for the normal most-recent-first behaviour. Not surfaced in any admin UI; intended for an agent building an unbiased baseline sample of activity to compare against later, rather than for browsing.")]
            double? samplePercentage = null,
            [Description(
                "Column to sort by (e.g. 'createdDate', 'totalMicroseconds', 'taskName'); unrecognised or omitted falls back to most-recent-first.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTaskPerformanceCounter", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            var clampedSamplePercentage = samplePercentage.HasValue
                ? Math.Clamp(samplePercentage.Value, 0.0, 100.0)
                : (double?)null;
            from ??= DateTime.UtcNow.AddHours(-1);
            to ??= DateTime.UtcNow;
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelTaskPerformanceCounter.List: entry take={clampedTake} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelTaskPerformanceCounter.List");

                var rows = await repository
                    .GetLastAsync(permissionValidation.Landlord, clampedTake, from, to, directionId, taskTypeId,
                        clampedSamplePercentage, sortField, sortDirection, token)
                    .ConfigureAwait(false);

                var dtos = rows.Select(r => new EntityAnalysisModelTaskPerformanceCounterDto(
                    r.Id, r.EntityAnalysisModelGuid, r.EntityAnalysisModelName, r.DirectionId.GetValueOrDefault(),
                    DescribeDirection((TaskDirection)r.DirectionId.GetValueOrDefault()),
                    r.TaskTypeId.GetValueOrDefault(), DescribeTaskType((TaskType)r.TaskTypeId.GetValueOrDefault()),
                    r.TotalMicroseconds.GetValueOrDefault(), r.MinMicroseconds.GetValueOrDefault(),
                    r.MaxMicroseconds.GetValueOrDefault(), r.TotalAllocatedBytes.GetValueOrDefault(),
                    r.MinAllocatedBytes.GetValueOrDefault(), r.MaxAllocatedBytes.GetValueOrDefault(),
                    r.InvokeCount.GetValueOrDefault(), r.CreatedDate.GetValueOrDefault(), r.Instance)).ToList();

                var total = await repository
                    .CountAsync(permissionValidation.Landlord, from, to, directionId, taskTypeId,
                        clampedSamplePercentage, token)
                    .ConfigureAwait(false);

                var statistics = await repository
                    .GetStatisticsAsync(permissionValidation.Landlord, from, to, directionId, taskTypeId,
                        clampedSamplePercentage, MaxListTake, token)
                    .ConfigureAwait(false);

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelTaskPerformanceCounter.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<EntityAnalysisModelTaskPerformanceCounterDto>(dtos, total, statistics);
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
                    log.Debug($"EntityAnalysisModelTaskPerformanceCounter.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelTaskPerformanceCounter.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private static string DescribeDirection(TaskDirection direction)
        {
            return Enum.IsDefined(direction) ? direction.ToString() : "Unknown";
        }

        private static string DescribeTaskType(TaskType taskType)
        {
            return Enum.IsDefined(taskType) ? taskType.ToString() : "Unknown";
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

            throw new ForbiddenException(
                strings[EntityAnalysisModelTaskPerformanceCounterResources.PermissionDenied], permissions);
        }
    }
}