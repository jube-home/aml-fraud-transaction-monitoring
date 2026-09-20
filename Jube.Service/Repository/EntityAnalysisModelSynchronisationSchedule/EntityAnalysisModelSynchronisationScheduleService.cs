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
using Jube.Dto.Repository.EntityAnalysisModelSynchronisationSchedule;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.EntityAnalysisModelSynchronisationSchedule;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.EntityAnalysisModelSynchronisationSchedule;
using log4net;
using Microsoft.Extensions.Localization;
using SchedulePoco = Jube.Data.Poco.EntityAnalysisModelSynchronisationSchedule;
using ScheduleRepository = Jube.Data.Repository.EntityAnalysisModelSynchronisationScheduleRepository;

namespace Jube.Service.Repository.EntityAnalysisModelSynchronisationSchedule
{
    public sealed class EntityAnalysisModelSynchronisationScheduleService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [5];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly ScheduleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelSynchronisationScheduleDtoValidator validator;

        private EntityAnalysisModelSynchronisationScheduleService(DbContext dbContext, string userName,
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
            repository = new ScheduleRepository(dbContext, userName);
            validator = new EntityAnalysisModelSynchronisationScheduleDtoValidator(strings);
        }

        public static Task<EntityAnalysisModelSynchronisationScheduleService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelSynchronisationScheduleService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelSynchronisationScheduleResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelSynchronisationSchedule.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelSynchronisationScheduleResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelSynchronisationSchedule.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelSynchronisationScheduleResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelSynchronisationScheduleService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Returns the most recently scheduled model-synchronisation row for the caller's tenant -- " +
                     "the date and time every node in the cluster is next expected to synchronise its compiled " +
                     "models by. Returns null when no synchronisation has ever been scheduled for the tenant.")]
        [ServiceOperation("EntityAnalysisModelSynchronisationScheduleGetCurrent", OperationKind.Read,
            Idempotent = true)]
        public async Task<EntityAnalysisModelSynchronisationScheduleDto?> GetCurrentAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSynchronisationSchedule", "GetCurrent", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelSynchronisationSchedule.GetCurrent: entry user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelSynchronisationSchedule.GetCurrent");
                var schedulePoco = await repository.GetCurrentAsync(token).ConfigureAwait(false);
                if (schedulePoco == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"EntityAnalysisModelSynchronisationSchedule.GetCurrent: no schedule found user={userName}");
                    }

                    return null;
                }

                op.Entity(schedulePoco.Id);
                return EntityAnalysisModelSynchronisationScheduleMapper.ToDto(schedulePoco);
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
                    log.Debug($"EntityAnalysisModelSynchronisationSchedule.GetCurrent: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelSynchronisationSchedule.GetCurrent: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists scheduled model-synchronisation rows for the caller's tenant, ordered by Id, capped " +
                     "at 'take' rows (max 200). Call again with 'afterId' set to the last returned Id to page " +
                     "through the rest.")]
        [ServiceOperation("EntityAnalysisModelSynchronisationScheduleList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelSynchronisationScheduleDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSynchronisationSchedule", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelSynchronisationSchedule.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelSynchronisationSchedule.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelSynchronisationScheduleDto>(
                    EntityAnalysisModelSynchronisationScheduleMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelSynchronisationSchedule.ListPaged: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelSynchronisationSchedule.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Schedules model synchronisation across the cluster for the caller's tenant. When " +
                     "ScheduleDate is omitted, the server schedules it for the current time -- an immediate " +
                     "'Synchronise Now'. Not idempotent -- calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelSynchronisationScheduleCreate", OperationKind.Write, Idempotent = false)]
        public async Task<SchedulePoco> InsertAsync(
            [Description("The synchronisation schedule to create.")]
            EntityAnalysisModelSynchronisationScheduleDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSynchronisationSchedule", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelSynchronisationSchedule.Create: entry user={userName} scheduleDate={model?.ScheduleDate}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelSynchronisationSchedule.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelSynchronisationSchedule.Create: validation failed user={userName} " +
                            $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository
                    .InsertAsync(EntityAnalysisModelSynchronisationScheduleMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"EntityAnalysisModelSynchronisationSchedule.Create: created Id={saved.Id} scheduleDate={saved.ScheduleDate} user={userName}");
                }

                return saved;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (DtoValidationException)
            {
                op.Outcome("invalid");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelSynchronisationSchedule.Create: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelSynchronisationSchedule.Create: unexpected failure user={userName}",
                    ex);
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

            throw new ForbiddenException(strings[EntityAnalysisModelSynchronisationScheduleResources.PermissionDenied],
                permissions);
        }
    }
}