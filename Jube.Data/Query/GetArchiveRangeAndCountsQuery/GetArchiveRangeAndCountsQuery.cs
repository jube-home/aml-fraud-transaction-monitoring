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

namespace Jube.Data.Query.GetArchiveRangeAndCountsQuery
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;

    public class GetArchiveRangeAndCountsQuery(DbContext dbContext)
    {
        public Task<GetArchiveRangeAndCountsQueryDto> ExecuteAsync(Guid entityAnalysisModelGuid,
            CancellationToken token = default)
        {
            return (from archive in dbContext.Archive
                join model in dbContext.EntityAnalysisModel on archive.EntityAnalysisModelId equals model.Id
                where model.Guid == entityAnalysisModelGuid
                group archive by new
                {
                    archive.EntityAnalysisModelId
                }
                into g
                select new GetArchiveRangeAndCountsQueryDto
                {
                    Count = g.Count(),
                    Min = g.Min(q => q.ReferenceDate),
                    Max = g.Max(q => q.ReferenceDate)
                }).FirstOrDefaultAsync(token);
        }
    }
}