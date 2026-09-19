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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Query.CaseBySessionCaseSearchCompile;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.CaseBySessionCaseSearchCompile;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.CaseBySessionCaseSearchCompile
{
    public sealed class CaseBySessionCaseSearchCompileService
    {
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly CaseEventRepository caseEventRepository;
        private readonly CaseRepository caseRepository;
        private readonly global::Jube.Data.Query.CaseQuery.GetCaseBySessionCaseSearchCompileQuery query;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseBySessionCaseSearchCompileService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;

            var assertSelectOnlySetting = dynamicEnvironment.AppSettings("ParserAssertSelectOnly");
            var parserAssertSelectOnly = assertSelectOnlySetting == null ||
                                         assertSelectOnlySetting.Equals("True", StringComparison.OrdinalIgnoreCase);

            query = new global::Jube.Data.Query.CaseQuery.GetCaseBySessionCaseSearchCompileQuery(dbContext, userName,
                log, parserAssertSelectOnly,
                dynamicEnvironment.AppSettings("ReportConnectionString") ?? dbContext.Connection.ConnectionString);
            caseEventRepository = new CaseEventRepository(dbContext, userName);
            caseRepository = new CaseRepository(dbContext, userName);
        }

        public static Task<CaseBySessionCaseSearchCompileService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                dynamicEnvironment, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseBySessionCaseSearchCompileService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseBySessionCaseSearchCompileResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseBySessionCaseSearchCompile.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseBySessionCaseSearchCompileResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"CaseBySessionCaseSearchCompile.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseBySessionCaseSearchCompileResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseBySessionCaseSearchCompileService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dynamicEnvironment);
        }

        [Description("Retrieves the next case matching a previously compiled Session Case Search, identified by " +
                     "the compiled search's Guid. The next case is the first, in the search's own ordering, that " +
                     "is unlocked or already locked to the calling user and that the user's roles may see. " +
                     "SIDE EFFECTS: retrieving a case records a case-opened event against it and locks it to the " +
                     "calling user, and each execution is recorded against the compiled search. Fails as not found " +
                     "when the compiled search is unknown or has no remaining case.")]
        [ServiceOperation("CaseBySessionCaseSearchCompileGet", OperationKind.Write, Idempotent = false)]
        public async Task<CaseBySessionCaseSearchCompileDto> GetAsync(
            [Description("Guid of the compiled Session Case Search.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseBySessionCaseSearchCompile", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseBySessionCaseSearchCompile.Get: entry user={userName} guid={guid}");
            }

            try
            {
                EnsurePermitted("CaseBySessionCaseSearchCompile.Get");
                token.ThrowIfCancellationRequested();

                global::Jube.Data.Query.CaseQuery.Dto.CaseQueryDto? value;
                try
                {
                    value = await query.ExecuteAsync(guid, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new NotFoundException(strings[CaseBySessionCaseSearchCompileResources.NotFound], ex);
                }

                if (value is null)
                {
                    throw new InvalidOperationException(
                        $"Compiled search {guid} resolved a case that is not visible to user '{userName}'.");
                }

                var caseEvent = new CaseEvent
                {
                    CaseId = value.Id,
                    CaseEventTypeId = 2,
                    CaseKey = value.CaseKey,
                    CaseKeyValue = value.CaseKeyValue
                };

                await caseEventRepository.InsertAsync(caseEvent, token).ConfigureAwait(false);

                await caseRepository.LockToUserAsync(value.Id, token).ConfigureAwait(false);

                value.Locked = true;
                value.LockedUser = userName;

                op.Rows(1);
                return CaseBySessionCaseSearchCompileMapper.ToDto(value);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("not_found");
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
                log.Error($"CaseBySessionCaseSearchCompile.Get: unexpected failure user={userName}", ex);
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

            throw new ForbiddenException(strings[CaseBySessionCaseSearchCompileResources.PermissionDenied],
                permissions);
        }
    }
}