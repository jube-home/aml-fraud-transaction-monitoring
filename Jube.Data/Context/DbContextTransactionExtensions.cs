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
using System.Threading;
using System.Threading.Tasks;

namespace Jube.Data.Context
{
    public static class DbContextTransactionExtensions
    {
        public static Task InTransactionAsync(this DbContext dbContext, Func<Task> work,
            CancellationToken token = default)
        {
            return dbContext.InTransactionAsync(async () =>
            {
                await work().ConfigureAwait(false);
                return true;
            }, token);
        }

        public static async Task<T> InTransactionAsync<T>(this DbContext dbContext, Func<Task<T>> work,
            CancellationToken token = default)
        {
            if (dbContext.Transaction is not null)
            {
                return await work().ConfigureAwait(false);
            }

            await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);
            try
            {
                var result = await work().ConfigureAwait(false);
                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
                return result;
            }
            catch
            {
                await dbContext.RollbackTransactionAsync(token).ConfigureAwait(false);
                throw;
            }
        }
    }
}