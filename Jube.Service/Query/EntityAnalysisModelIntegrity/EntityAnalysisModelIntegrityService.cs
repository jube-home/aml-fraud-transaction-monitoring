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
using Jube.Data.Query;
using Jube.Data.Query.Models;
using Jube.Data.Repository;
using Jube.Dto.Query.EntityAnalysisModelIntegrity;
using Jube.Engine.Integrity;
using Jube.Parser.Dependency;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelIntegrity;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Jube.Service.Query.EntityAnalysisModelIntegrity
{
    public sealed class EntityAnalysisModelIntegrityService
    {
        private static readonly TimeSpan HeartbeatTolerance = TimeSpan.FromMinutes(10);

        private static readonly int[] permissions = [60];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly IEngineStateSource? engineStateSource;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisModelIntegrityService(DbContext dbContext, IEngineStateSource? engineStateSource,
            string userName, int tenantRegistryId, PermissionValidation permissionValidation, ILog log,
            ILog auditLog, IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.engineStateSource = engineStateSource;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
        }

        public static Task<EntityAnalysisModelIntegrityService> CreateAsync(DbContext dbContext,
            IEngineStateSource? engineStateSource, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, engineStateSource, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelIntegrityService> CreateAsync(DbContext dbContext,
            IEngineStateSource? engineStateSource, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelIntegrityResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelIntegrity.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelIntegrityResources
                    .NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelIntegrity.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelIntegrityResources
                    .NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelIntegrityService(dbContext, engineStateSource, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the caller's tenant's models that can be checked, by name.")]
        [ServiceOperation("EntityAnalysisModelIntegrityModels", OperationKind.Read, Idempotent = true)]
        public Task<List<IntegrityModelDto>> ModelsAsync(CancellationToken token = default)
        {
            return RunAsync("Models", async () =>
                (await new EntityAnalysisModelRepository(dbContext, tenantRegistryId).GetAsync(token)
                    .ConfigureAwait(false))
                .Where(m => m.TenantRegistryId == tenantRegistryId)
                .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .Select(m => new IntegrityModelDto { Id = m.Id, Name = m.Name, Active = m.Active == 1 })
                .ToList(), token);
        }

        [Description("Checks the basic integrity of a model and returns every finding with a stable code, " +
                     "errors first: dependencies (names that resolve to nothing, uses of inactive entities, " +
                     "entities nothing uses), compilation (rules the engine could not compile at its last " +
                     "synchronisation), configuration (TTL counters that are disabled, search key TTLs that " +
                     "shorten abstraction windows, no active activation rules) and the engine (instances that " +
                     "stopped reporting, and entities the engine has not loaded or still runs after they were " +
                     "deactivated). The engine's state comes from the engine in this process when there is one, " +
                     "otherwise from what engines recorded at their last synchronisation. Nothing is changed.")]
        [ServiceOperation("EntityAnalysisModelIntegrityCheck", OperationKind.Read, Idempotent = true)]
        public Task<ModelIntegrityReportDto> CheckAsync(
            [Description("The model to check.")] int entityAnalysisModelId,
            CancellationToken token = default)
        {
            return RunAsync("Check", async () =>
            {
                var model = await RequireModelAsync(entityAnalysisModelId, token).ConfigureAwait(false);
                var graph = await ModelDependencyGraphBuilder
                    .BuildAsync(dbContext, tenantRegistryId, userName, model.Id, log, token).ConfigureAwait(false);
                var inputs = await new GetModelIntegrityInputsQuery(dbContext, tenantRegistryId)
                    .ExecuteAsync(model.Id, token).ConfigureAwait(false);
                var observation = await ObserveAsync(model.Id, inputs, token).ConfigureAwait(false);
                var now = DateTime.UtcNow;

                var checks = ModelIntegrityRules.DependencyChecks(graph)
                    .Concat(ModelIntegrityRules.CompilationChecks(inputs.CompileStatuses.Select(c =>
                        new CompileStatus(c.Kind, c.Id, c.Name, c.Active, c.Compiled, c.CompileError))))
                    .Concat(ModelIntegrityRules.ConfigurationChecks(model.Active == 1, model.EnableTtlCounter == 1,
                        graph, inputs.SearchRules.Select(r => new SearchWindowInput(r.Id, r.Name, r.Interval, r.Value,
                            r.SearchKey, r.SearchKeyTtlInterval, r.SearchKeyTtlValue)), now))
                    .Concat(ModelIntegrityRules.EngineChecks(observation, model.Active == 1, graph.Entities, now,
                        HeartbeatTolerance))
                    .OrderBy(c => c.Severity)
                    .ToList();

                return new ModelIntegrityReportDto
                {
                    EntityAnalysisModelId = model.Id,
                    ModelName = model.Name,
                    CheckedDate = now,
                    EngineSource = observation.Source.ToString(),
                    Errors = checks.Count(c => c.Severity == IntegritySeverity.Error),
                    Warnings = checks.Count(c => c.Severity == IntegritySeverity.Warning),
                    Infos = checks.Count(c => c.Severity == IntegritySeverity.Info),
                    Checks = checks.Select(c => new IntegrityCheckDto
                    {
                        Code = c.Code.ToString(), Title = IntegrityCodeText.Describe(c.Code),
                        Severity = c.Severity.ToString(), Category = c.Category,
                        EntityKind = c.EntityKind, EntityId = c.EntityId, EntityName = c.EntityName,
                        Message = c.Message
                    }).ToList()
                };
            }, token);
        }

        [Description("Returns what the engine is running for a model: the engine instances that synchronise the " +
                     "tenant with when each last reported and synchronised, and for each instance whether it has " +
                     "started the model and how many entities of each kind it has loaded. From the engine in this " +
                     "process when there is one, otherwise from what engines recorded at their last " +
                     "synchronisation.")]
        [ServiceOperation("EntityAnalysisModelIntegrityEngineState", OperationKind.Read, Idempotent = true)]
        public Task<EngineStateDto> EngineStateAsync(
            [Description("The model.")] int entityAnalysisModelId,
            CancellationToken token = default)
        {
            return RunAsync("EngineState", async () =>
            {
                var model = await RequireModelAsync(entityAnalysisModelId, token).ConfigureAwait(false);
                var inputs = await new GetModelIntegrityInputsQuery(dbContext, tenantRegistryId)
                    .ExecuteAsync(model.Id, token).ConfigureAwait(false);
                var observation = await ObserveAsync(model.Id, inputs, token).ConfigureAwait(false);
                var guids = await new GetModelEntityGuidsQuery(dbContext, tenantRegistryId)
                    .ExecuteAsync(model.Id, token).ConfigureAwait(false);

                return new EngineStateDto
                {
                    Source = observation.Source.ToString(),
                    Nodes = observation.Nodes.Select(n => new EngineNodeDto
                    {
                        Instance = n.Instance, HeartbeatDate = n.HeartbeatDate, SynchronisedDate = n.SynchronisedDate
                    }).ToList(),
                    Instances = observation.States.Select(s => new EngineInstanceStateDto
                    {
                        Instance = s.Instance,
                        Started = s.Started,
                        CapturedDate = s.CapturedDate,
                        Loaded = s.Loaded.GroupBy(l => l.Kind).OrderBy(g => g.Key, StringComparer.Ordinal)
                            .Select(g => new EngineLoadedCountDto
                            {
                                Kind = g.Key, Count = g.Count(),
                                Entities = g.OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
                                    .Select(l => new EngineLoadedEntityDto
                                    {
                                        Id = l.Id, Name = l.Name,
                                        Guid = guids.TryGetValue((l.Kind, l.Id), out var guid) ? guid : null
                                    }).ToList()
                            }).ToList()
                    }).ToList()
                };
            }, token);
        }

        [Description("Returns a model's dependency graph as nodes (entities, and names that resolve to nothing) " +
                     "and edges drawn from the entity that uses to the entity used, labelled with the name in " +
                     "rule text or the setting. At most 150 nodes are returned, the most connected first; give " +
                     "an entity kind and id to centre the graph on that entity and its neighbours within radius " +
                     "steps.")]
        [ServiceOperation("EntityAnalysisModelIntegrityGraph", OperationKind.Read, Idempotent = true)]
        public Task<ModelGraphDto> GraphAsync(
            [Description("The model.")] int entityAnalysisModelId,
            [Description("The kind of entity to centre on, e.g. TtlCounter; leave out for the whole model.")]
            string? focusKind = null,
            [Description("The id of the entity to centre on.")]
            int? focusId = null,
            [Description("How many steps from the focus to include; 1 to 5.")]
            int radius = 2,
            CancellationToken token = default)
        {
            return RunAsync("Graph", async () =>
            {
                var model = await RequireModelAsync(entityAnalysisModelId, token).ConfigureAwait(false);
                var graph = await ModelDependencyGraphBuilder
                    .BuildAsync(dbContext, tenantRegistryId, userName, model.Id, log, token).ConfigureAwait(false);

                ModelEntity? focus = null;
                if (!string.IsNullOrEmpty(focusKind) && focusId.HasValue)
                {
                    focus = Enum.TryParse<ModelEntityKind>(focusKind, true, out var kind) && Enum.IsDefined(kind)
                        ? graph.Entities.FirstOrDefault(e => e.Kind == kind && e.Id == focusId.Value)
                        : null;
                    if (focus == null)
                    {
                        throw new NotFoundException(string.Format(
                            strings[EntityAnalysisModelIntegrityResources.EntityNotFound], focusKind, focusId));
                    }
                }

                var built = ModelGraphBuilder.Build(graph, focus, Math.Clamp(radius, 1, 5));
                return new ModelGraphDto
                {
                    Nodes = built.Nodes.Select(n => new ModelGraphNodeDto
                        { Id = n.Id, Label = n.Label, Kind = n.Kind, Active = n.Active, Detail = n.Detail }).ToList(),
                    Edges = built.Edges.Select(e => new ModelGraphEdgeDto
                        { From = e.From, To = e.To, Label = e.Label, Dashed = e.Dashed }).ToList(),
                    Truncated = built.Truncated,
                    MaxNodes = ModelGraphBuilder.MaxNodes,
                    Focus = built.Focus
                };
            }, token);
        }

        private async Task<EngineObservation> ObserveAsync(int entityAnalysisModelId,
            ModelIntegrityInputsDto inputs, CancellationToken token)
        {
            var nodes = inputs.Nodes.Select(n => new EngineNode(n.Instance, n.HeartbeatDate, n.SynchronisedDate))
                .ToList();
            var snapshots = (await new EntityAnalysisModelEngineSnapshotRepository(dbContext)
                    .GetByEntityAnalysisModelIdAsync(tenantRegistryId, entityAnalysisModelId, token)
                    .ConfigureAwait(false))
                .Select(s => JsonConvert.DeserializeObject<EngineModelState>(s.Json))
                .Where(s => s != null)
                .Select(s => s!)
                .ToList();

            if (engineStateSource is { Available: true })
            {
                var states = snapshots.Where(s => s.Instance != engineStateSource.Instance).ToList();
                var local = engineStateSource.GetModelState(entityAnalysisModelId);
                if (local != null)
                {
                    states.Add(local);
                }

                return new EngineObservation(EngineStateSourceKind.InProcess, states, nodes);
            }

            return new EngineObservation(
                snapshots.Count > 0 ? EngineStateSourceKind.Snapshot : EngineStateSourceKind.Unavailable, snapshots,
                nodes);
        }

        private async Task<T> RunAsync<T>(string operation, Func<Task<T>> body, CancellationToken token)
        {
            using var op = OperationScope.Start("EntityAnalysisModelIntegrity", operation, userName, tenantRegistryId,
                auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelIntegrity.{operation}: entry user={userName}");
            }

            try
            {
                token.ThrowIfCancellationRequested();
                EnsurePermitted($"EntityAnalysisModelIntegrity.{operation}");
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
                log.Error($"EntityAnalysisModelIntegrity.{operation}: unexpected failure user={userName}", ex);
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
                throw new NotFoundException(strings[EntityAnalysisModelIntegrityResources.ModelNotFound]);
            }

            return model;
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

            throw new ForbiddenException(strings[EntityAnalysisModelIntegrityResources.PermissionDenied], permissions);
        }
    }
}