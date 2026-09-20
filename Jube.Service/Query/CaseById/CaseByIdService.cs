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
using Jube.Dto.Query.CaseById;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.CaseById;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.CaseById
{
    public sealed class CaseByIdService
    {
        private const int CaseViewedEventTypeId = 4;
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly CaseEventRepository caseEventRepository;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly global::Jube.Data.Query.CaseQuery.GetCaseByIdQuery query;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseByIdService(DbContext dbContext, string userName, int tenantRegistryId,
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
            query = new global::Jube.Data.Query.CaseQuery.GetCaseByIdQuery(dbContext, userName);
            caseEventRepository = new CaseEventRepository(dbContext, userName);
        }

        public static Task<CaseByIdService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseByIdService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseByIdResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseById.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseByIdResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseById.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseByIdResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseByIdService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Gets one case by its integer Id, including its workflow status colours, the payload " +
                     "fields configured for the workflow and the visible activations. Only cases in the caller's " +
                     "tenant, whose workflow and status are both granted to one of the caller's roles, are " +
                     "returned. Note that each successful call also records a 'case viewed' case event " +
                     "(event type 4) against the case, as the case screen always has.")]
        [ServiceOperation("CaseByIdGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseByIdDto> GetAsync(
            [Description("Integer Id of the case.")]
            int id, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseById", "Get", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseById.Get: entry user={userName} id={id}");
            }

            try
            {
                EnsurePermitted("CaseById.Get");

                var queryResults = await query.ExecuteAsync(id, token).ConfigureAwait(false);
                if (queryResults is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseById.Get: case {id} not found or not visible user={userName}");
                    }

                    throw new NotFoundException(strings[CaseByIdResources.NotFound]);
                }

                await caseEventRepository.InsertAsync(new CaseEvent
                {
                    CaseId = queryResults.Id,
                    CaseEventTypeId = CaseViewedEventTypeId,
                    CaseKey = queryResults.CaseKey,
                    CaseKeyValue = queryResults.CaseKeyValue
                }, token).ConfigureAwait(false);

                var dto = CaseByIdMapper.ToDto(queryResults);
                op.Rows(1);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseById.Get: found id={id} user={userName}");
                }

                return dto;
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
                log.Error($"CaseById.Get: unexpected failure user={userName}", ex);
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

            throw new ForbiddenException(strings[CaseByIdResources.PermissionDenied], permissions);
        }
    }
}