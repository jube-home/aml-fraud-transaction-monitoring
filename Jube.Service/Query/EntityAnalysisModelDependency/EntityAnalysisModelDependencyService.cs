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
using Jube.Dto.Query.EntityAnalysisModelDependency;
using Jube.Dto.Validation;
using Jube.Parser.Dependency;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelDependency;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.EntityAnalysisModelDependency
{
    public sealed class EntityAnalysisModelDependencyService
    {
        private const int MaxDepth = 10;
        private const int MaxDependents = 1000;

        private static readonly HashSet<ModelEntityKind> deleteGuarded =
        [
            ModelEntityKind.RequestXPath, ModelEntityKind.InlineScriptProperty, ModelEntityKind.InlineFunction,
            ModelEntityKind.AbstractionRule, ModelEntityKind.AbstractionCalculation, ModelEntityKind.ActivationRule,
            ModelEntityKind.TtlCounter, ModelEntityKind.Sanction, ModelEntityKind.List, ModelEntityKind.Dictionary,
            ModelEntityKind.HttpAdaptation, ModelEntityKind.ExhaustiveAdaptation, ModelEntityKind.CaseWorkflow
        ];

        private static readonly ModelEntityKind[] unreferencedKinds =
        [
            ModelEntityKind.List, ModelEntityKind.Dictionary, ModelEntityKind.TtlCounter,
            ModelEntityKind.AbstractionRule, ModelEntityKind.AbstractionCalculation, ModelEntityKind.InlineFunction,
            ModelEntityKind.HttpAdaptation, ModelEntityKind.ExhaustiveAdaptation
        ];

        private static readonly int[] permissions = [8, 10, 13, 14, 16, 17, 25, 26];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisModelDependencyService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log,
            ILog auditLog, IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
        }

        public static Task<EntityAnalysisModelDependencyService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelDependencyService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelDependencyResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelDependency.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelDependencyResources
                    .NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelDependency.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelDependencyResources
                    .NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelDependencyService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Answers what would stop working if an entity of a model were deleted, deactivated or " +
                     "renamed: every entity that uses it, found from the names in rule text and from settings " +
                     "such as search keys, TTL counter data names and case workflows, directly and through other " +
                     "entities (an activation rule that uses an abstraction rule that uses the entity). " +
                     "DeleteBlocked says whether deleting it would be refused. Nothing is changed.")]
        [ServiceOperation("EntityAnalysisModelDependencyDependents", OperationKind.Read, Idempotent = true)]
        public Task<DependencyImpactDto> DependentsAsync(
            [Description("The model the entity belongs to.")]
            int entityAnalysisModelId,
            [Description("The kind of entity: RequestXPath, InlineScriptProperty, InlineFunction, GatewayRule, " +
                         "AbstractionRule, AbstractionCalculation, ActivationRule, TtlCounter, Sanction, List, " +
                         "Dictionary, HttpAdaptation, ExhaustiveAdaptation, ReprocessingRule, InlineScript or " +
                         "CaseWorkflow.")]
            string? kind,
            [Description("The entity's id.")] int id,
            CancellationToken token = default)
        {
            return RunAsync("Dependents", async () =>
            {
                var result = new DependencyImpactDto();
                var (graph, entity, error) = await ResolveAsync(entityAnalysisModelId, kind, id, token)
                    .ConfigureAwait(false);
                if (error != null)
                {
                    result.Errors = [error];
                    return result;
                }

                result.Entity = ToDto(entity!);
                result.Dependents = Impact(graph!, entity!);
                result.DirectDependents = result.Dependents.Where(d => d.Depth == 1)
                    .Select(d => (d.Dependent.Kind, d.Dependent.Id)).Distinct().Count();
                result.DeleteBlocked = deleteGuarded.Contains(entity!.Kind) && result.DirectDependents > 0;
                return result;
            }, token);
        }

        [Description("Lists what an entity of a model uses: the names in its rule text and its settings, each " +
                     "resolved to the entity it names. A use whose Target is null names nothing that exists, so " +
                     "the rule would not compile or the setting would not work.")]
        [ServiceOperation("EntityAnalysisModelDependencyDependencies", OperationKind.Read, Idempotent = true)]
        public Task<DependencyListDto> DependenciesAsync(
            [Description("The model the entity belongs to.")]
            int entityAnalysisModelId,
            [Description("The kind of entity, as for EntityAnalysisModelDependencyDependents.")]
            string? kind,
            [Description("The entity's id.")] int id,
            CancellationToken token = default)
        {
            return RunAsync("Dependencies", async () =>
            {
                var result = new DependencyListDto();
                var (graph, entity, error) = await ResolveAsync(entityAnalysisModelId, kind, id, token)
                    .ConfigureAwait(false);
                if (error != null)
                {
                    result.Errors = [error];
                    return result;
                }

                result.Entity = ToDto(entity!);
                result.Dependencies = graph!.DependenciesOf(entity!).Select(d => ToDto(d, 1, null)).ToList();
                return result;
            }, token);
        }

        [Description("Finds the dependency problems in a model: uses of names that resolve to nothing, active " +
                     "entities that use inactive ones, and lists, dictionaries, TTL counters, abstraction rules, " +
                     "abstraction calculations, inline functions and adaptations that nothing uses.")]
        [ServiceOperation("EntityAnalysisModelDependencyIssues", OperationKind.Read, Idempotent = true)]
        public Task<DependencyIssuesDto> IssuesAsync(
            [Description("The model to check.")] int entityAnalysisModelId,
            CancellationToken token = default)
        {
            return RunAsync("Issues", async () =>
            {
                var model = await RequireModelAsync(entityAnalysisModelId, token).ConfigureAwait(false);
                var graph = await ModelDependencyGraphBuilder
                    .BuildAsync(dbContext, tenantRegistryId, userName, model.Id, log, token).ConfigureAwait(false);

                return new DependencyIssuesDto
                {
                    Dangling = graph.Dangling().Select(d => ToDto(d, 1, null)).ToList(),
                    OnInactiveTargets = graph.OnInactiveTargets().Select(d => ToDto(d, 1, null)).ToList(),
                    Unreferenced = graph.Unreferenced(unreferencedKinds).OrderBy(e => e.Kind).ThenBy(e => e.Name)
                        .Select(ToDto).ToList()
                };
            }, token);
        }

        private async Task<(ModelDependencyGraph? Graph, ModelEntity? Entity, ValidationErrorDto? Error)>
            ResolveAsync(int entityAnalysisModelId, string? kind, int id, CancellationToken token)
        {
            var model = await RequireModelAsync(entityAnalysisModelId, token).ConfigureAwait(false);
            if (!Enum.TryParse<ModelEntityKind>(kind, true, out var parsed) || !Enum.IsDefined(parsed) ||
                int.TryParse(kind, out _))
            {
                return (null, null, Error("Kind", "KindInvalid",
                    string.Format(strings[EntityAnalysisModelDependencyResources.KindInvalid], kind,
                        string.Join(", ", Enum.GetNames<ModelEntityKind>()))));
            }

            var graph = await ModelDependencyGraphBuilder
                .BuildAsync(dbContext, tenantRegistryId, userName, model.Id, log, token).ConfigureAwait(false);
            var entity = graph.Entities.FirstOrDefault(e => e.Kind == parsed && e.Id == id);

            return entity == null
                ? (null, null, Error("Id", "EntityNotFound",
                    string.Format(strings[EntityAnalysisModelDependencyResources.EntityNotFound], parsed, id)))
                : (graph, entity, null);
        }

        private static List<ModelDependencyDto> Impact(ModelDependencyGraph graph, ModelEntity entity)
        {
            var result = new List<ModelDependencyDto>();
            var visited = new HashSet<ModelEntity> { entity };
            var queue = new Queue<(ModelEntity Target, int Depth, ModelEntity? Via)>();
            queue.Enqueue((entity, 1, null));

            while (queue.Count > 0 && result.Count < MaxDependents)
            {
                var (target, depth, via) = queue.Dequeue();
                foreach (var dependency in graph.DependentsOf(target.Kind, target.Id)
                             .Where(d => d.Dependent != target)
                             .OrderBy(d => d.Dependent.Kind).ThenBy(d => d.Dependent.Name))
                {
                    result.Add(ToDto(dependency, depth, via == null ? null : $"{via.Kind}:{via.Name}"));
                    if (depth < MaxDepth && visited.Add(dependency.Dependent))
                    {
                        queue.Enqueue((dependency.Dependent, depth + 1, via ?? dependency.Dependent));
                    }
                }
            }

            return result;
        }

        private static ModelDependencyDto ToDto(ModelDependency dependency, int depth, string? via)
        {
            return new ModelDependencyDto
            {
                Dependent = ToDto(dependency.Dependent),
                Target = dependency.Target == null ? null : ToDto(dependency.Target),
                DependencyKind = dependency.Kind.ToString(),
                Name = string.IsNullOrEmpty(dependency.Namespace)
                    ? dependency.Name
                    : $"{dependency.Namespace}.{dependency.Name}",
                Line = dependency.Line,
                Depth = depth,
                Via = via
            };
        }

        private static ModelEntityDto ToDto(ModelEntity entity)
        {
            return new ModelEntityDto
                { Kind = entity.Kind.ToString(), Id = entity.Id, Name = entity.Name, Active = entity.Active };
        }

        private async Task<T> RunAsync<T>(string operation, Func<Task<T>> body, CancellationToken token)
        {
            using var op = OperationScope.Start("EntityAnalysisModelDependency", operation, userName, tenantRegistryId,
                auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelDependency.{operation}: entry user={userName}");
            }

            try
            {
                token.ThrowIfCancellationRequested();
                EnsurePermitted($"EntityAnalysisModelDependency.{operation}");
                return await body().ConfigureAwait(false);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
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
                log.Error($"EntityAnalysisModelDependency.{operation}: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<Data.Poco.EntityAnalysisModel> RequireModelAsync(int entityAnalysisModelId,
            CancellationToken token)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, tenantRegistryId)
                .GetByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            if (model == null || model.TenantRegistryId != tenantRegistryId)
            {
                throw new NotFoundException(strings[EntityAnalysisModelDependencyResources.ModelNotFound]);
            }

            return model;
        }

        private static ValidationErrorDto Error(string propertyName, string errorCode, string message)
        {
            return new ValidationErrorDto { PropertyName = propertyName, ErrorCode = errorCode, Message = message };
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

            throw new ForbiddenException(strings[EntityAnalysisModelDependencyResources.PermissionDenied], permissions);
        }
    }
}