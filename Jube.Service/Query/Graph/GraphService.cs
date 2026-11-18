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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.GetGraphEdgeEventTimesQuery;
using Jube.Data.Query.GetGraphEdgeWindowQuery;
using Jube.Data.Query.GetGraphExplodeQuery;
using Jube.Data.Query.GetGraphFlowQuery;
using Jube.Data.Query.GetGraphPathsQuery;
using Jube.Data.Query.GetGraphNeighbourhoodQuery;
using Jube.Data.Query.GetGraphNodeCasesQuery;
using Jube.Data.Repository;
using Jube.Dto.Query.Graph;
using Jube.Dto.Options;
using Jube.Graph.Model;
using Jube.Graph.Relevance;
using Jube.Resources;
using Jube.Service.Charts;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.Graph;
using Jube.Service.Observability;
using Jube.Service.Query.Graph.Jobs;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Jube.Service.Query.Graph
{
    public sealed class GraphService
    {
        public const int MaximumHops = 3;
        public const int MaximumFanOut = 100;
        public const int DefaultFanOut = 25;
        public const int MaximumCap = 150;
        public const int ArchiveIdsPerEdge = 50;
        public const int MaximumLanes = 1000;
        public const int MaximumTransactions = 5000;
        public const int MaximumTimeStripEvents = 20000;
        public const int MaximumPathHops = 6;
        public const int MaximumPaths = 20;
        public const int PathFanOut = 30;
        public const int MaximumExpanded = 25;
        public const int MaximumTouchingEdges = 50000;
        public const int MaximumPinnedLength = 200000;
        public const int QueryTimeoutSeconds = 30;
        public const double MaximumFlowBudget = 10000000;
        public const int MaximumFlowRows = 2000;
        public const int MaximumExplorationName = 120;
        public const int MaximumAddressLength = 8000;
        private const int MaximumKeyLength = 128;
        private const int MaximumValueLength = 1024;
        private static readonly int[] permissions = [49, 1];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private GraphService(DbContext dbContext, string userName, int tenantRegistryId,
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
            this.dbContext.CommandTimeout = QueryTimeoutSeconds;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<GraphService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<GraphService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(GraphResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[GraphResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[GraphResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new GraphService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation, log,
                auditLog, serviceChangeBus, strings);
        }

        [Description("Returns the network around a key value: the nodes within the requested hops of the seed " +
                     "under a Graph Registry's edge definitions, the edges among them with the archive ids of the " +
                     "transactions behind each edge in the window, hubs hidden unless asked, and clusters collapsed " +
                     "beyond the render cap. Read-only.")]
        [ServiceOperation("GraphNeighbourhoodGet", OperationKind.Read, Idempotent = true)]
        public async Task<GraphNeighbourhoodDto> GetNeighbourhoodAsync(GraphNeighbourhoodRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "Neighbourhood", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();
                var registry = await ResolveRegistryAsync(request.RegistryGuid, token).ConfigureAwait(false);
                var key = RequireKey(request.Key, request.Value);
                var hops = request.Hops ?? registry.Registry.DefaultHops ?? 2;
                var fanOut = request.FanOut ?? DefaultFanOut;
                var cap = Math.Clamp(request.Cap ?? registry.Registry.RenderCap ?? MaximumCap, 10, MaximumCap);

                if (hops is < 1 or > MaximumHops)
                {
                    throw new ArgumentException(strings[GraphResources.HopsOutOfRange]);
                }

                if (fanOut is < 1 or > MaximumFanOut)
                {
                    throw new ArgumentException(strings[GraphResources.FanOutOutOfRange]);
                }

                var expanded = ParseLongs(request.Expanded).Distinct().ToList();

                if (expanded.Count > MaximumExpanded)
                {
                    throw new ArgumentException(strings[GraphResources.ExpandedInvalid]);
                }

                var (from, to) = Window(request.From, request.To, registry.Registry.DefaultWindowDays ?? 30);
                var selected = SelectDefinitions(registry, request.Edges);
                var result = new GraphNeighbourhoodDto
                {
                    From = Offset(from), To = Offset(to), Hops = hops,
                    Definitions = registry.Definitions.Select(d => ToDefinition(d, registry)).ToList(),
                    NoDefinitions = registry.Definitions.Count == 0,
                    BuiltDate = await BuiltDateAsync(registry, token).ConfigureAwait(false)
                };
                var nodes = new GraphViewerNodes(dbContext, tenantRegistryId, userName);
                var seed = await nodes.GetAsync(tenantRegistryId, key.Key, key.Value, token).ConfigureAwait(false);

                if (seed == null || selected.Length == 0)
                {
                    op.Rows(0);

                    return result;
                }

                var seeds = new List<long> { seed.Id };

                if (expanded.Count > 0)
                {
                    seeds.AddRange((await nodes.GetByIdsAsync(tenantRegistryId, expanded, token).ConfigureAwait(false))
                        .Select(n => n.Id));
                }

                var rows = await new GetGraphNeighbourhoodQuery(dbContext).ExecuteAsync(tenantRegistryId,
                        seeds.Distinct().ToArray(), selected, request.IncludeHubs == true, registry.Granted, from, to,
                        request.MinimumAmount ?? 0, fanOut, hops, token)
                    .ConfigureAwait(false);
                var reached = rows.GroupBy(r => r.NodeId)
                    .Select(g => g.OrderBy(r => r.Depth).First())
                    .OrderBy(r => r.Depth).ThenBy(r => r.NodeId)
                    .ToList();
                var seedRow = reached.FirstOrDefault(r => r.NodeId == seed.Id);

                if (seedRow != null)
                {
                    seedRow.Depth = 0;
                }

                var interest = new Dictionary<long, double>();

                if (request.Relevance == true)
                {
                    (reached, interest) = await RankByRelevanceAsync(seed.Id, rows, reached, token).ConfigureAwait(false);
                    result.Relevance = true;
                }

                var kept = reached.Take(cap).ToList();
                var overflow = reached.Skip(cap).ToList();
                var keptIds = kept.Select(r => r.NodeId).ToHashSet();
                var edgeIds = rows.Where(r => r.EdgeId.HasValue && keptIds.Contains(r.NodeId)
                                              && r.ViaNodeId.HasValue && keptIds.Contains(r.ViaNodeId.Value))
                    .Select(r => r.EdgeId!.Value).Distinct().ToArray();
                var nodeRows = await nodes.GetByIdsAsync(tenantRegistryId, keptIds, token).ConfigureAwait(false);
                var overflowRows = overflow.Count == 0
                    ? []
                    : await nodes.GetByIdsAsync(tenantRegistryId, overflow.Select(r => r.NodeId).ToList(), token)
                        .ConfigureAwait(false);
                var visibleNodeIds = nodeRows.Select(n => n.Id).ToHashSet();
                var visibleOverflowIds = overflowRows.Select(n => n.Id).ToHashSet();
                overflow = overflow.Where(r => visibleOverflowIds.Contains(r.NodeId)).ToList();
                var cases = await CasesAsync(nodeRows, token).ConfigureAwait(false);
                var byId = nodeRows.ToDictionary(n => n.Id);
                var tags = await TagsAsync(keptIds, token).ConfigureAwait(false);

                foreach (var row in kept)
                {
                    if (byId.TryGetValue(row.NodeId, out var node))
                    {
                        result.Nodes.Add(WithTags(ToNode(node, row.Depth, row.ViaNodeId, node.Id == seed.Id, cases), tags));
                    }
                }

                foreach (var node in result.Nodes)
                {
                    node.Interest = interest.TryGetValue(node.Id, out var value) ? value : null;
                }

                result.Seed = result.Nodes.FirstOrDefault(n => n.Seed);
                result.Edges = (await EdgesAsync(edgeIds, registry, from, to, token).ConfigureAwait(false))
                    .Where(e => visibleNodeIds.Contains(e.FromNodeId) && visibleNodeIds.Contains(e.ToNodeId)).ToList();
                result.OverCap = overflow.Count;
                var overflowById = overflowRows.ToDictionary(n => n.Id);

                foreach (var group in overflow.Where(r => r.ViaNodeId.HasValue && overflowById.ContainsKey(r.NodeId))
                             .GroupBy(r => (r.ViaNodeId!.Value, overflowById[r.NodeId].Key)))
                {
                    result.Combos.Add(new GraphComboDto
                    {
                        Id = $"{group.Key.Item1}:{group.Key.Key}",
                        ViaNodeId = group.Key.Item1,
                        Key = group.Key.Key,
                        Count = group.Count(),
                        NodeIds = group.Select(r => r.NodeId).ToList()
                    });
                }

                if (request.IncludeHubs != true)
                {
                    var touching = await new GraphEdgeRepository(dbContext).GetTouchingAsync(tenantRegistryId,
                        seed.Id, token).ConfigureAwait(false);
                    result.HiddenHubs = touching.Where(e => selected.Contains(e.GraphRegistryEdgeGuid))
                        .Select(e => e.FromNodeId == seed.Id ? (e.ToHub, e.ToNodeId) : (e.FromHub, e.FromNodeId))
                        .Where(x => x.Item1 == 1).Select(x => x.Item2).Distinct().Count();
                }

                op.Rows(result.Nodes.Count);

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
            catch (ArgumentException)
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
                log.Error($"Graph.Neighbourhood: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Finds up to K shortest paths between two nodes under a Graph Registry, each as the ordered " +
                     "node ids and edge ids with a weight and a sentence, together with the nodes and edges touched " +
                     "so the paths can be drawn. Hubs are skipped unless asked. Read-only.")]
        [ServiceOperation("GraphPathsGet", OperationKind.Read, Idempotent = true)]
        public async Task<GraphPathsDto> GetPathsAsync(GraphPathsRequestDto? request, CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "Paths", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();
                var registry = await ResolveRegistryAsync(request.RegistryGuid, token).ConfigureAwait(false);

                if (request.FromNodeId <= 0 || request.ToNodeId <= 0 || request.FromNodeId == request.ToNodeId)
                {
                    throw new ArgumentException(strings[GraphResources.NodeIdsRequired]);
                }

                var maxHops = request.MaxHops ?? 4;
                var k = request.K ?? 5;

                if (maxHops is < 1 or > MaximumPathHops || k is < 1 or > MaximumPaths)
                {
                    throw new ArgumentException(strings[GraphResources.PathHopsOutOfRange]);
                }

                var nodes = new GraphViewerNodes(dbContext, tenantRegistryId, userName);
                var from = await nodes.GetByIdAsync(tenantRegistryId, request.FromNodeId, token).ConfigureAwait(false)
                           ?? throw new NotFoundException(strings[GraphResources.NodeNotFound]);
                var to = await nodes.GetByIdAsync(tenantRegistryId, request.ToNodeId, token).ConfigureAwait(false)
                         ?? throw new NotFoundException(strings[GraphResources.NodeNotFound]);
                var (windowFrom, windowTo) = Window(request.From, request.To, registry.Registry.DefaultWindowDays ?? 30);
                var selected = SelectDefinitions(registry, request.Edges);
                var forward = (maxHops + 1) / 2;
                var rows = selected.Length == 0
                    ? []
                    : await new GetGraphPathsQuery(dbContext).ExecuteAsync(tenantRegistryId, from.Id, to.Id, selected,
                        request.IncludeHubs == true, registry.Granted, forward, maxHops - forward, PathFanOut, k, token)
                        .ConfigureAwait(false);
                var nodeIds = rows.SelectMany(r => ParseLongs(r.NodePath)).Append(from.Id).Append(to.Id).Distinct().ToList();
                var nodeRows = await nodes.GetByIdsAsync(tenantRegistryId, nodeIds, token).ConfigureAwait(false);
                var visiblePathIds = nodeRows.Select(n => n.Id).ToHashSet();
                rows = rows.Where(r => ParseLongs(r.NodePath).All(visiblePathIds.Contains)).ToList();
                var edgeIds = rows.SelectMany(r => ParseLongs(r.EdgePath)).Distinct().ToArray();
                var cases = await CasesAsync(nodeRows, token).ConfigureAwait(false);
                var depth = new Dictionary<long, int> { [from.Id] = 0 };

                foreach (var row in rows)
                {
                    var path = ParseLongs(row.NodePath).ToList();

                    for (var i = 0; i < path.Count; i++)
                    {
                        if (!depth.TryGetValue(path[i], out var d) || i < d)
                        {
                            depth[path[i]] = i;
                        }
                    }
                }

                var result = new GraphPathsDto
                {
                    Nodes = nodeRows.OrderBy(n => depth.GetValueOrDefault(n.Id, maxHops)).ThenBy(n => n.Id)
                        .Select(n => ToNode(n, depth.GetValueOrDefault(n.Id, maxHops), null, n.Id == from.Id, cases)).ToList(),
                    Edges = await EdgesAsync(edgeIds, registry, windowFrom, windowTo, token).ConfigureAwait(false)
                };
                result.From = result.Nodes.FirstOrDefault(n => n.Id == from.Id);
                result.To = result.Nodes.FirstOrDefault(n => n.Id == to.Id);

                foreach (var row in rows)
                {
                    var path = new GraphPathDto
                    {
                        Hops = row.Hops, Weight = row.Weight, NodeIds = ParseLongs(row.NodePath).ToList(),
                        EdgeIds = ParseLongs(row.EdgePath).ToList()
                    };
                    path.Sentence = string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentencePath], path.Hops,
                        from.Key, from.Value, to.Key, to.Value, path.Weight);
                    result.Paths.Add(path);
                    result.Sentences.Add(path.Sentence);
                }

                op.Rows(result.Paths.Count);

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
            catch (ArgumentException)
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
                log.Error($"Graph.Paths: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Follows the money from (or back to) a node through time-ordered transactions: each hop must " +
                     "occur after the previous one within the hop window and carry a comparable amount, so the result is " +
                     "the set of tranches, the nodes they reach, the pass-through nodes and the archive ids behind each " +
                     "step, drawable with the flow layout. Read-only.")]
        [ServiceOperation("GraphFlowGet", OperationKind.Read, Idempotent = true)]
        public async Task<GraphFlowDto> GetFlowAsync(GraphFlowRequestDto? request, CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "Flow", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();
                var registry = await ResolveRegistryAsync(request.RegistryGuid, token).ConfigureAwait(false);
                var hours = request.HopWindowHours ?? 72;
                var tolerance = request.Tolerance ?? 0.2;
                var depth = request.Depth ?? 4;
                var fanOut = request.FanOut ?? 10;

                if (hours is < 1 or > 720 || tolerance is < 0 or > 0.9 || depth is < 1 or > MaximumPathHops
                    || fanOut is < 1 or > 50 || Math.Pow(fanOut, depth) > MaximumFlowBudget)
                {
                    throw new ArgumentException(strings[GraphResources.FlowInvalid]);
                }

                var nodes = new GraphViewerNodes(dbContext, tenantRegistryId, userName);
                var seed = await nodes.GetByIdAsync(tenantRegistryId, request.NodeId, token).ConfigureAwait(false)
                           ?? throw new NotFoundException(strings[GraphResources.NodeNotFound]);
                var (windowFrom, windowTo) = Window(request.From, request.To, registry.Registry.DefaultWindowDays ?? 30);
                var forward = request.Forward ?? true;
                var at = request.At.HasValue ? DateTime.SpecifyKind(request.At.Value, DateTimeKind.Utc) : forward ? windowFrom : windowTo;
                var amount = request.Amount ?? 0;
                var firstWindow = windowTo - windowFrom;

                if (request.ArchiveId.HasValue)
                {
                    var start = await dbContext.GraphEdgeEvent
                        .Where(e => e.TenantRegistryId == tenantRegistryId && e.ArchiveId == request.ArchiveId.Value && e.Retracted == 0)
                        .OrderBy(e => e.Id).FirstOrDefaultAsync(token).ConfigureAwait(false)
                        ?? throw new NotFoundException(strings[GraphResources.EdgeNotFound]);
                    at = forward ? start.OccurredDate.AddMilliseconds(-1) : start.OccurredDate.AddMilliseconds(1);
                    amount = start.Amount ?? 0;
                    firstWindow = TimeSpan.FromHours(hours);
                }

                var selected = SelectDefinitions(registry, request.Edges)
                    .Where(g => registry.Definitions.Any(d => d.Guid == g && d.KindId == (int)GraphEdgeKind.MoneyFlow)).ToArray();
                var rows = selected.Length == 0
                    ? []
                    : await new GetGraphFlowQuery(dbContext).ExecuteAsync(tenantRegistryId, seed.Id, at, amount, forward, selected,
                        registry.Granted, firstWindow, TimeSpan.FromHours(hours), tolerance, fanOut, depth, MaximumFlowRows,
                        token)
                        .ConfigureAwait(false);
                var flowIds = rows.SelectMany(r => ParseLongs(r.NodePath)).Distinct().ToList();

                if (flowIds.Count > 0)
                {
                    var visibleFlowIds = (await nodes.GetByIdsAsync(tenantRegistryId, flowIds, token).ConfigureAwait(false))
                        .Select(n => n.Id).ToHashSet();
                    rows = rows.Where(r => ParseLongs(r.NodePath).All(visibleFlowIds.Contains)).ToList();
                }

                var result = new GraphFlowDto { Forward = forward };
                var depthOf = new Dictionary<long, int> { [seed.Id] = 0 };
                var edgeIds = new HashSet<long>();

                foreach (var row in rows)
                {
                    var tranche = new GraphFlowTrancheDto
                    {
                        Depth = row.Depth, At = Offset(row.At), Amount = row.Tranche, NodeId = row.NodeId,
                        NodeIds = ParseLongs(row.NodePath).ToList(), EdgeIds = ParseLongs(row.EdgePath).ToList(),
                        ArchiveIds = ParseLongs(row.ArchivePath).ToList()
                    };
                    result.Tranches.Add(tranche);
                    edgeIds.UnionWith(tranche.EdgeIds);

                    for (var i = 0; i < tranche.NodeIds.Count; i++)
                    {
                        if (!depthOf.TryGetValue(tranche.NodeIds[i], out var d) || i < d)
                        {
                            depthOf[tranche.NodeIds[i]] = i;
                        }
                    }
                }

                var terminal = result.Tranches.Select(t => t.NodeId).ToHashSet();
                var intermediate = result.Tranches.SelectMany(t => t.NodeIds.Skip(1).Take(Math.Max(0, t.NodeIds.Count - 2))).ToHashSet();
                result.PassThroughNodeIds = intermediate.Where(n => n != seed.Id).OrderBy(n => n).ToList();
                var nodeRows = await nodes.GetByIdsAsync(tenantRegistryId, depthOf.Keys.ToList(), token).ConfigureAwait(false);
                var cases = await CasesAsync(nodeRows, token).ConfigureAwait(false);
                result.Graph = new GraphNeighbourhoodDto
                {
                    From = Offset(windowFrom), To = Offset(windowTo), Hops = depth,
                    Definitions = registry.Definitions.Select(d => ToDefinition(d, registry)).ToList(),
                    NoDefinitions = selected.Length == 0,
                    BuiltDate = await BuiltDateAsync(registry, token).ConfigureAwait(false),
                    Nodes = nodeRows.OrderBy(n => depthOf[n.Id]).ThenBy(n => n.Id)
                        .Select(n => ToNode(n, depthOf[n.Id], null, n.Id == seed.Id, cases)).ToList(),
                    Edges = await EdgesAsync(edgeIds.ToArray(), registry, windowFrom, windowTo, token).ConfigureAwait(false)
                };
                result.Graph.Seed = result.Graph.Nodes.FirstOrDefault(n => n.Seed);
                result.Sentences.Add(string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentenceFlow],
                    result.Tranches.Count, terminal.Count, result.Tranches.Count == 0 ? 0 : result.Tranches.Max(t => t.Depth),
                    result.PassThroughNodeIds.Count));
                op.Rows(result.Tranches.Count);

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
            catch (ArgumentException)
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
                log.Error($"Graph.Flow: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the one-hop neighbourhood of a node as lanes: one row per counterpart with its " +
                     "transaction count, total, first and last seen, hub flag, cases and the edge definitions behind " +
                     "it. This is the accessible list view of the network canvas. Read-only.")]
        [ServiceOperation("GraphExplodeGet", OperationKind.Read, Idempotent = true)]
        public async Task<GraphExplodeDto> GetExplodeAsync(GraphExplodeRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "Explode", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();
                var registry = await ResolveRegistryAsync(request.RegistryGuid, token).ConfigureAwait(false);
                var (from, to) = Window(request.From, request.To, registry.Registry.DefaultWindowDays ?? 30);
                var selected = SelectDefinitions(registry, request.Edges);
                var limit = Math.Clamp(request.Limit ?? 200, 1, MaximumLanes);
                var nodes = new GraphViewerNodes(dbContext, tenantRegistryId, userName);
                var node = await nodes.GetByIdAsync(tenantRegistryId, request.NodeId, token).ConfigureAwait(false)
                           ?? throw new NotFoundException(strings[GraphResources.NodeNotFound]);
                var lanes = selected.Length == 0
                    ? []
                    : await new GetGraphExplodeQuery(dbContext).ExecuteAsync(tenantRegistryId, node.Id, selected,
                        registry.Granted, from, to, limit, token).ConfigureAwait(false);
                var counterparts = await nodes.GetByIdsAsync(tenantRegistryId, lanes.Select(l => l.NodeId).ToList(),
                    token).ConfigureAwait(false);
                var cases = await CasesAsync(counterparts.Append(node).ToList(), token).ConfigureAwait(false);
                var byId = counterparts.ToDictionary(n => n.Id);
                var names = registry.Definitions.ToDictionary(d => d.Guid, d => d.Name ?? string.Empty);
                var result = new GraphExplodeDto
                {
                    Node = ToNode(node, 0, null, true, cases), From = Offset(from), To = Offset(to)
                };

                foreach (var lane in lanes)
                {
                    if (!byId.TryGetValue(lane.NodeId, out var counterpart))
                    {
                        continue;
                    }

                    result.Lanes.Add(new GraphExplodeLaneDto
                    {
                        Node = ToNode(counterpart, 1, node.Id, false, cases),
                        Count = lane.Count,
                        Total = lane.Total,
                        First = Offset(lane.First),
                        Last = Offset(lane.Last),
                        Definitions = ParseGuids(lane.Definitions).Select(d => names.GetValueOrDefault(d, string.Empty)).ToList(),
                        EdgeIds = ParseLongs(lane.EdgeIds).ToList()
                    });
                }

                op.Rows(result.Lanes.Count);

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
            catch (ArgumentException)
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
                log.Error($"Graph.Explode: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the archived transactions behind one edge within a window, newest first, with their " +
                     "archive ids so the Case Key Journal can be filtered to exactly the evidence of the edge. " +
                     "Read-only.")]
        [ServiceOperation("GraphEdgeTransactionsGet", OperationKind.Read, Idempotent = true)]
        public async Task<GraphEdgeTransactionsDto> GetEdgeTransactionsAsync(GraphEdgeTransactionsRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "EdgeTransactions", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();
                var edge = await new GraphEdgeRepository(dbContext).GetByIdAsync(tenantRegistryId, request.EdgeId, token)
                               .ConfigureAwait(false)
                           ?? throw new NotFoundException(strings[GraphResources.EdgeNotFound]);
                var definition = await new GraphRegistryEdgeRepository(dbContext, tenantRegistryId)
                    .GetByGuidAsync(edge.GraphRegistryEdgeGuid, token).ConfigureAwait(false);
                var registryRepository = new GraphRegistryRepository(dbContext, tenantRegistryId);
                var definitionRegistry = definition == null
                    ? null
                    : await registryRepository.GetByIdAsync(definition.GraphRegistryId.GetValueOrDefault(), token).ConfigureAwait(false);

                if (edge.Restricted == 1 && (definitionRegistry == null
                                             || !await new GraphRegistryRepository(dbContext, userName)
                                                 .IsGrantedAsync(definitionRegistry.Guid, token)
                                                 .ConfigureAwait(false)))
                {
                    throw new NotFoundException(strings[GraphResources.EdgeNotFound]);
                }

                var from = request.From ?? edge.FirstSeen;
                var to = request.To ?? DateTime.UtcNow;

                if (to <= from)
                {
                    throw new ArgumentException(strings[GraphResources.WindowInvalid]);
                }

                var limit = Math.Clamp(request.Limit ?? 500, 1, MaximumTransactions);
                var events = await new GraphEdgeEventRepository(dbContext)
                    .GetByEdgeInWindowAsync(edge.Id, from, to, limit, token).ConfigureAwait(false);
                var result = new GraphEdgeTransactionsDto
                {
                    Edge = ToEdge(edge, definition?.Name, null),
                    Transactions = events.Select(e => new GraphEdgeTransactionDto
                    {
                        ArchiveId = e.ArchiveId,
                        EntityAnalysisModelInstanceEntryGuid = e.EntityAnalysisModelInstanceEntryGuid,
                        OccurredDate = Offset(e.OccurredDate),
                        Amount = e.Amount
                    }).ToList(),
                    ArchiveIds = events.Select(e => e.ArchiveId).Distinct().ToList()
                };
                op.Rows(result.Transactions.Count);

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
            catch (ArgumentException)
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
                log.Error($"Graph.EdgeTransactions: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the facts of one node: its cached graph features (degree, hub, component, community, " +
                     "distance to confirmed true positives), the edge definitions it takes part in with counts, its " +
                     "cases, and plain sentences stating why it matters. Read-only.")]
        [ServiceOperation("GraphNodeGet", OperationKind.Read, Idempotent = true)]
        public async Task<GraphNodeFactsDto> GetNodeAsync(GraphNodeRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "Node", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();
                var nodes = new GraphViewerNodes(dbContext, tenantRegistryId, userName);
                GraphNode? node;

                if (request.NodeId.HasValue)
                {
                    node = await nodes.GetByIdAsync(tenantRegistryId, request.NodeId.Value, token).ConfigureAwait(false);
                }
                else
                {
                    var key = RequireKey(request.Key, request.Value);
                    node = await nodes.GetAsync(tenantRegistryId, key.Key, key.Value, token).ConfigureAwait(false);
                }

                if (node == null)
                {
                    throw new NotFoundException(strings[GraphResources.NodeNotFound]);
                }

                var cases = await CasesAsync([node], token).ConfigureAwait(false);
                var touching = await new GraphEdgeRepository(dbContext).GetTouchingAsync(tenantRegistryId, node.Id,
                        MaximumTouchingEdges, token)
                    .ConfigureAwait(false);
                var caseCounterparts = touching.Where(e => e.KindId == GraphCaseEdge.KindId)
                    .Select(e => e.FromNodeId == node.Id ? e.ToNodeId : e.FromNodeId).Distinct().ToList();

                if (caseCounterparts.Count > 0)
                {
                    var visibleCounterparts = (await nodes.GetByIdsAsync(tenantRegistryId, caseCounterparts, token)
                            .ConfigureAwait(false)).Select(n => n.Id).ToHashSet();
                    touching = touching.Where(e => e.KindId != GraphCaseEdge.KindId
                                                   || visibleCounterparts.Contains(
                                                       e.FromNodeId == node.Id ? e.ToNodeId : e.FromNodeId)).ToList();
                }

                var visibleDefinitions = new Dictionary<Guid, GraphRegistryEdge>();
                var registryRepository = new GraphRegistryRepository(dbContext, userName);
                var grantedRegistries = new Dictionary<int, bool>();

                foreach (var definition in await new GraphRegistryEdgeRepository(dbContext, tenantRegistryId)
                             .GetOrderByIdAsync(token).ConfigureAwait(false))
                {
                    if (!grantedRegistries.TryGetValue(definition.GraphRegistryId.GetValueOrDefault(), out var granted))
                    {
                        var owner = await new GraphRegistryRepository(dbContext, tenantRegistryId)
                            .GetByIdAsync(definition.GraphRegistryId.GetValueOrDefault(), token).ConfigureAwait(false);
                        granted = owner != null
                                  && await registryRepository.IsGrantedAsync(owner.Guid, token).ConfigureAwait(false);
                        grantedRegistries[definition.GraphRegistryId.GetValueOrDefault()] = granted;
                    }

                    if (definition.Restricted != 1 || granted)
                    {
                        visibleDefinitions[definition.Guid] = definition;
                    }
                }

                var definitions = visibleDefinitions;
                var result = new GraphNodeFactsDto
                {
                    Node = WithTags(ToNode(node, 0, null, false, cases), await TagsAsync([node.Id], token).ConfigureAwait(false))
                };

                foreach (var group in touching.Where(e => e.Restricted == 0 || definitions.ContainsKey(e.GraphRegistryEdgeGuid))
                             .GroupBy(e => e.GraphRegistryEdgeGuid))
                {
                    result.Definitions.Add(new GraphNodeDefinitionCountDto
                    {
                        Guid = group.Key,
                        Name = definitions.GetValueOrDefault(group.Key)?.Name,
                        Counterparts = group.Select(e => e.FromNodeId == node.Id ? e.ToNodeId : e.FromNodeId).Distinct()
                            .Count(),
                        Count = group.Sum(e => e.Count),
                        Total = group.Sum(e => e.AmountSum)
                    });
                }

                if (node.CommunityId.HasValue)
                {
                    var community = await new GraphCommunityRepository(dbContext)
                        .GetByIdAsync(tenantRegistryId, node.CommunityId.Value, token).ConfigureAwait(false);
                    result.Community = community == null ? null : GraphAnalyticsMapper.ToCommunity(community, strings);
                }

                var patterns = await new GraphPatternRepository(dbContext).GetByNodeAsync(tenantRegistryId, node.Id, token)
                    .ConfigureAwait(false);
                result.Patterns = patterns.Select(p => GraphAnalyticsMapper.ToPattern(p, p.AnchorNodeId == node.Id ? result.Node : null, strings))
                    .ToList();
                result.Sentences = Sentences(result.Node);

                if (result.Community != null)
                {
                    result.Sentences.Add(result.Community.Sentence!);
                }

                if (result.Patterns.Count > 0)
                {
                    result.Sentences.Add(string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentencePatternMember],
                        result.Patterns.Count, result.Patterns[0].Kind));
                }

                foreach (var tag in result.Node.Tags)
                {
                    result.Sentences.Add(string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentenceTag],
                        tag.Distance, tag.Tag, tag.Within3));
                }

                op.Rows(result.Definitions.Count);

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
            catch (ArgumentException)
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
                log.Error($"Graph.Node: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Draws the network around a key value as an SVG with the requested layout (force, radial, flow or " +
                     "hierarchy), node sizes by degree, amount or weight, and the positions of the previous render as " +
                     "anchors so the picture only moves where it must. Returns the SVG with the positions and the " +
                     "neighbourhood drawn. Read-only.")]
        [ServiceOperation("GraphRenderSvg", OperationKind.Read, Idempotent = true)]
        public async Task<GraphRenderResultDto> RenderAsync(GraphRenderRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "Render", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                var graph = request.Flow != null
                    ? (await GetFlowAsync(request.Flow, token).ConfigureAwait(false)).Graph
                    : await GetNeighbourhoodAsync(request.Query, token).ConfigureAwait(false);

                if (request.Flow == null && graph.Nodes.Count == 0)
                {
                    op.Rows(0);

                    return new GraphRenderResultDto
                    {
                        Svg = string.Empty,
                        Graph = graph,
                        LayoutId = request.LayoutId ?? (int)GraphLayout.Force
                    };
                }

                var registry = await ResolveRegistryAsync(request.Flow?.RegistryGuid ?? request.Query.RegistryGuid, token)
                    .ConfigureAwait(false);
                var layoutId = request.LayoutId
                               ?? (request.Flow != null ? (int)GraphLayout.Flow : registry.Registry.DefaultLayoutId)
                               ?? (int)GraphLayout.Force;

                if (layoutId is < 1 or > 4)
                {
                    throw new ArgumentException(strings[GraphResources.LayoutInvalid]);
                }

                var (svg, positions) = GraphChartRenderer.Render(graph, request, layoutId, strings);
                op.Rows(graph.Nodes.Count);

                return new GraphRenderResultDto
                {
                    Svg = svg,
                    Graph = graph,
                    LayoutId = layoutId,
                    Positions = positions.Select(p => new GraphAnchorDto { Id = p.Id, X = p.X, Y = p.Y }).ToList()
                };
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
            catch (ArgumentException)
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
                log.Error($"Graph.Render: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Draws the time strip of a neighbourhood: a histogram of the transactions behind its edges within " +
                     "the window, as an SVG whose bars carry their time range, for the brush and the playback cursor. " +
                     "Read-only.")]
        [ServiceOperation("GraphTimeStripSvg", OperationKind.Read, Idempotent = true)]
        public async Task<string> GetTimeStripSvgAsync(GraphNeighbourhoodRequestDto? request, int? bins, bool dark,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "TimeStrip", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                var graph = await GetNeighbourhoodAsync(request, token).ConfigureAwait(false);
                var edgeIds = graph.Edges.Select(e => e.Id).ToArray();
                var times = edgeIds.Length == 0
                    ? []
                    : (await new GetGraphEdgeEventTimesQuery(dbContext).ExecuteAsync(edgeIds, graph.From.UtcDateTime,
                            graph.To.UtcDateTime, MaximumTimeStripEvents, token).ConfigureAwait(false))
                    .Select(t => Offset(t.OccurredDate)).ToList();
                op.Rows(times.Count);

                return GraphChartRenderer.TimeStrip(times, bins ?? 48, dark);
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
            catch (ArgumentException)
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
                log.Error($"Graph.TimeStrip: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Freezes the current network view into the case as evidence: the SVG and a JSON definition (query, " +
                     "layout, positions and the archive ids behind every edge) are stored as two case files under one " +
                     "Document note, the SVG hashed with SHA-256, so the picture can be reproduced and audited later. " +
                     "The case must be open in the caller's tenant and not locked to another user.")]
        [ServiceOperation("GraphCaptureCreate", OperationKind.Write, Idempotent = false)]
        public async Task<GraphCaptureResultDto> CaptureAsync(GraphCaptureRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "Capture", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                if (!permissionValidation.Validate([1]))
                {
                    throw new ForbiddenException(strings[GraphResources.PermissionDenied], [1]);
                }

                var existingCase = await new CaseRepository(dbContext, userName).GetByIdActiveOnlyAsync(request.CaseId, token)
                                       .ConfigureAwait(false)
                                   ?? throw new NotFoundException(strings[GraphResources.CaseNotFound]);

                if (existingCase.Locked == 1 && !string.Equals(existingCase.LockedUser, userName, StringComparison.Ordinal))
                {
                    throw new ArgumentException(strings[GraphResources.CaseLocked]);
                }

                var render = await RenderAsync(request.Render, token).ConfigureAwait(false);
                var svgBytes = Encoding.UTF8.GetBytes(render.Svg ?? string.Empty);
                var hash = Convert.ToHexStringLower(SHA256.HashData(svgBytes));
                var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                var definition = JsonConvert.SerializeObject(new
                {
                    render.Graph.Seed, request.Render.Query, render.LayoutId, request.Render.MeasureId, render.Positions,
                    Window = new { render.Graph.From, render.Graph.To },
                    Edges = render.Graph.Edges.Select(e => new { e.Id, e.FromNodeId, e.ToNodeId, e.DefinitionName, e.ArchiveIds }),
                    Nodes = render.Graph.Nodes.Select(n => new { n.Id, n.Key, n.Value, n.Depth }),
                    Hash = hash,
                    CapturedBy = userName,
                    CapturedDate = DateTime.UtcNow
                }, Formatting.None);
                var definitionBytes = Encoding.UTF8.GetBytes(definition);
                var note = await new CaseNoteRepository(dbContext, userName).InsertAsync(new CaseNote
                {
                    Note = string.IsNullOrWhiteSpace(request.Note)
                        ? string.Format(CultureInfo.InvariantCulture, strings[GraphResources.CaptureNote],
                            render.Graph.Nodes.Count, render.Graph.Edges.Count, render.Graph.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            render.Graph.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                        : request.Note,
                    CaseKey = existingCase.CaseKey,
                    CaseKeyValue = existingCase.CaseKeyValue,
                    CaseId = existingCase.Id,
                    SourceId = (int)CaseNoteSource.User,
                    KindId = (int)CaseNoteKind.Document,
                    Version = 1
                }, token).ConfigureAwait(false);
                var files = new CaseFileRepository(dbContext, userName);
                var svgFile = await files.InsertAsync(new CaseFile
                {
                    CaseNoteId = note.Id, Object = svgBytes, CaseKey = existingCase.CaseKey, CaseKeyValue = existingCase.CaseKeyValue,
                    CaseId = existingCase.Id, Extension = ".svg", Size = svgBytes.Length,
                    Name = $"network-capture-{request.CaseId}-{stamp}.svg", ContentType = "image/svg+xml", Hash = hash
                }, token).ConfigureAwait(false);
                var definitionFile = await files.InsertAsync(new CaseFile
                {
                    CaseNoteId = note.Id, Object = definitionBytes, CaseKey = existingCase.CaseKey, CaseKeyValue = existingCase.CaseKeyValue,
                    CaseId = existingCase.Id, Extension = ".json", Size = definitionBytes.Length,
                    Name = $"network-capture-{request.CaseId}-{stamp}.json", ContentType = "application/json",
                    Hash = Convert.ToHexStringLower(SHA256.HashData(definitionBytes))
                }, token).ConfigureAwait(false);
                op.Entity(svgFile.Id);
                op.Created();

                return new GraphCaptureResultDto
                {
                    SvgCaseFileId = svgFile.Id, DefinitionCaseFileId = definitionFile.Id, CaseNoteId = note.Id, Hash = hash,
                    Nodes = render.Graph.Nodes.Count, Edges = render.Graph.Edges.Count
                };
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
            catch (ArgumentException)
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
                log.Error($"Graph.Capture: unexpected failure user={userName} case={request?.CaseId}", ex);
                throw;
            }
        }

        [Description("Lists the caller's saved explorations on the Graph page, newest first. Explorations are private " +
                     "to the user who saved them. Read-only.")]
        [ServiceOperation("GraphExplorationList", OperationKind.Read, Idempotent = true)]
        public async Task<List<GraphExplorationDto>> GetExplorationsAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "ExplorationList", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();
                var rows = await Explorations().GetAsync(token).ConfigureAwait(false);
                op.Rows(rows.Count);

                return rows.Select(ToExploration).ToList();
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
                log.Error($"Graph.ExplorationList: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Saves an exploration of the Graph page for the caller: a name, the registry, the address that " +
                     "restores the view and optional pinned positions. An Id of 0 creates; otherwise the caller's own " +
                     "exploration is updated. Saving under an existing name on the same registry overwrites it.")]
        [ServiceOperation("GraphExplorationSave", OperationKind.Write, Idempotent = true)]
        public async Task<GraphExplorationDto> SaveExplorationAsync(GraphExplorationDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "ExplorationSave", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > MaximumExplorationName)
                {
                    throw new ArgumentException(strings[GraphResources.ExplorationNameInvalid]);
                }

                if (string.IsNullOrWhiteSpace(request.Address) || request.Address.Length > MaximumAddressLength)
                {
                    throw new ArgumentException(strings[GraphResources.ExplorationAddressInvalid]);
                }

                if (request.Pinned is { Length: > MaximumPinnedLength })
                {
                    throw new ArgumentException(strings[GraphResources.ExplorationPinnedInvalid]);
                }

                await ResolveRegistryAsync(request.GraphRegistryGuid, token).ConfigureAwait(false);
                var repository = Explorations();
                var existing = request.Id > 0
                    ? await repository.GetByIdAsync(request.Id, token).ConfigureAwait(false)
                    : await repository.GetByNameAsync(request.GraphRegistryGuid, request.Name.Trim(), token).ConfigureAwait(false);
                GraphExploration saved;

                if (existing == null)
                {
                    if (request.Id > 0)
                    {
                        throw new NotFoundException(strings[GraphResources.ExplorationNotFound]);
                    }

                    saved = await repository.InsertAsync(new GraphExploration
                    {
                        Name = request.Name.Trim(), GraphRegistryGuid = request.GraphRegistryGuid, Address = request.Address,
                        Pinned = string.IsNullOrWhiteSpace(request.Pinned) ? null : request.Pinned
                    }, token).ConfigureAwait(false);
                    op.Created();
                }
                else
                {
                    existing.Name = request.Name.Trim();
                    existing.Address = request.Address;
                    existing.Pinned = string.IsNullOrWhiteSpace(request.Pinned) ? null : request.Pinned;
                    existing.GraphRegistryGuid = request.GraphRegistryGuid;
                    saved = await repository.UpdateAsync(existing, token).ConfigureAwait(false);
                    op.Updated();
                }

                op.Entity(saved.Id);

                return ToExploration(saved);
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
            catch (ArgumentException)
            {
                op.Outcome("invalid");
                throw;
            }
            catch (KeyNotFoundException)
            {
                op.Outcome("not_found");
                throw new NotFoundException(strings[GraphResources.ExplorationNotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"Graph.ExplorationSave: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Removes one of the caller's saved explorations. Reversible only by direct database action.")]
        [ServiceOperation("GraphExplorationDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteExplorationAsync(
            [Description("Server-assigned identifier of the exploration to remove.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "ExplorationDelete", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();
                await Explorations().DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (KeyNotFoundException)
            {
                op.Outcome("not_found");
                throw new NotFoundException(strings[GraphResources.ExplorationNotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"Graph.ExplorationDelete: unexpected failure user={userName} id={id}", ex);
                throw;
            }
        }

        private GraphExplorationRepository Explorations()
        {
            return new GraphExplorationRepository(dbContext, tenantRegistryId, userName);
        }

        private static GraphExplorationDto ToExploration(GraphExploration row)
        {
            return new GraphExplorationDto
            {
                Id = row.Id, Name = row.Name, GraphRegistryGuid = row.GraphRegistryGuid, Address = row.Address,
                Pinned = row.Pinned, CreatedDate = Offset(row.CreatedDate),
                UpdatedDate = row.UpdatedDate.HasValue ? Offset(row.UpdatedDate.Value) : null
            };
        }

        private List<string> Sentences(GraphNodeDto node)
        {
            var sentences = new List<string>();

            if (node.Hub)
            {
                sentences.Add(string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentenceHub],
                    node.Key, node.Value, node.Degree));
            }
            else if (node.Degree > 0)
            {
                sentences.Add(string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentenceDegree],
                    node.Degree, node.DegreeMoney));
            }

            if (node.DistanceToConfirmed.HasValue)
            {
                sentences.Add(string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentenceDistance],
                    node.DistanceToConfirmed.Value, node.ConfirmedWithin3 ?? 0));
            }

            if (node.Cases.Open > 0)
            {
                sentences.Add(string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentenceOpenCases],
                    node.Cases.Open));
            }

            if (node.Cases.Confirmed > 0)
            {
                sentences.Add(string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentenceConfirmedCases],
                    node.Cases.Confirmed));
            }

            if (node.CommunityId.HasValue)
            {
                sentences.Add(string.Format(CultureInfo.InvariantCulture, strings[GraphResources.SentenceCommunity],
                    node.CommunityId.Value));
            }

            return sentences;
        }

        private async Task<(List<GetGraphNeighbourhoodQueryDto> Ordered, Dictionary<long, double> Interest)> RankByRelevanceAsync(
            long seedId, List<GetGraphNeighbourhoodQueryDto> rows, List<GetGraphNeighbourhoodQueryDto> reached,
            CancellationToken token)
        {
            var edgeIds = rows.Where(r => r.EdgeId.HasValue).Select(r => r.EdgeId!.Value).Distinct().ToArray();

            if (edgeIds.Length == 0)
            {
                return (reached, []);
            }

            var edges = await new GraphEdgeRepository(dbContext).GetByIdsAsync(tenantRegistryId, edgeIds, token)
                .ConfigureAwait(false);
            var graph = CompressedGraphBuilder.FromEdges(
                edges.Select(e => (e.FromNodeId, e.ToNodeId, Math.Max(e.Weight, 1e-6f), e.KindId)), true);

            if (!graph.TryGetIndex(seedId, out var seedIndex))
            {
                return (reached, []);
            }

            var nodes = new GraphNodeRepository(dbContext);
            var nodeRows = await nodes.GetByIdsAsync(tenantRegistryId, reached.Select(r => r.NodeId).ToList(), token)
                .ConfigureAwait(false);
            var cases = await CasesAsync(nodeRows, token).ConfigureAwait(false);
            var boosts = new Dictionary<int, double>();

            foreach (var node in nodeRows)
            {
                if (!graph.TryGetIndex(node.Id, out var index) || index == seedIndex)
                {
                    continue;
                }

                var boost = 0d;
                var found = cases.GetValueOrDefault((node.Key, node.Value));

                if (found is { Confirmed: > 0 })
                {
                    boost += 0.4;
                }

                if (node.DistanceToConfirmed == 1)
                {
                    boost += 0.2;
                }

                if (node.Hub == 1)
                {
                    boost -= 0.3;
                }

                if (boost != 0)
                {
                    boosts[index] = boost;
                }
            }

            var ranked = InterestPropagation.Compute(Egonet.FromSeed(graph, seedIndex), seedIndex, boosts, MaximumHops + 2,
                0.7, 0.05);
            var interest = ranked.ToDictionary(r => graph.NodeIds[r.Index], r => r.Interest);
            var rank = ranked.Select((r, i) => (graph.NodeIds[r.Index], i)).ToDictionary(x => x.Item1, x => x.i);
            var ordered = reached.OrderBy(r => r.NodeId == seedId ? -1 : rank.GetValueOrDefault(r.NodeId, int.MaxValue))
                .ThenBy(r => r.Depth).ThenBy(r => r.NodeId).ToList();

            return (ordered, interest);
        }

        private async Task<Dictionary<long, List<GraphNodeTagDto>>> TagsAsync(IReadOnlyCollection<long> nodeIds,
            CancellationToken token)
        {
            var rows = await new GraphNodeTagRepository(dbContext).GetByNodeIdsAsync(tenantRegistryId, nodeIds, token)
                .ConfigureAwait(false);

            return rows.GroupBy(r => r.GraphNodeId).ToDictionary(g => g.Key, g => g.Select(r => new GraphNodeTagDto
            {
                Tag = r.Tag, Distance = r.Distance, Within1 = r.Within1, Within2 = r.Within2, Within3 = r.Within3
            }).ToList());
        }

        private static GraphNodeDto WithTags(GraphNodeDto node, IReadOnlyDictionary<long, List<GraphNodeTagDto>> tags)
        {
            if (tags.TryGetValue(node.Id, out var list))
            {
                node.Tags = list;
                node.NearestTag = list[0].Tag;
                node.NearestTagDistance = list[0].Distance;
            }

            return node;
        }

        private async Task<List<GraphEdgeDto>> EdgesAsync(long[] edgeIds, ResolvedRegistry registry, DateTime from,
            DateTime to, CancellationToken token)
        {
            if (edgeIds.Length == 0)
            {
                return [];
            }

            var edges = await new GraphEdgeRepository(dbContext).GetByIdsAsync(tenantRegistryId, edgeIds, token)
                .ConfigureAwait(false);
            var windows = (await new GetGraphEdgeWindowQuery(dbContext).ExecuteAsync(edgeIds, from, to, ArchiveIdsPerEdge,
                    token).ConfigureAwait(false))
                .ToDictionary(x => x.GraphEdgeId);
            var names = registry.Definitions.ToDictionary(d => d.Guid, d => d.Name);

            return edges.OrderBy(e => e.Id)
                .Select(e => ToEdge(e, names.GetValueOrDefault(e.GraphRegistryEdgeGuid), windows.GetValueOrDefault(e.Id)))
                .ToList();
        }

        private Task<Dictionary<(string, string), GetGraphNodeCasesQueryDto>> CasesAsync(
            IReadOnlyList<GraphNode> nodes, CancellationToken token)
        {
            return CasesAsync(dbContext, tenantRegistryId, userName, nodes, token);
        }

        internal static async Task<Dictionary<(string, string), GetGraphNodeCasesQueryDto>> CasesAsync(DbContext dbContext,
            int tenantRegistryId, string userName, IReadOnlyList<GraphNode> nodes, CancellationToken token)
        {
            if (nodes.Count == 0)
            {
                return [];
            }

            var distinct = nodes.Select(n => (n.Key, n.Value)).Distinct().ToList();
            var rows = await new GetGraphNodeCasesQuery(dbContext).ExecuteAsync(tenantRegistryId, userName,
                    distinct.Select(d => d.Key).ToArray(), distinct.Select(d => d.Value).ToArray(), token)
                .ConfigureAwait(false);

            return rows.ToDictionary(r => (r.CaseKey, r.CaseKeyValue));
        }

        private async Task<DateTimeOffset?> BuiltDateAsync(ResolvedRegistry registry, CancellationToken token)
        {
            var guids = registry.Definitions.Select(d => d.Guid).ToHashSet();
            var states = await new GraphBuildStateRepository(dbContext).GetByTenantAsync(tenantRegistryId, token)
                .ConfigureAwait(false);
            var newest = states.Where(s => guids.Contains(s.GraphRegistryEdgeGuid) && s.LastRunDate.HasValue)
                .Select(s => s.LastRunDate!.Value).DefaultIfEmpty().Max();

            return newest == default ? null : Offset(newest);
        }

        private async Task<ResolvedRegistry> ResolveRegistryAsync(Guid guid, CancellationToken token)
        {
            var repository = new GraphRegistryRepository(dbContext, userName);
            var registry = await repository.GetByGuidAsync(guid, token).ConfigureAwait(false);

            if (registry == null || registry.Active != 1)
            {
                throw new NotFoundException(strings[GraphResources.RegistryNotFound]);
            }

            var granted = await repository.IsGrantedAsync(guid, token).ConfigureAwait(false);
            var definitions = (await new GraphRegistryEdgeRepository(dbContext, tenantRegistryId)
                .GetActiveByGraphRegistryGuidAsync(guid, token).ConfigureAwait(false)).ToList();
            return new ResolvedRegistry(registry, definitions, granted);
        }

        private Guid[] SelectDefinitions(ResolvedRegistry registry, string? edges)
        {
            var visible = registry.Definitions.Where(d => d.Restricted != 1 || registry.Granted).ToList();

            if (string.IsNullOrWhiteSpace(edges))
            {
                return visible.Select(d => d.Guid).ToArray();
            }

            var requested = new HashSet<Guid>();

            foreach (var part in edges.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Guid.TryParse(part, out var parsed))
                {
                    throw new ArgumentException(strings[GraphResources.EdgesInvalid]);
                }

                requested.Add(parsed);
            }

            return visible.Where(d => requested.Contains(d.Guid)).Select(d => d.Guid).ToArray();
        }

        private (DateTime From, DateTime To) Window(DateTime? from, DateTime? to, int defaultDays)
        {
            var end = to.HasValue ? DateTime.SpecifyKind(to.Value, DateTimeKind.Utc) : DateTime.UtcNow;
            var start = from.HasValue
                ? DateTime.SpecifyKind(from.Value, DateTimeKind.Utc)
                : end.AddDays(-Math.Max(1, defaultDays));

            if (end <= start)
            {
                throw new ArgumentException(strings[GraphResources.WindowInvalid]);
            }

            return (start, end);
        }

        private (string Key, string Value) RequireKey(string? key, string? value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(strings[GraphResources.KeyRequired]);
            }

            if (key.Length > MaximumKeyLength || value.Length > MaximumValueLength)
            {
                throw new ArgumentException(strings[GraphResources.KeyTooLong]);
            }

            return (key.Trim(), value.Trim());
        }

        private static IEnumerable<Guid> ParseGuids(string? list)
        {
            if (string.IsNullOrWhiteSpace(list))
            {
                yield break;
            }

            foreach (var part in list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(part, out var guid))
                {
                    yield return guid;
                }
            }
        }

        private static IEnumerable<long> ParseLongs(string? list)
        {
            if (string.IsNullOrWhiteSpace(list))
            {
                yield break;
            }

            foreach (var part in list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (long.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                {
                    yield return id;
                }
            }
        }

        internal static GraphNodeDto ToNode(GraphNode node, int depth, long? via, bool seed,
            IReadOnlyDictionary<(string, string), GetGraphNodeCasesQueryDto> cases)
        {
            var found = cases.GetValueOrDefault((node.Key, node.Value));

            return new GraphNodeDto
            {
                Id = node.Id,
                Key = node.Key,
                Value = node.Value,
                Hub = node.Hub == 1,
                HubOverrideId = node.HubOverrideId,
                Degree = node.Degree,
                DegreeMoney = node.DegreeMoney,
                DegreeIdentity = node.DegreeIdentity,
                HubNeighbourCount = node.HubNeighbourCount,
                ComponentId = node.ComponentId,
                ComponentSize = node.ComponentSize,
                CommunityId = node.CommunityId,
                CoreNumber = node.CoreNumber,
                PageRank = node.PageRank,
                ClusteringCoefficient = node.ClusteringCoefficient,
                DistanceToConfirmed = node.DistanceToConfirmed,
                ConfirmedWithin1 = node.ConfirmedWithin1,
                ConfirmedWithin2 = node.ConfirmedWithin2,
                ConfirmedWithin3 = node.ConfirmedWithin3,
                PropagationScore = node.PropagationScore,
                HubScore = node.HubScore,
                AuthorityScore = node.AuthorityScore,
                DistanceToNotConfirmed = node.DistanceToNotConfirmed,
                ComputedDate = node.ComputedDate.HasValue ? Offset(node.ComputedDate.Value) : null,
                FirstSeen = Offset(node.FirstSeen),
                LastSeen = Offset(node.LastSeen),
                Depth = depth,
                ViaNodeId = via,
                Seed = seed,
                Cases = found == null
                    ? new GraphNodeCasesDto()
                    : new GraphNodeCasesDto
                    {
                        Open = found.Open, Closed = found.Closed, Confirmed = found.Confirmed,
                        LatestCaseId = found.LatestCaseId, LatestOpenCaseId = found.LatestOpenCaseId
                    }
            };
        }

        private static GraphEdgeDefinitionDto ToDefinition(GraphRegistryEdge definition, ResolvedRegistry registry)
        {
            return new GraphEdgeDefinitionDto
            {
                Guid = definition.Guid, Name = definition.Name, KindId = definition.KindId ?? 1,
                Restricted = definition.Restricted == 1,
                Visible = definition.Restricted != 1 || registry.Granted,
                IconName = definition.IconName,
                FromNodeKey = definition.FromNodeKey ?? definition.FromKey,
                ToNodeKey = definition.ToNodeKey ?? definition.ToKey,
                FromNodeIcon = definition.FromNodeIcon,
                ToNodeIcon = definition.ToNodeIcon
            };
        }

        private static GraphEdgeDto ToEdge(GraphEdge edge, string? definitionName, GetGraphEdgeWindowQueryDto? window)
        {
            return new GraphEdgeDto
            {
                Id = edge.Id,
                DefinitionGuid = edge.GraphRegistryEdgeGuid,
                DefinitionName = definitionName,
                KindId = edge.KindId,
                FromNodeId = edge.FromNodeId,
                ToNodeId = edge.ToNodeId,
                Count = edge.Count,
                AmountSum = edge.AmountSum,
                WindowCount = window?.WindowCount ?? 0,
                WindowAmount = window?.WindowAmount ?? 0,
                FirstSeen = Offset(edge.FirstSeen),
                LastSeen = Offset(edge.LastSeen),
                WindowFirst = window?.WindowFirst.HasValue == true ? Offset(window.WindowFirst.Value) : null,
                WindowLast = window?.WindowLast.HasValue == true ? Offset(window.WindowLast.Value) : null,
                Weight = edge.Weight,
                Specificity = edge.Specificity,
                Restricted = edge.Restricted == 1,
                ArchiveIds = ParseLongs(window?.ArchiveIds).ToList()
            };
        }

        private static DateTimeOffset Offset(DateTime value)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }

        private void EnsurePermitted()
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"Graph: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[GraphResources.PermissionDenied], permissions);
        }

        private sealed record ResolvedRegistry(GraphRegistry Registry, List<GraphRegistryEdge> Definitions,
            bool Granted);
    }
}
