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
using Jube.Dto.Repository.CaseWorkflowXPathRole;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.CaseWorkflowXPathRole;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseWorkflowXPathRole;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.CaseWorkflowXPathRoleRepository;

namespace Jube.Service.Repository.CaseWorkflowXPathRole
{
    public sealed class CaseWorkflowXPathRoleService
    {
        private static readonly int[] permissions = [20];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseWorkflowXPathRoleDtoValidator validator;

        private CaseWorkflowXPathRoleService(DbContext dbContext, string userName, int tenantRegistryId,
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
            validator = new CaseWorkflowXPathRoleDtoValidator(repository, strings);
        }

        public static Task<CaseWorkflowXPathRoleService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowXPathRoleService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowXPathRoleResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflowXPathRole.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowXPathRoleResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowXPathRole.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowXPathRoleResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowXPathRoleService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the Role grants for a Case Workflow XPath, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowXPathRoleListByCaseWorkflowXPathGuid", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowXPathRoleDto>> GetByCaseWorkflowXPathGuidAsync(
            [Description("Guid of the Case Workflow XPath to list role grants for.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowXPathRole", "ListByCaseWorkflowXPathGuid", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowXPathRole.ListByCaseWorkflowXPathGuid: entry guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowXPathRole.ListByCaseWorkflowXPathGuid", permissions);
                var dtos = CaseWorkflowXPathRoleMapper.ToDto(
                    await repository.GetByCaseWorkflowXPathGuidAsync(guid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowXPathRole.ListByCaseWorkflowXPathGuid: {dtos.Count} rows guid={guid} " +
                              $"user={userName}");
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
                log.Error($"CaseWorkflowXPathRole.ListByCaseWorkflowXPathGuid: unexpected failure guid={guid} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Grants a Role access to a Case Workflow XPath.")]
        [ServiceOperation("CaseWorkflowXPathRoleCreate", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowXPathRoleDto> InsertAsync(CaseWorkflowXPathRoleDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowXPathRole", "Insert", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowXPathRole.Insert: entry user={userName} " +
                          $"caseWorkflowXPathGuid={model?.CaseWorkflowXPathGuid} " +
                          $"roleRegistryGuid={model?.RoleRegistryGuid}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowXPathRole.Insert", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowXPathRole.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(CaseWorkflowXPathRoleMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowXPathRole.Insert: created Id={saved.Id} user={userName}");
                }

                return CaseWorkflowXPathRoleMapper.ToDto(saved);
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
                log.Error($"CaseWorkflowXPathRole.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Removes a Role's grant on a Case Workflow XPath. The row must exist and be visible to " +
                     "the caller's tenant; unlike other areas in this system, a missing or invisible row surfaces " +
                     "as an unexpected failure here, not a distinguishable not-found result -- this preserves the " +
                     "original controller's own behaviour, and means a retry of this call is not safe to assume " +
                     "succeeded, unlike other Delete operations in this catalogue.")]
        [ServiceOperation("CaseWorkflowXPathRoleDelete", OperationKind.Delete, Idempotent = false, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Case Workflow XPath Role grant to remove.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowXPathRole", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowXPathRole.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowXPathRole.Delete", permissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowXPathRole.Delete: soft-deleted Id={id} user={userName}");
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
                log.Error($"CaseWorkflowXPathRole.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[CaseWorkflowXPathRoleResources.PermissionDenied], specs);
        }
    }
}