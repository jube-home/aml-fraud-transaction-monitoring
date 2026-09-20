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
using Jube.Dto.Query.Completions;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.Completions;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.Completions
{
    public sealed class CompletionsService
    {
        private const int WorkflowParseTypeId = 5;
        private const int ModelParseTypeId = 6;

        private static readonly int[] byCaseWorkflowIdPermissions = [25, 17, 20];
        private static readonly int[] includingDeletedPermissions = [1];
        private static readonly int[] byEntityAnalysisModelIdPermissions = [16];
        private static readonly int[] byParseTypePermissions = [8, 10, 13, 14, 17, 20, 26];

        private readonly ILog auditLog;
        private readonly CaseWorkflowRepository caseWorkflowRepository;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;

        private readonly global::Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery
            query;

        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CompletionsService(DbContext dbContext, string userName, int tenantRegistryId,
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
            query = new global::Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(
                dbContext, userName);
            caseWorkflowRepository = new CaseWorkflowRepository(dbContext, userName);
        }

        public static Task<CompletionsService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CompletionsService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CompletionsResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("Completions.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CompletionsResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Completions.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CompletionsResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CompletionsService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Gets the payload field completions (parse type 5) for the entity analysis model behind a " +
                     "case workflow in the caller's tenant, looked up by the workflow's integer Id. Deleted " +
                     "workflows are not found.")]
        [ServiceOperation("CompletionsGetByCaseWorkflowId", OperationKind.Read, Idempotent = true)]
        public Task<List<CompletionDto>> GetByCaseWorkflowIdAsync(
            [Description("Integer Id of the case workflow.")]
            int caseWorkflowId, CancellationToken token = default)
        {
            return RunAsync("GetByCaseWorkflowId", byCaseWorkflowIdPermissions, async () =>
            {
                var workflow = await caseWorkflowRepository.GetByIdAsync(caseWorkflowId, token).ConfigureAwait(false);
                return await ByWorkflowAsync(workflow, token).ConfigureAwait(false);
            }, token);
        }

        [Description("Gets the payload field completions (parse type 5) for the entity analysis model behind a " +
                     "case workflow in the caller's tenant, looked up by the workflow's Guid, including deleted " +
                     "workflows (but not those of deleted models).")]
        [ServiceOperation("CompletionsGetByCaseWorkflowGuidIncludingDeleted", OperationKind.Read, Idempotent = true)]
        public Task<List<CompletionDto>> GetByCaseWorkflowGuidIncludingDeletedAsync(
            [Description("Guid of the case workflow.")]
            Guid caseWorkflowGuid, CancellationToken token = default)
        {
            return RunAsync("GetByCaseWorkflowGuidIncludingDeleted", includingDeletedPermissions, async () =>
            {
                var workflow = await caseWorkflowRepository.GetByGuidIncludingDeletedAsync(caseWorkflowGuid, token)
                    .ConfigureAwait(false);
                return await ByWorkflowAsync(workflow, token).ConfigureAwait(false);
            }, token);
        }

        [Description("Gets the payload field completions (parse type 5) for the entity analysis model behind a " +
                     "case workflow in the caller's tenant, looked up by the workflow's integer Id, including " +
                     "deleted workflows.")]
        [ServiceOperation("CompletionsGetByCaseWorkflowIdIncludingDeleted", OperationKind.Read, Idempotent = true)]
        public Task<List<CompletionDto>> GetByCaseWorkflowIdIncludingDeletedAsync(
            [Description("Integer Id of the case workflow.")]
            int caseWorkflowId, CancellationToken token = default)
        {
            return RunAsync("GetByCaseWorkflowIdIncludingDeleted", includingDeletedPermissions, async () =>
            {
                var workflow = await caseWorkflowRepository.GetByIdIncludingDeletedAsync(caseWorkflowId, token)
                    .ConfigureAwait(false);
                return await ByWorkflowAsync(workflow, token).ConfigureAwait(false);
            }, token);
        }

        [Description("Gets the payload field completions (parse type 6, reporting) for an entity analysis model " +
                     "in the caller's tenant.")]
        [ServiceOperation("CompletionsGetByEntityAnalysisModelId", OperationKind.Read, Idempotent = true)]
        public Task<List<CompletionDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Integer Id of the entity analysis model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            return RunAsync("GetByEntityAnalysisModelId", byEntityAnalysisModelIdPermissions,
                () => CompletionsAsync(entityAnalysisModelId, ModelParseTypeId, true, token), token);
        }

        [Description("Gets the payload field completions for an entity analysis model in the caller's tenant " +
                     "and a given parse type Id (a parse type of zero or less yields no completions). The " +
                     "non-reporting variant is used.")]
        [ServiceOperation("CompletionsGetByEntityAnalysisModelIdParseTypeId", OperationKind.Read, Idempotent = true)]
        public Task<List<CompletionDto>> GetByEntityAnalysisModelIdParseTypeIdAsync(
            [Description("Integer Id of the entity analysis model.")]
            int entityAnalysisModelId,
            [Description("Integer Id of the parse type.")]
            int parseTypeId, CancellationToken token = default)
        {
            return RunAsync("GetByEntityAnalysisModelIdParseTypeId", byParseTypePermissions,
                () => CompletionsAsync(entityAnalysisModelId, parseTypeId, false, token), token);
        }

        private Task<List<CompletionDto>> ByWorkflowAsync(Data.Poco.CaseWorkflow? workflow,
            CancellationToken token)
        {
            if (workflow is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Completions: case workflow not found or not visible user={userName}");
                }

                throw new CaseWorkflowNotFoundException(strings[CompletionsResources.CaseWorkflowNotFound]);
            }

            if (workflow.EntityAnalysisModelId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Completions: case workflow {workflow.Id} has no model user={userName}");
                }

                throw new NotFoundException(strings[CompletionsResources.NotFound]);
            }

            return CompletionsAsync(workflow.EntityAnalysisModelId.Value, WorkflowParseTypeId, true, token);
        }

        private async Task<List<CompletionDto>> CompletionsAsync(int entityAnalysisModelId, int parseTypeId,
            bool reporting, CancellationToken token)
        {
            var fields = await query.ExecuteAsync(entityAnalysisModelId, parseTypeId, reporting, token)
                .ConfigureAwait(false);
            return CompletionsMapper.ToDtos(fields);
        }

        private async Task<List<CompletionDto>> RunAsync(string operation, int[] permissions,
            Func<Task<List<CompletionDto>>> body, CancellationToken token)
        {
            using var op = OperationScope.Start("Completions", operation, userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"Completions.{operation}: entry user={userName}");
            }

            try
            {
                token.ThrowIfCancellationRequested();
                EnsurePermitted($"Completions.{operation}", permissions);

                var result = await body().ConfigureAwait(false);
                op.Rows(result.Count);
                return result;
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
            catch (CaseWorkflowNotFoundException)
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
                log.Error($"Completions.{operation}: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(string op, int[] permissions)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[CompletionsResources.PermissionDenied], permissions);
        }
    }
}