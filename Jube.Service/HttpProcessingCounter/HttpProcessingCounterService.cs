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
using Jube.Dto.HttpProcessingCounter;
using Jube.Dto.Payload;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.HttpProcessingCounter;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.HttpProcessingCounter
{
    public sealed class HttpProcessingCounterService
    {
        private const int MaxListTake = 100000;
        private static readonly int[] permissions = [5, 9];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly HttpProcessingCounterRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private HttpProcessingCounterService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new HttpProcessingCounterRepository(dbContext);
        }

        public static Task<HttpProcessingCounterService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<HttpProcessingCounterService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(HttpProcessingCounterResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("HttpProcessingCounter.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[HttpProcessingCounterResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"HttpProcessingCounter.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[HttpProcessingCounterResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new HttpProcessingCounterService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists platform-wide HTTP processing counters, most recent first when no sort is given, " +
                     "capped at 'take' rows (max 100000, default 100000). Each row is a roughly one-minute " +
                     "interval's counts of HTTP requests by how they were routed -- Model (synchronous), " +
                     "AsynchronousModel, Tag, Error, Sanction (direct recall), Exhaustive (direct recall), and " +
                     "All (every request received). Not scoped to any tenant -- these are the whole instance's " +
                     "own HTTP throughput counters, not per-Model or per-tenant data. Each of from/to defaults " +
                     "independently to the last hour when omitted. Optionally apply samplePercentage on top of " +
                     "the date range to draw a random subset instead of the most recent rows -- useful for " +
                     "taking an unbiased baseline sample of activity to compare later, rather than only ever " +
                     "seeing the latest, potentially unrepresentative rows. The response also carries the true " +
                     "Total row count (ignoring 'take') and summary Statistics for this DTO's double-typed " +
                     "columns over the full filtered set for this DTO's continuous per-interval counters " +
                     "(Model, AsynchronousModel, Tag, Error, Sanction, Exhaustive, All).")]
        [ServiceOperation("HttpProcessingCounterList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<HttpProcessingCounterDto>> ListAsync(
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
                "Percentage (0-100) chance of including each row that already matches every other filter, evaluated independently per row -- e.g. 10 returns roughly a random one-tenth of matching rows, not the newest tenth. Omit for the normal most-recent-first behaviour. Not surfaced in any admin UI; intended for an agent building an unbiased baseline sample of activity to compare against later, rather than for browsing.")]
            double? samplePercentage = null,
            [Description(
                "Column to sort by (e.g. 'createdDate', 'all', 'model'); unrecognised or omitted falls back to most-recent-first.")]
            string? sortField = null,
            [Description("'asc' for ascending; anything else (including omitted) sorts descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("HttpProcessingCounter", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            var clampedSamplePercentage = samplePercentage.HasValue
                ? Math.Clamp(samplePercentage.Value, 0.0, 100.0)
                : (double?)null;
            from ??= DateTime.UtcNow.AddHours(-1);
            to ??= DateTime.UtcNow;
            if (log.IsDebugEnabled)
            {
                log.Debug($"HttpProcessingCounter.List: entry take={clampedTake} user={userName}");
            }

            try
            {
                EnsurePermitted("HttpProcessingCounter.List");

                var rows = await repository
                    .GetAsync(clampedTake, from, to, clampedSamplePercentage, sortField, sortDirection, token)
                    .ConfigureAwait(false);

                var dtos = rows.Select(r => new HttpProcessingCounterDto(
                    r.Instance,
                    r.CreatedDate.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(r.CreatedDate.Value, DateTimeKind.Utc))
                        : null,
                    r.Model.GetValueOrDefault(), r.AsynchronousModel.GetValueOrDefault(), r.Tag.GetValueOrDefault(),
                    r.Error.GetValueOrDefault(), r.Sanction.GetValueOrDefault(), r.Exhaustive.GetValueOrDefault(),
                    r.All.GetValueOrDefault())).ToList();

                var total = await repository.CountAsync(from, to, clampedSamplePercentage, token)
                    .ConfigureAwait(false);

                var statistics = await repository
                    .GetStatisticsAsync(from, to, clampedSamplePercentage, MaxListTake, token)
                    .ConfigureAwait(false);

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"HttpProcessingCounter.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<HttpProcessingCounterDto>(dtos, total, statistics);
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
                    log.Debug($"HttpProcessingCounter.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"HttpProcessingCounter.List: unexpected failure user={userName}", ex);
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

            throw new ForbiddenException(strings[HttpProcessingCounterResources.PermissionDenied], permissions);
        }
    }
}