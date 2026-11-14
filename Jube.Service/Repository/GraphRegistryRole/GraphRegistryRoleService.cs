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
using Jube.Dto.Repository.GraphRegistryRole;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Repository.GraphRegistryRole;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.GraphRegistryRole;
using log4net;
using Microsoft.Extensions.Localization;
using RoleRepository = Jube.Data.Repository.GraphRegistryRoleRepository;

namespace Jube.Service.Repository.GraphRegistryRole
{
    public sealed class GraphRegistryRoleService
    {
        private static readonly int[] permissions = [50];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RoleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly GraphRegistryRoleDtoValidator validator;

        private GraphRegistryRoleService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new RoleRepository(dbContext, userName);
            validator = new GraphRegistryRoleDtoValidator(repository, strings);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<GraphRegistryRoleService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<GraphRegistryRoleService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(GraphRegistryRoleResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("GraphRegistryRole.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[GraphRegistryRoleResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GraphRegistryRole.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[GraphRegistryRoleResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new GraphRegistryRoleService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the Role grants for a Graph Registry, visible to the caller's tenant.")]
        [ServiceOperation("GraphRegistryRoleListByGraphRegistryGuid", OperationKind.Read, Idempotent = true)]
        public async Task<List<GraphRegistryRoleDto>> GetByGraphRegistryGuidAsync(
            [Description("Guid of the Graph Registry to list role grants for.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryRole", "ListByGraphRegistryGuid", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistryRole.ListByGraphRegistryGuid", permissions);
                var dtos = GraphRegistryRoleMapper.ToDto(
                    await repository.GetByGraphRegistryGuidAsync(guid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);

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
                log.Error($"GraphRegistryRole.ListByGraphRegistryGuid: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Grants a Role access to a Graph Registry: the role sees it in the directory and on cases, and " +
                     "sees its restricted edges.")]
        [ServiceOperation("GraphRegistryRoleCreate", OperationKind.Write, Idempotent = false)]
        public async Task<GraphRegistryRoleDto> InsertAsync(GraphRegistryRoleDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryRole", "Insert", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("GraphRegistryRole.Insert", permissions);
                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"GraphRegistryRole.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(GraphRegistryRoleMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"GraphRegistryRole.Insert: created Id={saved.Id} user={userName}");
                }

                return GraphRegistryRoleMapper.ToDto(saved);
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
                log.Error($"GraphRegistryRole.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Removes a Role's grant on a Graph Registry. The row must exist and be visible to the caller's " +
                     "tenant; a missing row surfaces as an unexpected failure, as the Visualisation Registry Role " +
                     "operation does, so a retry is not safe to assume succeeded.")]
        [ServiceOperation("GraphRegistryRoleDelete", OperationKind.Delete, Idempotent = false, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Graph Registry Role grant to remove.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryRole", "Delete", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistryRole.Delete", permissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"GraphRegistryRole.Delete: soft-deleted Id={id} user={userName}");
                }
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (KeyNotFoundException)
            {
                op.Outcome("not_found");

                if (log.IsWarnEnabled)
                {
                    log.Warn($"GraphRegistryRole.Delete: id={id} not found or not visible; nothing to remove user={userName}");
                }
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"GraphRegistryRole.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[GraphRegistryRoleResources.PermissionDenied], specs);
        }
    }
}
