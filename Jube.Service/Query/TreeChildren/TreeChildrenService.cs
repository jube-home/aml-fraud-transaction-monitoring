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
using Jube.Dto.Query.TreeChildren;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.TreeChildren;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.TreeChildren
{
    public sealed class TreeChildrenService
    {
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private TreeChildrenService(DbContext dbContext, string userName, int tenantRegistryId,
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
        }

        public static Task<TreeChildrenService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<TreeChildrenService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(TreeChildrenResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("TreeChildren.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[TreeChildrenResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"TreeChildren.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[TreeChildrenResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new TreeChildrenService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        private async Task<List<TDto>> RunAsync<TRow, TDto>(TreeChildrenNode node,
            Func<CancellationToken, Task<IEnumerable<TRow>>> load, Func<TRow, TDto> map, CancellationToken token)
        {
            using var op = OperationScope.Start("TreeChildren", node.Route, userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"TreeChildren.{node.Route}: entry user={userName}");
            }

            try
            {
                EnsurePermitted(node);
                token.ThrowIfCancellationRequested();

                var dtos = (await load(token).ConfigureAwait(false)).Select(map).ToList();
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"TreeChildren.{node.Route}: {dtos.Count} rows user={userName}");
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
                log.Error($"TreeChildren.{node.Route}: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(TreeChildrenNode node)
        {
            if (permissionValidation.Validate(node.Permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn(
                    $"TreeChildren.{node.Route}: permission denied user={userName} specs=[{string.Join(",", node.Permissions)}]");
            }

            throw new ForbiddenException(strings[TreeChildrenResources.PermissionDenied], node.Permissions);
        }

        [Description(
            "Lists the request XPath nodes of an Entity Analysis Model as tree children, ordered by Id. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenRequestXPathGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetRequestXPathAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.RequestXPath,
                t => new EntityAnalysisModelRequestXPathRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByIdAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the datasource nodes of a Visualisation Registry as tree children, ordered by priority. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenVisualisationRegistryDatasourceGet", OperationKind.Read, Idempotent = true)]
        public Task<List<VisualisationRegistryTreeChildDto>> GetVisualisationRegistryDatasourceAsync(
            [Description("Id of the parent Visualisation Registry.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.VisualisationRegistryDatasource,
                t => new VisualisationRegistryDatasourceRepository(dbContext, userName)
                    .GetByVisualisationRegistryIdOrderByPriorityAsync(id, t),
                e => TreeChildrenMapper.ToVisualisationRegistryChild(e.Id, e.Name, e.Active, e.VisualisationRegistryId),
                token);
        }

        [Description(
            "Lists the users of a Role Registry as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenUserRegistryGet", OperationKind.Read, Idempotent = true)]
        public Task<List<RoleRegistryTreeChildDto>> GetUserRegistryAsync(
            [Description("Guid of the parent Role Registry.")]
            Guid guid = default, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.UserRegistry,
                t => new UserRegistryRepository(dbContext, userName).GetByRoleRegistryGuidAsync(guid, t),
                e => TreeChildrenMapper.ToRoleRegistryChild(e.Id, e.Name, e.Active, e.RoleRegistryGuid), token);
        }

        [Description("Lists the permission nodes granted to a Role Registry as tree children; the name is the " +
                     "permission specification name. Only roles in the caller's tenant are returned; Colour is " +
                     "'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenRoleRegistryPermissionGet", OperationKind.Read, Idempotent = true)]
        public Task<List<RoleRegistryTreeChildDto>> GetRoleRegistryPermissionAsync(
            [Description("Id of the parent Role Registry.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.RoleRegistryPermission,
                t => new global::Jube.Data.Query.GetRoleRegistryPermissionByRoleRegistryGuidQuery(dbContext, userName)
                    .ExecuteAsync(id, t),
                e => TreeChildrenMapper.ToRoleRegistryChild(e.Id, e.Name, e.Active, e.RoleRegistryGuid), token);
        }

        [Description(
            "Lists the parameter nodes of a Visualisation Registry as tree children, ordered by Id. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenVisualisationRegistryParameterGet", OperationKind.Read, Idempotent = true)]
        public Task<List<VisualisationRegistryTreeChildDto>> GetVisualisationRegistryParameterAsync(
            [Description("Id of the parent Visualisation Registry.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.VisualisationRegistryParameter,
                t => new VisualisationRegistryParameterRepository(dbContext, userName)
                    .GetByVisualisationRegistryIdOrderByIdAsync(id, t),
                e => TreeChildrenMapper.ToVisualisationRegistryChild(e.Id, e.Name, e.Active, e.VisualisationRegistryId),
                token);
        }

        [Description(
            "Lists the inline function nodes of an Entity Analysis Model as tree children, ordered by Id. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenInlineFunctionGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetInlineFunctionAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.InlineFunction,
                t => new EntityAnalysisModelInlineFunctionRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByIdAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the tag nodes of an Entity Analysis Model as tree children, ordered by Id. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenTagGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetTagAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.Tag,
                t => new EntityAnalysisModelTagRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByIdAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the gateway rule nodes of an Entity Analysis Model as tree children, ordered by priority. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenGatewayRuleGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetGatewayRuleAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.GatewayRule,
                t => new EntityAnalysisModelGatewayRuleRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByPriorityAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the exhaustive search instance nodes of an Entity Analysis Model as tree children, ordered by Id. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenExhaustiveGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetExhaustiveAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.Exhaustive,
                t => new ExhaustiveSearchInstanceRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByIdAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the reprocessing rule nodes of an Entity Analysis Model as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenReprocessingGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetReprocessingAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.Reprocessing,
                t => new EntityAnalysisModelReprocessingRuleRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the HTTP adaptation nodes of an Entity Analysis Model as tree children, ordered by Id. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenAdaptationGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetAdaptationAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.Adaptation,
                t => new EntityAnalysisModelHttpAdaptationRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByIdAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the case workflow nodes of an Entity Analysis Model as tree children, ordered by Id. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenCaseWorkflowGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetCaseWorkflowAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.CaseWorkflow,
                t => new CaseWorkflowRepository(dbContext, userName).GetByEntityAnalysisModelIdOrderByIdAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the abstraction calculation nodes of an Entity Analysis Model as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenAbstractionCalculationGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetAbstractionCalculationAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.AbstractionCalculation,
                t => new EntityAnalysisModelAbstractionCalculationRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByIdDescAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the abstraction rule nodes of an Entity Analysis Model as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenAbstractionRuleGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetAbstractionRuleAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.AbstractionRule,
                t => new EntityAnalysisModelAbstractionRuleRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByIdDescAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the activation rule nodes of an Entity Analysis Model as tree children, ordered by priority. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenActivationRuleGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetActivationRuleAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.ActivationRule,
                t => new EntityAnalysisModelActivationRuleRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByPriorityDescAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the TTL counter nodes of an Entity Analysis Model as tree children, ordered by Id. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenTTLCounterGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetTtlCounterAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.TtlCounter,
                t => new EntityAnalysisModelTtlCounterRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByIdAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the inline script nodes of an Entity Analysis Model as tree children, ordered by Id. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenInlineScriptGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetInlineScriptAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.InlineScript,
                t => new EntityAnalysisModelInlineScriptRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByIdAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the sanction nodes of an Entity Analysis Model as tree children, ordered by Id. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenSanctionsGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetSanctionsAsync(
            [Description("Id of the parent Entity Analysis Model.")]
            int id = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.Sanctions,
                t => new EntityAnalysisModelSanctionRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByIdAsync(id, t),
                e => TreeChildrenMapper.ToModelChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelId), token);
        }

        [Description(
            "Lists the list nodes of an Entity Analysis Model, addressed by model Guid, as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenListGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetListAsync(
            [Description("Guid of the parent Entity Analysis Model.")]
            Guid guid = default, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.List,
                t => new EntityAnalysisModelListRepository(dbContext, userName).GetByEntityAnalysisModelGuidAsync(guid,
                    t),
                e => TreeChildrenMapper.ToModelGuidChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelGuid), token);
        }

        [Description(
            "Lists the dictionary nodes of an Entity Analysis Model, addressed by model Guid, as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenDictionaryGet", OperationKind.Read, Idempotent = true)]
        public Task<List<EntityAnalysisModelTreeChildDto>> GetDictionaryAsync(
            [Description("Guid of the parent Entity Analysis Model.")]
            Guid guid = default, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.Dictionary,
                t => new EntityAnalysisModelDictionaryRepository(dbContext, userName)
                    .GetByEntityAnalysisModelGuidAsync(guid, t),
                e => TreeChildrenMapper.ToModelGuidChild(e.Id, e.Name, e.Active, e.EntityAnalysisModelGuid), token);
        }

        [Description(
            "Lists the xpath nodes of a Case Workflow as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenCaseWorkflowXPathGet", OperationKind.Read, Idempotent = true)]
        public Task<List<CasesWorkflowTreeChildDto>> GetCaseWorkflowXPathAsync(
            [Description("Id of the parent Case Workflow.")]
            int key = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.CaseWorkflowXPath,
                t => new CaseWorkflowXPathRepository(dbContext, userName)
                    .GetByCasesWorkflowIdOrderByIdDescAsync(key, t),
                e => TreeChildrenMapper.ToCasesWorkflowChild(e.Id, e.Name, e.Active, e.CaseWorkflowId), token);
        }

        [Description(
            "Lists the form nodes of a Case Workflow as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenCaseWorkflowFormGet", OperationKind.Read, Idempotent = true)]
        public Task<List<CasesWorkflowTreeChildDto>> GetCaseWorkflowFormAsync(
            [Description("Id of the parent Case Workflow.")]
            int key = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.CaseWorkflowForm,
                t => new CaseWorkflowFormRepository(dbContext, userName).GetByCasesWorkflowIdOrderByIdAsync(key, t),
                e => TreeChildrenMapper.ToCasesWorkflowChild(e.Id, e.Name, e.Active, e.CaseWorkflowId), token);
        }

        [Description(
            "Lists the action nodes of a Case Workflow as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenCaseWorkflowActionGet", OperationKind.Read, Idempotent = true)]
        public Task<List<CasesWorkflowTreeChildDto>> GetCaseWorkflowActionAsync(
            [Description("Id of the parent Case Workflow.")]
            int key = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.CaseWorkflowAction,
                t => new CaseWorkflowActionRepository(dbContext, userName).GetByCasesWorkflowIdOrderByIdAsync(key, t),
                e => TreeChildrenMapper.ToCasesWorkflowChild(e.Id, e.Name, e.Active, e.CaseWorkflowId), token);
        }

        [Description(
            "Lists the macro nodes of a Case Workflow as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenCaseWorkflowMacroGet", OperationKind.Read, Idempotent = true)]
        public Task<List<CasesWorkflowTreeChildDto>> GetCaseWorkflowMacroAsync(
            [Description("Id of the parent Case Workflow.")]
            int key = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.CaseWorkflowMacro,
                t => new CaseWorkflowMacroRepository(dbContext, userName).GetByCasesWorkflowIdOrderByIdAsync(key, t),
                e => TreeChildrenMapper.ToCasesWorkflowChild(e.Id, e.Name, e.Active, e.CaseWorkflowId), token);
        }

        [Description(
            "Lists the filter nodes of a Case Workflow as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenCaseWorkflowFilterGet", OperationKind.Read, Idempotent = true)]
        public Task<List<CasesWorkflowTreeChildDto>> GetCaseWorkflowFilterAsync(
            [Description("Id of the parent Case Workflow.")]
            int key = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.CaseWorkflowFilter,
                t => new CaseWorkflowFilterRepository(dbContext, userName).GetByCasesWorkflowIdOrderByIdAsync(key, t),
                e => TreeChildrenMapper.ToCasesWorkflowChild(e.Id, e.Name, e.Active, e.CaseWorkflowId), token);
        }

        [Description(
            "Lists the display nodes of a Case Workflow as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenCaseWorkflowDisplayGet", OperationKind.Read, Idempotent = true)]
        public Task<List<CasesWorkflowTreeChildDto>> GetCaseWorkflowDisplayAsync(
            [Description("Id of the parent Case Workflow.")]
            int key = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.CaseWorkflowDisplay,
                t => new CaseWorkflowDisplayRepository(dbContext, userName).GetByCasesWorkflowIdOrderByIdAsync(key, t),
                e => TreeChildrenMapper.ToCasesWorkflowChild(e.Id, e.Name, e.Active, e.CaseWorkflowId), token);
        }

        [Description(
            "Lists the status nodes of a Case Workflow as tree children. Only rows in the caller's tenant are returned; Colour is 'green' for active rows, otherwise 'red'.")]
        [ServiceOperation("TreeChildrenCaseWorkflowStatusGet", OperationKind.Read, Idempotent = true)]
        public Task<List<CasesWorkflowTreeChildDto>> GetCaseWorkflowStatusAsync(
            [Description("Id of the parent Case Workflow.")]
            int key = 0, CancellationToken token = default)
        {
            return RunAsync(TreeChildrenNodes.CaseWorkflowStatus,
                t => new CaseWorkflowStatusRepository(dbContext, userName).GetByCasesWorkflowIdOrderByPriorityAsync(key,
                    t),
                e => TreeChildrenMapper.ToCasesWorkflowChild(e.Id, e.Name, e.Active, e.CaseWorkflowId), token);
        }
    }
}