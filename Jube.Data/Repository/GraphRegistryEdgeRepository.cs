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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Concurrency;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;
    using Poco;

    public class GraphRegistryEdgeRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public GraphRegistryEdgeRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public GraphRegistryEdgeRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public Task<GraphRegistryEdge> GetByNameGraphRegistryIdAsync(string name, int graphRegistryId,
            CancellationToken token = default)
        {
            return dbContext.GraphRegistryEdge.FirstOrDefaultAsync(f =>
                f.GraphRegistry.TenantRegistryId == tenantRegistryId
                && f.GraphRegistryId == graphRegistryId
                && (f.Deleted == 0 || f.Deleted == null)
                && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<GraphRegistryEdge>> GetOrderByIdAsync(CancellationToken token = default)
        {
            return await dbContext.GraphRegistryEdge
                .Where(w => w.GraphRegistry.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task<IEnumerable<GraphRegistryEdge>> GetByGraphRegistryIdOrderByIdAsync(int graphRegistryId,
            CancellationToken token = default)
        {
            return await dbContext.GraphRegistryEdge
                .Where(w => w.GraphRegistry.TenantRegistryId == tenantRegistryId
                            && w.GraphRegistryId == graphRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task<IEnumerable<GraphRegistryEdge>> GetActiveByGraphRegistryGuidAsync(Guid graphRegistryGuid,
            CancellationToken token = default)
        {
            return await dbContext.GraphRegistryEdge
                .Where(w => w.GraphRegistry.TenantRegistryId == tenantRegistryId
                            && w.GraphRegistry.Guid == graphRegistryGuid
                            && w.GraphRegistry.Active == 1
                            && (w.GraphRegistry.Deleted == 0 || w.GraphRegistry.Deleted == null)
                            && w.Active == 1
                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task<IEnumerable<GraphRegistryEdge>> GetActiveForTenantAsync(CancellationToken token = default)
        {
            return await dbContext.GraphRegistryEdge
                .Where(w => w.GraphRegistry.TenantRegistryId == tenantRegistryId
                            && w.GraphRegistry.Active == 1
                            && (w.GraphRegistry.Deleted == 0 || w.GraphRegistry.Deleted == null)
                            && w.Active == 1
                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public Task<GraphRegistryEdge> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.GraphRegistryEdge.FirstOrDefaultAsync(w => w.Id == id
                                                                        && w.GraphRegistry.TenantRegistryId ==
                                                                        tenantRegistryId
                                                                        && (w.Deleted == 0 || w.Deleted == null),
                token);
        }

        public Task<GraphRegistryEdge> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.GraphRegistryEdge.FirstOrDefaultAsync(w => w.Guid == guid
                                                                        && w.GraphRegistry.TenantRegistryId ==
                                                                        tenantRegistryId
                                                                        && (w.Deleted == 0 || w.Deleted == null),
                token);
        }

        public Task<bool> ExistsGraphRegistryAsync(int graphRegistryId, CancellationToken token = default)
        {
            return dbContext.GraphRegistry.AnyAsync(w => w.Id == graphRegistryId
                                                         && w.TenantRegistryId == tenantRegistryId
                                                         && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<GraphRegistryEdge> InsertAsync(GraphRegistryEdge model, CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task<GraphRegistryEdge> UpdateAsync(GraphRegistryEdge model, CancellationToken token = default)
        {
            var existing = await dbContext.GraphRegistryEdge.FirstOrDefaultAsync(w => w.Id == model.Id
                && w.GraphRegistry.TenantRegistryId == tenantRegistryId
                && (w.Deleted == 0 || w.Deleted == null)
                && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.CreatedUser = existing.CreatedUser;
            model.CreatedDate = existing.CreatedDate;
            model.UpdatedUser = userName;
            model.UpdatedDate = DateTime.UtcNow;
            model.GraphRegistryId = existing.GraphRegistryId;
            OptimisticConcurrency.Verify(model.Version, existing.Version);
            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;
            model.ImportId = existing.ImportId;

            await dbContext.UpdateAsync(model, token: token);

            var restricted = model.Restricted ?? 0;

            if ((existing.Restricted ?? 0) != restricted)
            {
                var tenant = tenantRegistryId;
                var guid = existing.Guid;
                await dbContext.GraphEdge
                    .Where(e => e.TenantRegistryId == tenant && e.GraphRegistryEdgeGuid == guid && e.Restricted != restricted)
                    .Set(e => e.Restricted, restricted).UpdateAsync(token);
            }

            var audit = new GraphRegistryEdgeVersion
            {
                GraphRegistryEdgeId = existing.Id,
                GraphRegistryId = existing.GraphRegistryId,
                Name = existing.Name,
                KindId = existing.KindId,
                FromKey = existing.FromKey,
                ToKey = existing.ToKey,
                FromNodeKey = existing.FromNodeKey,
                ToNodeKey = existing.ToNodeKey,
                IconName = existing.IconName,
                FromNodeIcon = existing.FromNodeIcon,
                ToNodeIcon = existing.ToNodeIcon,
                AmountKey = existing.AmountKey,
                HalfLifeDays = existing.HalfLifeDays,
                KindPrior = existing.KindPrior,
                HubThreshold = existing.HubThreshold,
                MinimumAmount = existing.MinimumAmount,
                Hard = existing.Hard,
                Restricted = existing.Restricted,
                Active = existing.Active,
                Locked = existing.Locked,
                CreatedUser = existing.CreatedUser,
                CreatedDate = existing.CreatedDate,
                UpdatedUser = existing.UpdatedUser,
                UpdatedDate = existing.UpdatedDate,
                Deleted = existing.Deleted,
                DeletedUser = existing.DeletedUser,
                DeletedDate = existing.DeletedDate,
                Version = existing.Version,
                InheritedId = existing.InheritedId,
                ImportId = existing.ImportId
            };

            await dbContext.InsertAsync(audit, token: token);

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var record = await dbContext.GraphRegistryEdge
                .FirstOrDefaultAsync(w => w.GraphRegistry.TenantRegistryId == tenantRegistryId
                                          && w.Id == id
                                          && (w.Deleted == 0 || w.Deleted == null)
                                          && (w.Locked == 0 || w.Locked == null), token);

            if (record == null)
            {
                throw new KeyNotFoundException();
            }

            record.DeletedUser = userName;
            record.DeletedDate = DateTime.UtcNow;
            record.Deleted = 1;
            await dbContext.UpdateAsync(record, token: token);
        }
    }
}
