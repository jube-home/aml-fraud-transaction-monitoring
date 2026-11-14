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
    using Context;
    using LinqToDB;
    using LinqToDB.Async;
    using Poco;

    public class CaseTagRepository(DbContext dbContext, string userName)
    {
        public Task<List<CaseTag>> GetByCaseIdAsync(int caseId, CancellationToken token = default)
        {
            return dbContext.CaseTag.Where(t => t.CaseId == caseId && (t.Deleted == null || t.Deleted == 0))
                .OrderBy(t => t.Name).ToListAsync(token);
        }

        public async Task<List<CaseTag>> MergeTagsAsync(int caseId, string[] tags, CancellationToken token = default)
        {
            await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

            try
            {
                await LinqToDB.Data.DataContextExtensions.ExecuteAsync(dbContext,
                    "SELECT pg_advisory_xact_lock(hashtext(@key))", token,
                    new LinqToDB.Data.DataParameter("key", $"CaseTag:{caseId}"));
                var kept = new List<long>();

                foreach (var tag in tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.Ordinal))
                {
                    var existing = await dbContext.CaseTag.FirstOrDefaultAsync(t => t.CaseId == caseId && t.Name == tag, token)
                        .ConfigureAwait(false);

                    if (existing == null)
                    {
                        kept.Add(await dbContext.InsertWithInt64IdentityAsync(new CaseTag
                        {
                            CaseId = caseId, Name = tag, Version = 1, CreatedUser = userName, CreatedDate = DateTime.UtcNow,
                            Deleted = 0
                        }, token: token).ConfigureAwait(false));
                    }
                    else
                    {
                        await dbContext.CaseTag.Where(t => t.Id == existing.Id)
                            .Set(t => t.Deleted, (byte)0)
                            .Set(t => t.DeletedUser, (string)null)
                            .Set(t => t.DeletedDate, (DateTime?)null)
                            .Set(t => t.Version, (existing.Version ?? 1) + 1)
                            .UpdateAsync(token).ConfigureAwait(false);
                        kept.Add(existing.Id);
                    }
                }

                await dbContext.CaseTag.Where(t => t.CaseId == caseId && !kept.Contains(t.Id)
                                                   && (t.Deleted == null || t.Deleted == 0))
                    .Set(t => t.Deleted, (byte)1)
                    .Set(t => t.DeletedUser, userName)
                    .Set(t => t.DeletedDate, DateTime.UtcNow)
                    .UpdateAsync(token).ConfigureAwait(false);
                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
            }
            catch
            {
                await dbContext.RollbackTransactionAsync(token).ConfigureAwait(false);
                throw;
            }

            return await GetByCaseIdAsync(caseId, token).ConfigureAwait(false);
        }
    }
}
