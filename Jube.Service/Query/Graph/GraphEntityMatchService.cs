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

using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Query.Graph;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using LinqToDB;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Service.Query.Graph
{
    public sealed class GraphEntityMatchService
    {
        public const int MaximumList = 500;
        public const int MaximumNote = 500;
        private static readonly int[] readPermissions = [49, 1, 51];
        private static readonly int[] decidePermissions = [51];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;

        private GraphEntityMatchService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
        }

        public static async Task<GraphEntityMatchService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var tenant = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName, token)
                .ConfigureAwait(false);

            if (tenant is null)
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var validation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new GraphEntityMatchService(dbContext, userName, tenant.Value, validation, log,
                LogManager.GetLogger("Jube.Audit"), serviceChangeBus);
        }

        private void Ensure(int[] required)
        {
            if (!permissionValidation.Validate(required))
            {
                throw new CaseExceptions.ForbiddenException("Permission denied.", required);
            }
        }

        public async Task<List<GraphEntityMatchDto>> ListAsync(int? decisionId, int? limit,
            CancellationToken token = default)
        {
            Ensure(readPermissions);

            if (decisionId is { } d && !Enum.IsDefined((GraphEntityMatchDecision)d))
            {
                throw new ArgumentException("The decision must be 1, 2 or 3.");
            }

            var rows = await new GraphEntityMatchRepository(dbContext).ListAsync(tenantRegistryId,
                decisionId is null ? null : (short)decisionId.Value, Math.Clamp(limit ?? 100, 1, MaximumList), token)
                .ConfigureAwait(false);

            return await MapAsync(rows, token).ConfigureAwait(false);
        }

        public async Task<List<GraphEntityMatchDto>> ListForNodeAsync(long nodeId, CancellationToken token = default)
        {
            Ensure(readPermissions);

            var rows = await new GraphEntityMatchRepository(dbContext)
                .ListForNodeAsync(tenantRegistryId, nodeId, token).ConfigureAwait(false);

            return await MapAsync(rows, token).ConfigureAwait(false);
        }

        public async Task<GraphEntityMatchDto> DecideAsync(GraphEntityMatchDecideDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphEntityMatch", "Decide", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure(decidePermissions);

                if (!Enum.IsDefined((GraphEntityMatchDecision)model.DecisionId))
                {
                    throw new ArgumentException("The decision must be 1, 2 or 3.");
                }

                var note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();

                if (note is { Length: > MaximumNote })
                {
                    throw new ArgumentException($"The note must be at most {MaximumNote} characters.");
                }

                var repository = new GraphEntityMatchRepository(dbContext);
                var current = await repository.GetAsync(tenantRegistryId, model.Id, token).ConfigureAwait(false);

                if (current is null)
                {
                    throw new CaseExceptions.NotFoundException("The candidate was not found.");
                }

                if (current.DecisionId == model.DecisionId)
                {
                    throw new CaseExceptions.ConflictException("The candidate already has that decision.");
                }

                var visible = await VisibleNodeIdsAsync([current.NodeIdA, current.NodeIdB], token).ConfigureAwait(false);

                if (visible.Count != 2)
                {
                    throw new CaseExceptions.NotFoundException("The candidate was not found.");
                }

                await repository.DecideAsync(tenantRegistryId, model.Id, (short)model.DecisionId,
                    model.DecisionId == (int)GraphEntityMatchDecision.Pending ? null : userName,
                    model.DecisionId == (int)GraphEntityMatchDecision.Pending ? null : note, token)
                    .ConfigureAwait(false);

                if (model.DecisionId == (int)GraphEntityMatchDecision.Pending)
                {
                    await dbContext.GraphEntityMatch.Where(w => w.Id == model.Id)
                        .Set(s => s.DecidedDate, (DateTime?)null).UpdateAsync(token).ConfigureAwait(false);
                }

                await repository.RecomputeResolutionAsync(tenantRegistryId, token).ConfigureAwait(false);

                op.Entity((int)model.Id);
                op.Updated();

                var updated = await repository.GetAsync(tenantRegistryId, model.Id, token).ConfigureAwait(false);

                return (await MapAsync([updated], token).ConfigureAwait(false)).Single();
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        private async Task<List<long>> VisibleNodeIdsAsync(IReadOnlyCollection<long> ids, CancellationToken token)
        {
            var nodes = await new GraphNodeRepository(dbContext).GetByIdsAsync(tenantRegistryId, ids, token)
                .ConfigureAwait(false);

            return (await GraphViewerNodes.VisibleAsync(dbContext, tenantRegistryId, userName, nodes, token)
                .ConfigureAwait(false)).Select(n => n.Id).ToList();
        }

        private async Task<List<GraphEntityMatchDto>> MapAsync(List<GraphEntityMatch> rows, CancellationToken token)
        {
            if (rows.Count == 0)
            {
                return [];
            }

            var ids = rows.SelectMany(r => new[] { r.NodeIdA, r.NodeIdB }).Distinct().ToList();
            var nodes = await new GraphNodeRepository(dbContext).GetByIdsAsync(tenantRegistryId, ids, token)
                .ConfigureAwait(false);

            var visible = (await GraphViewerNodes.VisibleAsync(dbContext, tenantRegistryId, userName, nodes, token)
                .ConfigureAwait(false)).ToDictionary(n => n.Id);

            return
            [
                .. from r in rows
                where visible.ContainsKey(r.NodeIdA) && visible.ContainsKey(r.NodeIdB)
                select new GraphEntityMatchDto
                {
                    Id = r.Id,
                    NodeIdA = r.NodeIdA,
                    KeyA = visible[r.NodeIdA].Key,
                    ValueA = visible[r.NodeIdA].Value,
                    NodeIdB = r.NodeIdB,
                    KeyB = visible[r.NodeIdB].Key,
                    ValueB = visible[r.NodeIdB].Value,
                    SourceId = r.SourceId,
                    Posterior = r.Posterior,
                    Evidence = r.Evidence,
                    DecisionId = r.DecisionId,
                    DecidedUser = r.DecidedUser,
                    DecidedDate = r.DecidedDate is null
                        ? null
                        : new DateTimeOffset(DateTime.SpecifyKind(r.DecidedDate.Value, DateTimeKind.Utc)),
                    DecisionNote = r.DecisionNote,
                    ResolvedEntityId = r.ResolvedEntityId,
                    CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(r.CreatedDate, DateTimeKind.Utc))
                }
            ];
        }
    }
}
