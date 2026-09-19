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

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Service.Exceptions.Query.TreeChildren;
using Jube.Service.Query.TreeChildren;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class TreeChildrenEndpoints
    {
        private const string Base = "/api/TreeChildren";

        public static void MapTreeChildrenEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("TreeChildren");

            MapById(group, TreeChildrenNodes.RequestXPath,
                (service, key, token) => service.GetRequestXPathAsync(key, token));
            MapById(group, TreeChildrenNodes.VisualisationRegistryDatasource,
                (service, key, token) => service.GetVisualisationRegistryDatasourceAsync(key, token));
            MapByGuid(group, TreeChildrenNodes.UserRegistry,
                (service, key, token) => service.GetUserRegistryAsync(key, token));
            MapById(group, TreeChildrenNodes.RoleRegistryPermission,
                (service, key, token) => service.GetRoleRegistryPermissionAsync(key, token));
            MapById(group, TreeChildrenNodes.VisualisationRegistryParameter,
                (service, key, token) => service.GetVisualisationRegistryParameterAsync(key, token));
            MapById(group, TreeChildrenNodes.InlineFunction,
                (service, key, token) => service.GetInlineFunctionAsync(key, token));
            MapById(group, TreeChildrenNodes.Tag,
                (service, key, token) => service.GetTagAsync(key, token));
            MapById(group, TreeChildrenNodes.GatewayRule,
                (service, key, token) => service.GetGatewayRuleAsync(key, token));
            MapById(group, TreeChildrenNodes.Exhaustive,
                (service, key, token) => service.GetExhaustiveAsync(key, token));
            MapById(group, TreeChildrenNodes.Reprocessing,
                (service, key, token) => service.GetReprocessingAsync(key, token));
            MapById(group, TreeChildrenNodes.Adaptation,
                (service, key, token) => service.GetAdaptationAsync(key, token));
            MapById(group, TreeChildrenNodes.CaseWorkflow,
                (service, key, token) => service.GetCaseWorkflowAsync(key, token));
            MapById(group, TreeChildrenNodes.AbstractionCalculation,
                (service, key, token) => service.GetAbstractionCalculationAsync(key, token));
            MapById(group, TreeChildrenNodes.AbstractionRule,
                (service, key, token) => service.GetAbstractionRuleAsync(key, token));
            MapById(group, TreeChildrenNodes.ActivationRule,
                (service, key, token) => service.GetActivationRuleAsync(key, token));
            MapById(group, TreeChildrenNodes.TtlCounter,
                (service, key, token) => service.GetTtlCounterAsync(key, token));
            MapById(group, TreeChildrenNodes.InlineScript,
                (service, key, token) => service.GetInlineScriptAsync(key, token));
            MapById(group, TreeChildrenNodes.Sanctions,
                (service, key, token) => service.GetSanctionsAsync(key, token));
            MapByGuid(group, TreeChildrenNodes.List,
                (service, key, token) => service.GetListAsync(key, token));
            MapByGuid(group, TreeChildrenNodes.Dictionary,
                (service, key, token) => service.GetDictionaryAsync(key, token));
            MapByKey(group, TreeChildrenNodes.CaseWorkflowXPath,
                (service, key, token) => service.GetCaseWorkflowXPathAsync(key, token));
            MapByKey(group, TreeChildrenNodes.CaseWorkflowForm,
                (service, key, token) => service.GetCaseWorkflowFormAsync(key, token));
            MapByKey(group, TreeChildrenNodes.CaseWorkflowAction,
                (service, key, token) => service.GetCaseWorkflowActionAsync(key, token));
            MapByKey(group, TreeChildrenNodes.CaseWorkflowMacro,
                (service, key, token) => service.GetCaseWorkflowMacroAsync(key, token));
            MapByKey(group, TreeChildrenNodes.CaseWorkflowFilter,
                (service, key, token) => service.GetCaseWorkflowFilterAsync(key, token));
            MapByKey(group, TreeChildrenNodes.CaseWorkflowDisplay,
                (service, key, token) => service.GetCaseWorkflowDisplayAsync(key, token));
            MapByKey(group, TreeChildrenNodes.CaseWorkflowStatus,
                (service, key, token) => service.GetCaseWorkflowStatusAsync(key, token));
        }

        private static void MapById<T>(RouteGroupBuilder group, TreeChildrenNode node,
            Func<TreeChildrenService, int, CancellationToken, Task<List<T>>> call)
        {
            group.MapGet(node.Route, (HttpContext httpContext, ILog log,
                        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                        IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
                        CancellationToken token, int id = default) =>
                    HandleAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                        node, (service, t) => call(service, id, t), token))
                .Produces<List<T>>()
                .WithName(node.ToolName);
        }

        private static void MapByGuid<T>(RouteGroupBuilder group, TreeChildrenNode node,
            Func<TreeChildrenService, Guid, CancellationToken, Task<List<T>>> call)
        {
            group.MapGet(node.Route, (HttpContext httpContext, ILog log,
                        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                        IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
                        CancellationToken token, Guid? guid = null) =>
                    HandleAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                        node, (service, t) => call(service, guid ?? Guid.Empty, t), token))
                .Produces<List<T>>()
                .WithName(node.ToolName);
        }

        private static void MapByKey<T>(RouteGroupBuilder group, TreeChildrenNode node,
            Func<TreeChildrenService, int, CancellationToken, Task<List<T>>> call)
        {
            group.MapGet(node.Route, (HttpContext httpContext, ILog log,
                        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                        IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
                        CancellationToken token, int key = default) =>
                    HandleAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                        node, (service, t) => call(service, key, t), token))
                .Produces<List<T>>()
                .WithName(node.ToolName);
        }

        private static async Task<IResult> HandleAsync<T>(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, TreeChildrenNode node,
            Func<TreeChildrenService, CancellationToken, Task<List<T>>> run, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            var route = $"GET {Base}/{node.Route}";
            if (log.IsDebugEnabled)
            {
                log.Debug($"{route}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await TreeChildrenService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                return TypedResults.Ok(await run(service, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{route}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{route}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"{route}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"{route}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}