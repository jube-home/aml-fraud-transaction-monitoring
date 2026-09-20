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
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisPotentialMultiPartStringNames;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.EntityAnalysisPotentialMultiPartStringNames
{
    public sealed class EntityAnalysisPotentialMultiPartStringNamesService
    {
        private static readonly int[] permissions = [11, 17, 4, 1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly global::Jube.Data.Query.GetEntityAnalysisPotentialMultiPartStringNamesQuery query;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisPotentialMultiPartStringNamesService(DbContext dbContext, string userName,
            int tenantRegistryId,
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
            query = new global::Jube.Data.Query.GetEntityAnalysisPotentialMultiPartStringNamesQuery(dbContext,
                userName);
        }

        public static Task<EntityAnalysisPotentialMultiPartStringNamesService> CreateAsync(DbContext dbContext,
            string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisPotentialMultiPartStringNamesService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisPotentialMultiPartStringNamesResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisPotentialMultiPartStringNames.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisPotentialMultiPartStringNamesResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisPotentialMultiPartStringNames.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisPotentialMultiPartStringNamesResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisPotentialMultiPartStringNamesService(dbContext, userName,
                resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists, alphabetically, the names of the fields of an entity analysis model that can hold a " +
                     "multi-part string: non-deleted request XPaths of string data type together with non-deleted " +
                     "inline functions returning a string. Selected by the integer Id of the model, which must " +
                     "belong to the caller's tenant; otherwise the result is empty. Read-only.")]
        [ServiceOperation("EntityAnalysisPotentialMultiPartStringNamesGetById", OperationKind.Read, Idempotent = true)]
        public Task<List<string>> GetByIdAsync(
            [Description("Integer Id of the entity analysis model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            return RunAsync("GetById", $"entityAnalysisModelId={entityAnalysisModelId}",
                () => query.ExecuteAsync(entityAnalysisModelId, token), token);
        }

        [Description("Lists, alphabetically, the names of the fields of an entity analysis model that can hold a " +
                     "multi-part string: non-deleted request XPaths of string data type together with non-deleted " +
                     "inline functions returning a string. Selected by the Guid of the model, which must " +
                     "belong to the caller's tenant; otherwise the result is empty. Read-only.")]
        [ServiceOperation("EntityAnalysisPotentialMultiPartStringNamesGetByGuid", OperationKind.Read,
            Idempotent = true)]
        public Task<List<string>> GetByGuidAsync(
            [Description("Guid of the entity analysis model.")]
            Guid entityAnalysisModelGuid,
            CancellationToken token = default)
        {
            return RunAsync("GetByGuid", $"entityAnalysisModelGuid={entityAnalysisModelGuid}",
                () => query.ExecuteAsync(entityAnalysisModelGuid, token), token);
        }

        private async Task<List<string>> RunAsync(string operation, string arguments,
            Func<Task<IEnumerable<string>>> execute, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            using var op = OperationScope.Start("EntityAnalysisPotentialMultiPartStringNames", operation, userName,
                tenantRegistryId,
                auditLog, log, serviceChangeBus);
            var name = $"EntityAnalysisPotentialMultiPartStringNames.{operation}";
            if (log.IsDebugEnabled)
            {
                log.Debug($"{name}: entry user={userName} {arguments}");
            }

            try
            {
                EnsurePermitted(name);
                var names = (await execute().ConfigureAwait(false)).ToList();
                op.Rows(names.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"{name}: {names.Count} rows user={userName}");
                }

                return names;
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
                log.Error($"{name}: unexpected failure user={userName}", ex);
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

            throw new ForbiddenException(strings[EntityAnalysisPotentialMultiPartStringNamesResources.PermissionDenied],
                permissions);
        }
    }
}