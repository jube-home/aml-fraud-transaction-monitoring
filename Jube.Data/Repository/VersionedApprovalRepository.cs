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
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using Context;
    using LinqToDB;
    using Microsoft.Extensions.Logging.Abstractions;
    using Models;
    using Poco;
    using Query;
    using Query.GetApprovedEntityQuery;
    using Query.Models;

    public abstract class VersionedApprovalRepository<TEntity, TVersion>(
        DbContext dbContext,
        EntityApprovalKind kind,
        string versionParentIdPropertyName,
        int? tenantRegistryId,
        string userName)
        where TEntity : class
        where TVersion : class
    {
        private const string IdPropertyName = "Id";
        private const string VersionPropertyName = "Version";
        private const string GuidPropertyName = "Guid";
        private const string CreatedDatePropertyName = "CreatedDate";
        private const string CreatedUserPropertyName = "CreatedUser";

        private static readonly IMapper archiveMapper = new Mapper(new MapperConfiguration(
            cfg => { cfg.CreateMap<TEntity, TVersion>(); },
            NullLoggerFactory.Instance));

        protected readonly DbContext DbContext = dbContext;
        protected readonly EntityApprovalKind Kind = kind;
        protected readonly int? TenantRegistryId = tenantRegistryId;
        protected readonly string UserName = userName;

        public async Task<TEntity> ResubmitAsync(int id, string note = null, CancellationToken token = default)
        {
            var existing = await LoadForMutationAsync(id, token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var currentVersion = VersionOf(existing);
            var rows = await ApprovalRowsAsync(id, token).ConfigureAwait(false);

            if (!EntityApprovalResolver.IsRejected(rows, currentVersion))
            {
                throw new InvalidOperationException("Only a rejected version can be resubmitted.");
            }

            await ArchiveAsync(existing, token).ConfigureAwait(false);

            SetVersion(existing, currentVersion + 1);
            SetCreatedUser(existing, UserName);
            SetCreatedDate(existing, DateTime.UtcNow);

            await DbContext.UpdateAsync(existing, token: token).ConfigureAwait(false);

            var approvalRepository = TenantRegistryId.HasValue
                ? new EntityApprovalRepository(DbContext, TenantRegistryId.Value)
                : new EntityApprovalRepository(DbContext);

            await approvalRepository.ResubmitAsync(Kind, id, currentVersion, UserName, note, token)
                .ConfigureAwait(false);

            return existing;
        }

        public async Task<TEntity> RetractAsync(int id, string note = null, int approvalsRequired = 1,
            CancellationToken token = default)
        {
            var existing = await LoadForMutationAsync(id, token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var currentVersion = VersionOf(existing);
            var rows = await ApprovalRowsAsync(id, token).ConfigureAwait(false);

            if (!EntityApprovalResolver.IsRejected(rows, currentVersion))
            {
                throw new InvalidOperationException("Only a rejected version can be retracted.");
            }

            var baselineVersion = EntityApprovalResolver.EffectiveVersion(rows, currentVersion, approvalsRequired);

            if (!baselineVersion.HasValue)
            {
                throw new InvalidOperationException("There is no approved version to retract to.");
            }

            var baselineVersionValue = baselineVersion.Value;

            var query = new GetApprovedEntityQuery<TEntity, TVersion>(DbContext, Kind,
                versionParentIdPropertyName, TenantRegistryId);

            var candidates = await DbContext.GetTable<TVersion>()
                .Where(query.ParentIdInPredicate([id]))
                .ToListAsync(token).ConfigureAwait(false);

            var baselineSnapshot = candidates.FirstOrDefault(c =>
                GetApprovedEntityQuery<TEntity, TVersion>.VersionNumberOf(c) == baselineVersionValue);

            if (baselineSnapshot == null)
            {
                throw new InvalidOperationException("There is no approved version to retract to.");
            }

            var restored = GetApprovedEntityQuery<TEntity, TVersion>.MapVersionToEntity(baselineSnapshot, id);
            var newVersion = currentVersion + 1;

            await ArchiveAsync(existing, token).ConfigureAwait(false);

            SetId(restored, id);
            SetVersion(restored, newVersion);
            SetGuid(restored, GuidOf(existing));
            SetCreatedUser(restored, UserName);
            SetCreatedDate(restored, DateTime.UtcNow);

            await DbContext.UpdateAsync(restored, token: token).ConfigureAwait(false);

            var approvalRepository = TenantRegistryId.HasValue
                ? new EntityApprovalRepository(DbContext, TenantRegistryId.Value)
                : new EntityApprovalRepository(DbContext);

            await approvalRepository.RetractAsync(Kind, id, currentVersion, UserName, note, token)
                .ConfigureAwait(false);

            var carriedApprovers = rows
                .Where(r => r.EntityVersion == baselineVersionValue && r.State == EntityApprovalState.Approved)
                .Select(r => r.CreatedUser)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            await approvalRepository.InsertManyAsync(carriedApprovers.Select(approver => new EntityApproval
            {
                EntityApprovalKindId = (int)Kind,
                EntityId = id,
                EntityVersion = newVersion,
                StateId = (int)EntityApprovalState.Approved,
                CreatedUser = approver,
                CreatedDate = DateTime.UtcNow,
                CarriedForwardFromVersion = baselineVersionValue
            }), token).ConfigureAwait(false);

            return restored;
        }

        protected abstract Task<TEntity> LoadForMutationAsync(int id, CancellationToken token);

        protected Task ArchiveAsync(TEntity existing, CancellationToken token)
        {
            var audit = archiveMapper.Map<TVersion>(existing);
            SetValue(typeof(TVersion).GetProperty(versionParentIdPropertyName), audit, IdOf(existing));
            return DbContext.InsertAsync(audit, token: token);
        }

        private async Task<List<EntityApprovalRow>> ApprovalRowsAsync(int id, CancellationToken token)
        {
            var approvalRepository = TenantRegistryId.HasValue
                ? new EntityApprovalRepository(DbContext, TenantRegistryId.Value)
                : new EntityApprovalRepository(DbContext);

            var rows = await approvalRepository.GetRowsByKindAndEntityIdsAsync(Kind, [id], token)
                .ConfigureAwait(false);

            return rows.Where(r => r.EntityId == id).ToList();
        }

        private static int VersionOf(TEntity entity)
        {
            var value = typeof(TEntity).GetProperty(VersionPropertyName)?.GetValue(entity);
            return value == null ? 1 : Convert.ToInt32(value);
        }

        private static Guid GuidOf(TEntity entity)
        {
            var value = typeof(TEntity).GetProperty(GuidPropertyName)?.GetValue(entity);
            return value == null ? Guid.Empty : (Guid)value;
        }

        private static void SetId(TEntity entity, int id)
        {
            SetValue(typeof(TEntity).GetProperty(IdPropertyName), entity, id);
        }

        private static void SetVersion(TEntity entity, int version)
        {
            SetValue(typeof(TEntity).GetProperty(VersionPropertyName), entity, version);
        }

        private static void SetGuid(TEntity entity, Guid guid)
        {
            SetValue(typeof(TEntity).GetProperty(GuidPropertyName), entity, guid);
        }

        private static void SetCreatedUser(TEntity entity, string userName)
        {
            SetValue(typeof(TEntity).GetProperty(CreatedUserPropertyName), entity, userName);
        }

        private static void SetCreatedDate(TEntity entity, DateTime createdDate)
        {
            SetValue(typeof(TEntity).GetProperty(CreatedDatePropertyName), entity, createdDate);
        }

        private static int IdOf(TEntity entity)
        {
            var value = typeof(TEntity).GetProperty(IdPropertyName)?.GetValue(entity);
            return value == null ? 0 : Convert.ToInt32(value);
        }

        private static void SetValue(PropertyInfo property, object target, object value)
        {
            property?.SetValue(target, value);
        }
    }
}