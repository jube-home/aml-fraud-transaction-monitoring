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
using Jube.Dto.Repository.EntityAnalysisModelRole;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.EntityAnalysisModelRole;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.EntityAnalysisModelRole;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.EntityAnalysisModelRoleRepository;

namespace Jube.Service.Repository.EntityAnalysisModelRole
{
    public sealed class EntityAnalysisModelRoleService
    {
        private static readonly int[] permissions = [6];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelRoleDtoValidator validator;

        private EntityAnalysisModelRoleService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new RuleRepository(dbContext, userName);
            validator = new EntityAnalysisModelRoleDtoValidator(repository, strings);
        }

        public static Task<EntityAnalysisModelRoleService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelRoleService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelRoleResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelRole.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelRoleResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"EntityAnalysisModelRole.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelRoleResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelRoleService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the Role grants for an Entity Analysis Model, visible to the caller's tenant.")]
        [ServiceOperation("EntityAnalysisModelRoleListByEntityAnalysisModelGuid", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelRoleDto>> GetByEntityAnalysisModelGuidAsync(
            [Description("Guid of the Entity Analysis Model to list role grants for.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRole", "ListByEntityAnalysisModelGuid", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelRole.ListByEntityAnalysisModelGuid: entry guid={guid} " +
                          $"user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelRole.ListByEntityAnalysisModelGuid", permissions);
                var dtos = EntityAnalysisModelRoleMapper.ToDto(
                    await repository.GetByEntityAnalysisModelGuidAsync(guid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelRole.ListByEntityAnalysisModelGuid: {dtos.Count} rows " +
                              $"guid={guid} user={userName}");
                }

                return dtos;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelRole.ListByEntityAnalysisModelGuid: unexpected failure guid={guid} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Grants a Role access to an Entity Analysis Model.")]
        [ServiceOperation("EntityAnalysisModelRoleCreate", OperationKind.Write, Idempotent = false)]
        public async Task<EntityAnalysisModelRoleDto> InsertAsync(EntityAnalysisModelRoleDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRole", "Insert", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelRole.Insert: entry user={userName} " +
                          $"entityAnalysisModelGuid={model?.EntityAnalysisModelGuid} " +
                          $"roleRegistryGuid={model?.RoleRegistryGuid}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelRole.Insert", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelRole.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(EntityAnalysisModelRoleMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelRole.Insert: created Id={saved.Id} user={userName}");
                }

                return EntityAnalysisModelRoleMapper.ToDto(saved);
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
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelRole.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Removes a Role's grant on an Entity Analysis Model. The row must exist and be visible to " +
                     "the caller's tenant; unlike other areas in this system, a missing or invisible row surfaces " +
                     "as an unexpected failure here, not a distinguishable not-found result -- this preserves the " +
                     "original controller's own behaviour, and means a retry of this call is not safe to assume " +
                     "succeeded, unlike other Delete operations in this catalogue.")]
        [ServiceOperation("EntityAnalysisModelRoleDelete", OperationKind.Delete, Idempotent = false,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Entity Analysis Model Role grant to remove.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRole", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelRole.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelRole.Delete", permissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelRole.Delete: soft-deleted Id={id} user={userName}");
                }
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelRole.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(string op, int[] specs)
        {
            if (permissionValidation.Validate(specs))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", specs)}]");
            }

            throw new ForbiddenException(strings[EntityAnalysisModelRoleResources.PermissionDenied], specs);
        }
    }
}