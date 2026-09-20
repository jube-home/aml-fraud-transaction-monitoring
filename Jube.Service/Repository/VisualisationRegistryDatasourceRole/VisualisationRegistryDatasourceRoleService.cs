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
using Jube.Dto.Repository.VisualisationRegistryDatasourceRole;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.VisualisationRegistryDatasourceRole;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.VisualisationRegistryDatasourceRole;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.VisualisationRegistryDatasourceRoleRepository;

namespace Jube.Service.Repository.VisualisationRegistryDatasourceRole
{
    public sealed class VisualisationRegistryDatasourceRoleService
    {
        private static readonly int[] permissions = [33];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly VisualisationRegistryDatasourceRoleDtoValidator validator;

        private VisualisationRegistryDatasourceRoleService(DbContext dbContext, string userName,
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
            repository = new RuleRepository(dbContext, userName);
            validator = new VisualisationRegistryDatasourceRoleDtoValidator(repository, strings);
        }

        public static Task<VisualisationRegistryDatasourceRoleService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<VisualisationRegistryDatasourceRoleService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(VisualisationRegistryDatasourceRoleResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("VisualisationRegistryDatasourceRole.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[VisualisationRegistryDatasourceRoleResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"VisualisationRegistryDatasourceRole.Create: user '{userName}' resolves to no " +
                             "tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[VisualisationRegistryDatasourceRoleResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new VisualisationRegistryDatasourceRoleService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the Role grants for a Visualisation Registry Datasource, visible to the caller's tenant.")]
        [ServiceOperation("VisualisationRegistryDatasourceRoleListByVisualisationRegistryDatasourceGuid",
            OperationKind.Read, Idempotent = true)]
        public async Task<List<VisualisationRegistryDatasourceRoleDto>> GetByVisualisationRegistryDatasourceGuidAsync(
            [Description("Guid of the Visualisation Registry Datasource to list role grants for.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasourceRole",
                "ListByVisualisationRegistryDatasourceGuid", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug("VisualisationRegistryDatasourceRole.ListByVisualisationRegistryDatasourceGuid: entry " +
                          $"guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistryDatasourceRole.ListByVisualisationRegistryDatasourceGuid",
                    permissions);
                var dtos = VisualisationRegistryDatasourceRoleMapper.ToDto(
                    await repository.GetByVisualisationRegistryDatasourceGuidAsync(guid, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug("VisualisationRegistryDatasourceRole.ListByVisualisationRegistryDatasourceGuid: " +
                              $"{dtos.Count} rows guid={guid} user={userName}");
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
                log.Error("VisualisationRegistryDatasourceRole.ListByVisualisationRegistryDatasourceGuid: " +
                          $"unexpected failure guid={guid} user={userName}", ex);
                throw;
            }
        }

        [Description("Grants a Role access to a Visualisation Registry Datasource.")]
        [ServiceOperation("VisualisationRegistryDatasourceRoleCreate", OperationKind.Write, Idempotent = false)]
        public async Task<VisualisationRegistryDatasourceRoleDto> InsertAsync(
            VisualisationRegistryDatasourceRoleDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasourceRole", "Insert", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryDatasourceRole.Insert: entry user={userName} " +
                          $"visualisationRegistryDatasourceGuid={model?.VisualisationRegistryDatasourceGuid} " +
                          $"roleRegistryGuid={model?.RoleRegistryGuid}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("VisualisationRegistryDatasourceRole.Insert", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"VisualisationRegistryDatasourceRole.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(VisualisationRegistryDatasourceRoleMapper.ToPoco(model),
                    token).ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"VisualisationRegistryDatasourceRole.Insert: created Id={saved.Id} user={userName}");
                }

                return VisualisationRegistryDatasourceRoleMapper.ToDto(saved);
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
                log.Error($"VisualisationRegistryDatasourceRole.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Removes a Role's grant on a Visualisation Registry Datasource. The row must exist and be " +
                     "visible to the caller's tenant; unlike other areas in this system, a missing or invisible " +
                     "row surfaces as an unexpected failure here, not a distinguishable not-found result -- this " +
                     "preserves the original controller's own behaviour, and means a retry of this call is not " +
                     "safe to assume succeeded, unlike other Delete operations in this catalogue.")]
        [ServiceOperation("VisualisationRegistryDatasourceRoleDelete", OperationKind.Delete, Idempotent = false,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Visualisation Registry Datasource Role grant to remove.")]
            int id, CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasourceRole", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryDatasourceRole.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistryDatasourceRole.Delete", permissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"VisualisationRegistryDatasourceRole.Delete: soft-deleted Id={id} user={userName}");
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
                log.Error($"VisualisationRegistryDatasourceRole.Delete: unexpected failure id={id} user={userName}",
                    ex);
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

            throw new ForbiddenException(strings[VisualisationRegistryDatasourceRoleResources.PermissionDenied],
                specs);
        }
    }
}