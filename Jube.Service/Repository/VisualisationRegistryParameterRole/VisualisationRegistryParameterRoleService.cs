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
using Jube.Dto.Repository.VisualisationRegistryParameterRole;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.VisualisationRegistryParameterRole;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.VisualisationRegistryParameterRole;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.VisualisationRegistryParameterRoleRepository;

namespace Jube.Service.Repository.VisualisationRegistryParameterRole
{
    public sealed class VisualisationRegistryParameterRoleService
    {
        private static readonly int[] permissions = [32];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly VisualisationRegistryParameterRoleDtoValidator validator;

        private VisualisationRegistryParameterRoleService(DbContext dbContext, string userName,
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
            validator = new VisualisationRegistryParameterRoleDtoValidator(repository, strings);
        }

        public static Task<VisualisationRegistryParameterRoleService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<VisualisationRegistryParameterRoleService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(VisualisationRegistryParameterRoleResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("VisualisationRegistryParameterRole.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[VisualisationRegistryParameterRoleResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"VisualisationRegistryParameterRole.Create: user '{userName}' resolves to no tenant; " +
                             "refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[VisualisationRegistryParameterRoleResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new VisualisationRegistryParameterRoleService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the Role grants for a Visualisation Registry Parameter, visible to the caller's tenant.")]
        [ServiceOperation("VisualisationRegistryParameterRoleListByVisualisationRegistryParameterGuid",
            OperationKind.Read, Idempotent = true)]
        public async Task<List<VisualisationRegistryParameterRoleDto>> GetByVisualisationRegistryParameterGuidAsync(
            [Description("Guid of the Visualisation Registry Parameter to list role grants for.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryParameterRole",
                "ListByVisualisationRegistryParameterGuid", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug("VisualisationRegistryParameterRole.ListByVisualisationRegistryParameterGuid: entry " +
                          $"guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistryParameterRole.ListByVisualisationRegistryParameterGuid",
                    permissions);
                var dtos = VisualisationRegistryParameterRoleMapper.ToDto(
                    await repository.GetByVisualisationRegistryGuidAsync(guid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug("VisualisationRegistryParameterRole.ListByVisualisationRegistryParameterGuid: " +
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
                log.Error("VisualisationRegistryParameterRole.ListByVisualisationRegistryParameterGuid: " +
                          $"unexpected failure guid={guid} user={userName}", ex);
                throw;
            }
        }

        [Description("Grants a Role access to a Visualisation Registry Parameter.")]
        [ServiceOperation("VisualisationRegistryParameterRoleCreate", OperationKind.Write, Idempotent = false)]
        public async Task<VisualisationRegistryParameterRoleDto> InsertAsync(
            VisualisationRegistryParameterRoleDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryParameterRole", "Insert", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug("VisualisationRegistryParameterRole.Insert: entry user=" + userName +
                          $" visualisationRegistryParameterGuid={model?.VisualisationRegistryParameterGuid} " +
                          $"roleRegistryGuid={model?.RoleRegistryGuid}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("VisualisationRegistryParameterRole.Insert", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"VisualisationRegistryParameterRole.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(VisualisationRegistryParameterRoleMapper.ToPoco(model),
                    token).ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"VisualisationRegistryParameterRole.Insert: created Id={saved.Id} user={userName}");
                }

                return VisualisationRegistryParameterRoleMapper.ToDto(saved);
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
                log.Error($"VisualisationRegistryParameterRole.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Removes a Role's grant on a Visualisation Registry Parameter. The row must exist and be " +
                     "visible to the caller's tenant; unlike other areas in this system, a missing or invisible " +
                     "row surfaces as an unexpected failure here, not a distinguishable not-found result -- this " +
                     "preserves the original controller's own behaviour, and means a retry of this call is not " +
                     "safe to assume succeeded, unlike other Delete operations in this catalogue.")]
        [ServiceOperation("VisualisationRegistryParameterRoleDelete", OperationKind.Delete, Idempotent = false,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Visualisation Registry Parameter Role grant to remove.")]
            int id, CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryParameterRole", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryParameterRole.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistryParameterRole.Delete", permissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"VisualisationRegistryParameterRole.Delete: soft-deleted Id={id} user={userName}");
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
                log.Error($"VisualisationRegistryParameterRole.Delete: unexpected failure id={id} user={userName}",
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

            throw new ForbiddenException(strings[VisualisationRegistryParameterRoleResources.PermissionDenied],
                specs);
        }
    }
}