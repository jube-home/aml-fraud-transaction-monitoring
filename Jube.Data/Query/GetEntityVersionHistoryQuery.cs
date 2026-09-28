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

namespace Jube.Data.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;

    public class GetEntityVersionHistoryQuery<TVersion>(DbContext dbContext, string parentIdPropertyName)
        where TVersion : class
    {
        private const string IdPropertyName = "Id";
        private const string CreatedDatePropertyName = "CreatedDate";

        private IQueryable<TVersion> Table => dbContext.GetTable<TVersion>();

        public int? ParentIdOf(TVersion version)
        {
            var value = typeof(TVersion).GetProperty(parentIdPropertyName)?.GetValue(version);
            return value switch
            {
                null => null,
                int i => i,
                _ => Convert.ToInt32(value)
            };
        }

        public Task<TVersion> ByIdAsync(int id, CancellationToken token = default)
        {
            return Table.Where(EqualsPredicate<int>(IdPropertyName, id)).FirstOrDefaultAsync(token);
        }

        public Task<List<TVersion>> ByParentIdAsync(int parentId, CancellationToken token = default)
        {
            return OrderByRecency(Table.Where(EqualsPredicate<int?>(parentIdPropertyName, parentId)))
                .ToListAsync(token);
        }

        public Task<List<TVersion>> ByDateRangeAsync(int parentId, DateTime from, DateTime to,
            CancellationToken token = default)
        {
            return OrderByRecency(Table.Where(EqualsPredicate<int?>(parentIdPropertyName, parentId))
                    .Where(DateRangePredicate(from, to)))
                .ToListAsync(token);
        }

        public Task<TVersion> ByIdAndDateRangeAsync(int id, DateTime from, DateTime to,
            CancellationToken token = default)
        {
            return Table.Where(EqualsPredicate<int>(IdPropertyName, id))
                .Where(DateRangePredicate(from, to))
                .FirstOrDefaultAsync(token);
        }

        public Task<TVersion> LatestAsync(int parentId, CancellationToken token = default)
        {
            return OrderByRecency(Table.Where(EqualsPredicate<int?>(parentIdPropertyName, parentId)))
                .FirstOrDefaultAsync(token);
        }

        public async Task<IReadOnlyList<VersionFieldChange>> CompareAsync(int fromId, int toId,
            CancellationToken token = default)
        {
            var from = await ByIdAsync(fromId, token);
            var to = await ByIdAsync(toId, token);
            return VersionDiff.Compare(from, to);
        }

        private IOrderedQueryable<TVersion> OrderByRecency(IQueryable<TVersion> query)
        {
            return query.OrderByDescending(PropertySelector<int>(IdPropertyName));
        }

        private static Expression<Func<TVersion, bool>> EqualsPredicate<TValue>(string propertyName, TValue value)
        {
            var parameter = Expression.Parameter(typeof(TVersion), "x");
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value, property.Type);
            var body = Expression.Equal(property, constant);
            return Expression.Lambda<Func<TVersion, bool>>(body, parameter);
        }

        private static Expression<Func<TVersion, bool>> DateRangePredicate(DateTime from, DateTime to)
        {
            var parameter = Expression.Parameter(typeof(TVersion), "x");
            var property = Expression.Property(parameter, CreatedDatePropertyName);
            var fromConstant = Expression.Constant(from, property.Type);
            var toConstant = Expression.Constant(to, property.Type);
            var body = Expression.AndAlso(
                Expression.GreaterThanOrEqual(property, fromConstant),
                Expression.LessThanOrEqual(property, toConstant));
            return Expression.Lambda<Func<TVersion, bool>>(body, parameter);
        }

        private static Expression<Func<TVersion, TValue>> PropertySelector<TValue>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(TVersion), "x");
            var property = Expression.Property(parameter, propertyName);
            return Expression.Lambda<Func<TVersion, TValue>>(property, parameter);
        }
    }
}