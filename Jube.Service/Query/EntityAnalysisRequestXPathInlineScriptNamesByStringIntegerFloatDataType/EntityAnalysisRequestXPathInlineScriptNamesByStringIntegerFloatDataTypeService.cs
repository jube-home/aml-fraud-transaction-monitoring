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
using Jube.Dto.Query.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType
{
    using DataQuery =
        global::Jube.Data.Query.GetEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeQuery;

    public sealed class EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeService
    {
        private static readonly int[] permissions = [11, 17, 4, 12];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly DataQuery query;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeService(DbContext dbContext,
            string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            query = new DataQuery(dbContext, userName);
        }

        public static Task<EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeService> CreateAsync(
            DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeService>
            CreateAsync(DbContext dbContext,
                string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
                IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings =
                stringLocalizerFactory.Create(
                    typeof(EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        "EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[
                    EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[
                    EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeService(dbContext,
                userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the String and Float typed fields of an Entity Analysis Model that can be used as " +
                     "search keys: request XPaths, Inline Script public properties and Inline Functions returning " +
                     "String, ordered by name. Read-only and scoped to the caller's tenant; a model belonging to " +
                     "another tenant yields an empty list.")]
        [ServiceOperation("EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeGet",
            OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeDto>> GetAsync(
            [Description("Identifier of the Entity Analysis Model whose fields are listed.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(
                "EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType", "Get", userName,
                tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType.Get: entry user={userName} entityAnalysisModelId={entityAnalysisModelId}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType.Get");

                var ownedByTenant = await dbContext.EntityAnalysisModel
                    .AnyAsync(w => w.Id == entityAnalysisModelId && w.TenantRegistryId == tenantRegistryId
                                                                 && (w.Deleted == 0 || w.Deleted == null), token)
                    .ConfigureAwait(false);

                if (!ownedByTenant)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType.Get: model {entityAnalysisModelId} not in tenant of user={userName}; returning empty.");
                    }

                    op.Rows(0);
                    return [];
                }

                var dtos = EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeMapper.ToDto(
                    await query.ExecuteAsync(entityAnalysisModelId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType.Get: {dtos.Count} rows user={userName}");
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
                log.Error(
                    $"EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType.Get: unexpected failure user={userName}",
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

            throw new ForbiddenException(
                strings[
                    EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeResources.PermissionDenied],
                permissions);
        }
    }
}