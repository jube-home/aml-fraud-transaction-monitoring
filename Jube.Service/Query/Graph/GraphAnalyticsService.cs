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
using Jube.Dto.Query.Graph;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.Graph;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.Graph
{
    public sealed class GraphAnalyticsService
    {
        public const int MaximumPatterns = 500;
        public const int DefaultPatterns = 100;
        public const int MaximumCommunityMembers = 50;
        public const int MaximumHubs = 500;
        private static readonly int[] permissions = [49, 1];
        private static readonly int[] resolvePermissions = [51];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private GraphAnalyticsService(DbContext dbContext, string userName, int tenantRegistryId,
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
            this.dbContext.CommandTimeout = GraphService.QueryTimeoutSeconds;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static IReadOnlyList<int> ResolvePermissions => resolvePermissions;

        public static Task<GraphAnalyticsService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<GraphAnalyticsService> CreateAsync(DbContext dbContext, string? userName, ILog log,
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

            return new GraphAnalyticsService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the money-flow patterns the detectors found on the tenant graph (fan-in, fan-out, " +
                     "gather-scatter, scatter-gather, temporal cycle, pass-through, smurfing, structuring window, dense " +
                     "block), newest first, with the anchor node, score, window and archive ids as evidence. Read-only.")]
        [ServiceOperation("GraphPatternList", OperationKind.Read, Idempotent = true)]
        public async Task<List<GraphPatternDto>> GetPatternsAsync(GraphPatternListRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "PatternList", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted(permissions);
                var limit = Math.Clamp(request.Limit ?? DefaultPatterns, 1, MaximumPatterns);
                var patterns = await new GraphPatternRepository(dbContext).ListAsync(tenantRegistryId,
                    request.StateId.HasValue ? (byte)request.StateId.Value : null,
                    request.KindId.HasValue ? (byte)request.KindId.Value : null, limit, token).ConfigureAwait(false);
                var anchors = await NodesAsync(patterns.Select(p => p.AnchorNodeId).ToList(), token).ConfigureAwait(false);
                var result = patterns.Select(p => GraphAnalyticsMapper.ToPattern(p, anchors.GetValueOrDefault(p.AnchorNodeId), strings))
                    .ToList();
                op.Rows(result.Count);

                return result;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
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
                log.Error($"Graph.PatternList: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns one detected pattern with its members and their roles, the anchor node and the archive " +
                     "ids of the transactions that form it. Read-only.")]
        [ServiceOperation("GraphPatternGet", OperationKind.Read, Idempotent = true)]
        public async Task<GraphPatternDto> GetPatternAsync(long id, CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "PatternGet", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted(permissions);
                var repository = new GraphPatternRepository(dbContext);
                var pattern = await repository.GetByIdAsync(tenantRegistryId, id, token).ConfigureAwait(false)
                              ?? throw new NotFoundException(strings[GraphResources.PatternNotFound]);
                var members = await repository.GetMembersAsync(pattern.Id, token).ConfigureAwait(false);
                var nodes = await NodesAsync(members.Select(m => m.GraphNodeId).Append(pattern.AnchorNodeId).ToList(), token)
                    .ConfigureAwait(false);
                var result = GraphAnalyticsMapper.ToPattern(pattern, nodes.GetValueOrDefault(pattern.AnchorNodeId), strings);
                result.Members = members.OrderBy(m => m.Role).ThenBy(m => m.GraphNodeId)
                    .Where(m => nodes.ContainsKey(m.GraphNodeId))
                    .Select(m => new GraphPatternMemberDto { Node = nodes[m.GraphNodeId], Role = m.Role }).ToList();
                op.Rows(result.Members.Count);

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
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"Graph.PatternGet: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Records a decision on a detected pattern: Seen, Dismissed, or Cased with the case it was " +
                     "raised into. Needs the Resolve Graph Entities and Patterns permission.")]
        [ServiceOperation("GraphPatternDecide", OperationKind.Write, Idempotent = true)]
        public async Task<GraphPatternDto> DecidePatternAsync(GraphPatternDecideRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "PatternDecide", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted(resolvePermissions);

                if (request.StateId is < 2 or > 4 || (request.StateId == 4 && !request.CaseId.HasValue))
                {
                    throw new ArgumentException(strings[GraphResources.PatternStateInvalid]);
                }

                if (request.StateId == 4 && await new CaseRepository(dbContext, userName)
                        .GetByIdActiveOnlyAsync(request.CaseId!.Value, token).ConfigureAwait(false) == null)
                {
                    throw new NotFoundException(strings[GraphResources.CaseNotFound]);
                }

                var repository = new GraphPatternRepository(dbContext);
                var updated = await repository.DecideAsync(tenantRegistryId, request.Id, (byte)request.StateId, userName,
                    request.StateId == 4 ? request.CaseId : null, token).ConfigureAwait(false);

                if (updated == 0)
                {
                    throw new NotFoundException(strings[GraphResources.PatternNotFound]);
                }

                op.Entity((int)request.Id);
                op.Updated();

                return await GetPatternAsync(request.Id, token).ConfigureAwait(false);
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
                log.Error($"Graph.PatternDecide: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the communities of the tenant graph found by the latest community cycle, riskiest first: " +
                     "size, density, confirmed and closed case counts among members and the ratio to the tenant " +
                     "confirmed rate. Read-only.")]
        [ServiceOperation("GraphCommunityList", OperationKind.Read, Idempotent = true)]
        public async Task<List<GraphCommunityDto>> GetCommunitiesAsync(int? limit, CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "CommunityList", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted(permissions);
                var rows = await new GraphCommunityRepository(dbContext).GetTopAsync(tenantRegistryId,
                    Math.Clamp(limit ?? DefaultPatterns, 1, MaximumPatterns), token).ConfigureAwait(false);
                var result = rows.Select(c => GraphAnalyticsMapper.ToCommunity(c, strings)).ToList();
                op.Rows(result.Count);

                return result;
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
                log.Error($"Graph.CommunityList: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns one community with its most central members by PageRank. Read-only.")]
        [ServiceOperation("GraphCommunityGet", OperationKind.Read, Idempotent = true)]
        public async Task<GraphCommunityDto> GetCommunityAsync(long id, CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "CommunityGet", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted(permissions);
                var community = await new GraphCommunityRepository(dbContext).GetByIdAsync(tenantRegistryId, id, token)
                                    .ConfigureAwait(false)
                                ?? throw new NotFoundException(strings[GraphResources.CommunityNotFound]);
                var result = GraphAnalyticsMapper.ToCommunity(community, strings);
                var members = await dbContext.GraphNode
                    .Where(n => n.TenantRegistryId == tenantRegistryId && n.CommunityId == id)
                    .OrderByDescending(n => n.PageRank).ThenBy(n => n.Id).Take(MaximumCommunityMembers).ToListAsync(token)
                    .ConfigureAwait(false);
                result.Members = await ToNodesAsync(members, token).ConfigureAwait(false);
                op.Rows(result.Members.Count);

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
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"Graph.CommunityGet: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the nodes the hub cycle marked as hubs (shared devices, public networks, payroll " +
                     "accounts) with their degrees and overrides, highest degree first. Read-only.")]
        [ServiceOperation("GraphHubList", OperationKind.Read, Idempotent = true)]
        public async Task<List<GraphNodeDto>> GetHubsAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "HubList", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted(permissions);
                var hubs = await new GraphNodeRepository(dbContext).GetHubsAsync(tenantRegistryId, MaximumHubs, token)
                    .ConfigureAwait(false);
                var result = await ToNodesAsync(hubs, token).ConfigureAwait(false);
                op.Rows(result.Count);

                return result;
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
                log.Error($"Graph.HubList: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Overrides whether a node is a hub: never, always, or back to the hub cycle's decision. " +
                     "Takes effect on the node at once and on its edges at the next hub cycle. Needs the Resolve " +
                     "Graph Entities and Patterns permission.")]
        [ServiceOperation("GraphHubOverride", OperationKind.Write, Idempotent = true)]
        public async Task<GraphNodeDto> OverrideHubAsync(GraphHubOverrideRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Graph", "HubOverride", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted(resolvePermissions);

                if (request.OverrideId is < 0 or > 2)
                {
                    throw new ArgumentException(strings[GraphResources.HubOverrideInvalid]);
                }

                var nodes = new GraphNodeRepository(dbContext);
                var updated = await nodes.SetHubOverrideAsync(tenantRegistryId, request.NodeId, (byte)request.OverrideId,
                    token).ConfigureAwait(false);

                if (updated == 0)
                {
                    throw new NotFoundException(strings[GraphResources.NodeNotFound]);
                }

                var node = await nodes.GetByIdAsync(tenantRegistryId, request.NodeId, token).ConfigureAwait(false);
                op.Entity((int)request.NodeId);
                op.Updated();

                var shown = await ToNodesAsync([node], token).ConfigureAwait(false);

                return shown.Count == 0 ? throw new NotFoundException(strings[GraphResources.NodeNotFound]) : shown[0];
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
                log.Error($"Graph.HubOverride: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<Dictionary<long, GraphNodeDto>> NodesAsync(IReadOnlyCollection<long> ids, CancellationToken token)
        {
            if (ids.Count == 0)
            {
                return [];
            }

            var rows = await new GraphNodeRepository(dbContext).GetByIdsAsync(tenantRegistryId, ids.Distinct().ToList(), token)
                .ConfigureAwait(false);

            return (await ToNodesAsync(rows, token).ConfigureAwait(false)).ToDictionary(n => n.Id);
        }

        private async Task<List<GraphNodeDto>> ToNodesAsync(IReadOnlyList<GraphNode> rows, CancellationToken token)
        {
            var visible = await GraphViewerNodes.VisibleAsync(dbContext, tenantRegistryId, userName, rows, token)
                .ConfigureAwait(false);
            var cases = await GraphService.CasesAsync(dbContext, tenantRegistryId, userName, visible, token).ConfigureAwait(false);

            return visible.Select(n => GraphService.ToNode(n, 0, null, false, cases)).ToList();
        }

        private void EnsurePermitted(int[] required)
        {
            if (permissionValidation.Validate(required))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"Graph: permission denied user={userName} specs=[{string.Join(",", required)}]");
            }

            throw new ForbiddenException(strings[GraphResources.PermissionDenied], required);
        }
    }
}
