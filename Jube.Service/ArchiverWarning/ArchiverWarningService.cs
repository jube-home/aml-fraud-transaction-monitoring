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
using Jube.Dto.ArchiverWarning;
using Jube.Dto.Payload;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.ArchiverWarning;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.ArchiverWarning
{
    public sealed class ArchiverWarningService
    {
        private const int MaxListTake = 100000;

        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly ArchiverWarningRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private ArchiverWarningService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new ArchiverWarningRepository(dbContext, userName);
        }

        public static Task<ArchiverWarningService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ArchiverWarningService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(ArchiverWarningResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("ArchiverWarning.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[ArchiverWarningResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"ArchiverWarning.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[ArchiverWarningResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ArchiverWarningService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists captured archiver warn-threshold breaches -- the specific invocation (when the " +
                     "stage operates on one item; null for BulkCopyArchiveBuffer, which is a batch), the model, " +
                     "and the stage that was slow -- so a slow archive backlog can be traced to the actual item " +
                     "and cause, not just the aggregate stage stats. Only populated for stages exceeding " +
                     "ArchiverWarnThresholdMilliseconds. Most recent first when no sort is given, capped at " +
                     "'take' rows (max 100000, default 100000). Each of from/to defaults independently to " +
                     "the last hour when omitted. Landlord only: every other caller is refused with a 403 (permission denied). Optionally restrict to a date range (by " +
                     "OccurredDate) and/or an exact stageId match. Optionally apply samplePercentage on top " +
                     "of every other filter to draw a random subset instead of the most recent rows -- " +
                     "useful for taking an unbiased baseline sample of activity to compare later, rather " +
                     "than only ever seeing the latest, potentially unrepresentative rows. The response also " +
                     "carries the true Total row count (ignoring 'take') and summary Statistics for this " +
                     "DTO's continuous measured columns (DurationMicroseconds) over the full filtered set.")]
        [ServiceOperation("ArchiverWarningList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<ArchiverWarningDto>> ListAsync(
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
                "Exact match against StageId (1=BuildArchiveJson, 2=CaseCreationDispatch, 3=RdbmsArchiveWrite, 4=BulkCopyArchiveBuffer); null matches every row.")]
            int? stageId = null,
            [Description(
                "Percentage (0-100) chance of including each row that already matches every other filter, evaluated independently per row -- e.g. 10 returns roughly a random one-tenth of matching rows, not the newest tenth. Omit for the normal most-recent-first behaviour. Not surfaced in any admin UI; intended for an agent building an unbiased baseline sample of activity to compare against later, rather than for browsing.")]
            double? samplePercentage = null,
            [Description(
                "Column to sort by (e.g. 'occurredDate', 'durationMicroseconds', 'entityAnalysisModelName'); unrecognised or omitted falls back to most-recent-first.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ArchiverWarning", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            var clampedSamplePercentage = samplePercentage.HasValue
                ? Math.Clamp(samplePercentage.Value, 0.0, 100.0)
                : (double?)null;
            from ??= DateTime.UtcNow.AddHours(-1);
            to ??= DateTime.UtcNow;
            if (log.IsDebugEnabled)
            {
                log.Debug($"ArchiverWarning.List: entry take={clampedTake} user={userName}");
            }

            try
            {
                EnsurePermitted("ArchiverWarning.List");

                var rows = await repository
                    .GetLastAsync(permissionValidation.Landlord, clampedTake, from, to, stageId,
                        clampedSamplePercentage, sortField, sortDirection, token)
                    .ConfigureAwait(false);

                var dtos = rows.Select(r => new ArchiverWarningDto(
                    r.Id, r.OccurredDate.GetValueOrDefault(), r.EntityAnalysisModelGuid, r.EntityAnalysisModelName,
                    r.EntityAnalysisModelInstanceEntryGuid, r.StageId.GetValueOrDefault(),
                    ((ArchiverStage)r.StageId.GetValueOrDefault()).Describe(),
                    r.DurationMicroseconds.GetValueOrDefault(),
                    r.CreatedDate.GetValueOrDefault(), r.Instance)).ToList();

                var total = await repository
                    .CountAsync(permissionValidation.Landlord, from, to, stageId, clampedSamplePercentage, token)
                    .ConfigureAwait(false);

                var statistics = await repository
                    .GetStatisticsAsync(permissionValidation.Landlord, from, to, stageId, clampedSamplePercentage,
                        MaxListTake, token)
                    .ConfigureAwait(false);

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"ArchiverWarning.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<ArchiverWarningDto>(dtos, total, statistics);
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
                    log.Debug($"ArchiverWarning.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"ArchiverWarning.List: unexpected failure user={userName}", ex);
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

            throw new ForbiddenException(strings[ArchiverWarningResources.PermissionDenied], permissions);
        }
    }
}