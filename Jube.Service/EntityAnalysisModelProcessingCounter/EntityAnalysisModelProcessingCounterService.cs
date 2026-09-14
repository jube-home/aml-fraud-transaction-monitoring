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
using Jube.Dto.EntityAnalysisModelProcessingCounter;
using Jube.Dto.Payload;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelProcessingCounter;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelProcessingCounter
{
    public sealed class EntityAnalysisModelProcessingCounterService
    {
        private const int MaxListTake = 100000;
        private static readonly int[] permissions = [27];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelProcessingCounterRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisModelProcessingCounterService(DbContext dbContext, string userName,
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
            repository = new EntityAnalysisModelProcessingCounterRepository(dbContext, userName);
        }

        public static Task<EntityAnalysisModelProcessingCounterService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelProcessingCounterService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelProcessingCounterResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelProcessingCounter.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelProcessingCounterResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelProcessingCounter.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelProcessingCounterResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelProcessingCounterService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists per-Model processing counter rows, most recent first when no sort is given, " +
                     "capped at 'take' rows (max 100000, default 100000). Each row is a roughly one-minute " +
                     "interval's counts for one Entity Analysis Model -- synchronous invocations, Gateway " +
                     "Rule matches, response-elevation triggers and their summed value, activation-watcher " +
                     "triggers, response times, and pending archive WAL entries. Each of from/to defaults " +
                     "independently to the last hour when omitted. Optionally restrict to a case-insensitive " +
                     "substring match on the Model's name. Landlord callers see rows for every tenant; other " +
                     "callers only see rows for Models in their own tenant. Optionally apply samplePercentage " +
                     "on top of every other filter to draw a random subset instead of the most recent rows -- " +
                     "useful for taking an unbiased baseline sample of activity to compare later, rather than " +
                     "only ever seeing the latest, potentially unrepresentative rows. The response also " +
                     "carries the true Total row count (ignoring 'take') and summary Statistics for this " +
                     "DTO's continuous measured columns (ModelInvoke, GatewayMatch, ResponseElevation, " +
                     "ResponseElevationSum, ActivationWatcher, ResponseElevationLimit, " +
                     "ModelTotalResponseTime, MinResponseTimeMicroseconds, MaxResponseTimeMicroseconds, " +
                     "ArchiveWalPendingCount) over the full filtered set. Id is always 0 -- a pre-existing " +
                     "gap in the legacy read path, which never selected a row identifier, carried forward " +
                     "unchanged.")]
        [ServiceOperation("EntityAnalysisModelProcessingCounterList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<EntityAnalysisModelProcessingCounterDto>> ListAsync(
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
                "Case-insensitive substring match against the Entity Analysis Model's name; null matches every row.")]
            string? search = null,
            [Description(
                "Percentage (0-100) chance of including each row that already matches every other filter, evaluated independently per row -- e.g. 10 returns roughly a random one-tenth of matching rows, not the newest tenth. Omit for the normal most-recent-first behaviour. Not surfaced in any admin UI; intended for an agent building an unbiased baseline sample of activity to compare against later, rather than for browsing.")]
            double? samplePercentage = null,
            [Description(
                "Column to sort by (e.g. 'createdDate', 'modelInvoke', 'responseElevationSum', 'name'); unrecognised or omitted falls back to most-recent-first.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelProcessingCounter", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            var clampedSamplePercentage = samplePercentage.HasValue
                ? Math.Clamp(samplePercentage.Value, 0.0, 100.0)
                : (double?)null;
            from ??= DateTime.UtcNow.AddHours(-1);
            to ??= DateTime.UtcNow;
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelProcessingCounter.List: entry take={clampedTake} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelProcessingCounter.List");

                var rows = await repository
                    .GetLastAsync(permissionValidation.Landlord, clampedTake, from, to, search,
                        clampedSamplePercentage, sortField, sortDirection, token)
                    .ConfigureAwait(false);

                var dtos = rows.Select(r => new EntityAnalysisModelProcessingCounterDto(
                    r.EntityAnalysisModelName, 0,
                    r.CreatedDate.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(r.CreatedDate.Value, DateTimeKind.Utc))
                        : null,
                    r.Instance, r.ModelInvoke.GetValueOrDefault(), r.GatewayMatch.GetValueOrDefault(),
                    r.ResponseElevation.GetValueOrDefault(), r.ResponseElevationSum.GetValueOrDefault(),
                    r.ActivationWatcher.GetValueOrDefault(), r.ResponseElevationLimit.GetValueOrDefault(),
                    r.ModelTotalResponseTime, r.MinResponseTimeMicroseconds, r.MaxResponseTimeMicroseconds,
                    r.ArchiveWalPendingCount, r.EntityAnalysisModelGuid)).ToList();

                var total = await repository
                    .CountAsync(permissionValidation.Landlord, from, to, search, clampedSamplePercentage, token)
                    .ConfigureAwait(false);

                var statistics = await repository
                    .GetStatisticsAsync(permissionValidation.Landlord, from, to, search, clampedSamplePercentage,
                        MaxListTake, token)
                    .ConfigureAwait(false);

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelProcessingCounter.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<EntityAnalysisModelProcessingCounterDto>(dtos, total, statistics);
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
                    log.Debug($"EntityAnalysisModelProcessingCounter.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelProcessingCounter.List: unexpected failure user={userName}", ex);
                throw;
            }
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
                strings[EntityAnalysisModelProcessingCounterResources.PermissionDenied], permissions);
        }
    }
}