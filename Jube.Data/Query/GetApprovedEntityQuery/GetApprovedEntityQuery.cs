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

namespace Jube.Data.Query.GetApprovedEntityQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using Context;
    using LinqToDB;
    using Microsoft.Extensions.Logging.Abstractions;
    using Repository;

    public class GetApprovedEntityQuery<TEntity, TVersion>(
        DbContext dbContext,
        EntityApprovalKind kind,
        string versionParentIdPropertyName,
        int? tenantRegistryId)
        where TEntity : class
        where TVersion : class
    {
        private const string IdPropertyName = "Id";
        private const string VersionPropertyName = "Version";
        private const string DeletedPropertyName = "Deleted";

        private static readonly IMapper mapper = new Mapper(new MapperConfiguration(
            cfg => { cfg.CreateMap<TVersion, TEntity>().ForMember(IdPropertyName, o => o.Ignore()); },
            NullLoggerFactory.Instance));

        private readonly EntityApprovalRepository repository = tenantRegistryId.HasValue
            ? new EntityApprovalRepository(dbContext, tenantRegistryId.Value)
            : new EntityApprovalRepository(dbContext);

        private readonly PropertyInfo versionParentIdProperty =
            typeof(TVersion).GetProperty(versionParentIdPropertyName)
            ?? throw new ArgumentException(
                string.Concat(typeof(TVersion).Name, " has no property ",
                    versionParentIdPropertyName), nameof(versionParentIdPropertyName));

        public async Task<List<TEntity>> ExecuteAsync(IReadOnlyList<TEntity> current, int approvalsRequired = 1,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(current);

            if (current.Count == 0)
            {
                return [];
            }

            var currentVersionByEntityId = new Dictionary<int, int>();

            foreach (var entity in current)
            {
                currentVersionByEntityId[IdOf(entity)] = VersionOf(entity);
            }

            var approvals = await repository
                .GetRowsByKindAndEntityIdsAsync(kind, currentVersionByEntityId.Keys.ToList(), token)
                .ConfigureAwait(false);

            var effective = EntityApprovalResolver.EffectiveVersionsByEntity(approvals, currentVersionByEntityId,
                approvalsRequired);

            var substitutions = effective
                .Where(w => w.Value != currentVersionByEntityId[w.Key])
                .ToDictionary(k => k.Key, v => v.Value);

            var substituted = substitutions.Count == 0
                ? []
                : await SubstitutionsAsync(substitutions, token).ConfigureAwait(false);

            var resolved = new List<TEntity>(effective.Count);

            foreach (var entity in current)
            {
                var id = IdOf(entity);

                if (!effective.TryGetValue(id, out var version))
                {
                    continue;
                }

                if (version == currentVersionByEntityId[id])
                {
                    if (!IsDeleted(entity))
                    {
                        resolved.Add(entity);
                    }

                    continue;
                }

                if (substituted.TryGetValue(id, out var approvedVersion) && !IsDeleted(approvedVersion))
                {
                    resolved.Add(approvedVersion);
                }
            }

            return resolved;
        }

        public static TEntity MapVersionToEntity(TVersion version, int parentId)
        {
            var entity = mapper.Map<TEntity>(version);
            typeof(TEntity).GetProperty(IdPropertyName)?.SetValue(entity, parentId);
            return entity;
        }

        public static int VersionNumberOf(TVersion version)
        {
            return VersionOfVersionRow(version);
        }

        private async Task<Dictionary<int, TEntity>> SubstitutionsAsync(Dictionary<int, int> substitutions,
            CancellationToken token)
        {
            var parentIds = substitutions.Keys.ToList();

            var candidates = await dbContext.GetTable<TVersion>()
                .Where(ParentIdInPredicate(parentIds))
                .ToListAsync(token).ConfigureAwait(false);

            var substituted = new Dictionary<int, TEntity>();

            foreach (var candidate in candidates)
            {
                var parentId = ParentIdOf(candidate);

                if (parentId == null || !substitutions.TryGetValue(parentId.Value, out var wanted))
                {
                    continue;
                }

                if (VersionOfVersionRow(candidate) != wanted)
                {
                    continue;
                }

                substituted[parentId.Value] = MapVersionToEntity(candidate, parentId.Value);
            }

            return substituted;
        }

        public Expression<Func<TVersion, bool>> ParentIdInPredicate(ICollection<int> parentIds)
        {
            var parameter = Expression.Parameter(typeof(TVersion), "x");
            var property = Expression.Property(parameter, versionParentIdProperty);
            var value = property.Type == typeof(int)
                ? property
                : Expression.Property(property, "Value");
            var hasValue = property.Type == typeof(int)
                ? (Expression)Expression.Constant(true)
                : Expression.Property(property, "HasValue");

            var contains = Expression.Call(
                typeof(Enumerable), nameof(Enumerable.Contains), [typeof(int)],
                Expression.Constant(parentIds), value);

            return Expression.Lambda<Func<TVersion, bool>>(Expression.AndAlso(hasValue, contains), parameter);
        }

        private static int IdOf(TEntity entity)
        {
            return Convert.ToInt32(typeof(TEntity).GetProperty(IdPropertyName)?.GetValue(entity) ?? 0);
        }

        public static bool IsDeleted(TEntity entity)
        {
            var value = typeof(TEntity).GetProperty(DeletedPropertyName)?.GetValue(entity);
            return value != null && Convert.ToInt32(value) != 0;
        }

        private static int VersionOf(TEntity entity)
        {
            var value = typeof(TEntity).GetProperty(VersionPropertyName)?.GetValue(entity);
            return value == null ? 1 : Convert.ToInt32(value);
        }

        private static int VersionOfVersionRow(TVersion version)
        {
            var value = typeof(TVersion).GetProperty(VersionPropertyName)?.GetValue(version);
            return value == null ? 0 : Convert.ToInt32(value);
        }

        private int? ParentIdOf(TVersion version)
        {
            var value = versionParentIdProperty.GetValue(version);
            return value == null ? null : Convert.ToInt32(value);
        }
    }
}