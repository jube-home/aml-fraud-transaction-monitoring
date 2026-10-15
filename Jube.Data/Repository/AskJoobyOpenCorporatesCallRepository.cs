/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class AskJoobyOpenCorporatesCallRepository(DbContext dbContext)
    {
        public async Task<AskJoobyOpenCorporatesCall> InsertAsync(AskJoobyOpenCorporatesCall model,
            CancellationToken token = default)
        {
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task<List<AskJoobyOpenCorporatesCall>> ListByRunIdsAsync(IReadOnlyCollection<int> runIds,
            CancellationToken token = default)
        {
            if (runIds.Count == 0)
            {
                return [];
            }

            return await dbContext.AskJoobyOpenCorporatesCall
                .Where(w => w.AskJoobyRunId.HasValue && runIds.Contains(w.AskJoobyRunId.Value))
                .OrderBy(w => w.Id)
                .ToListAsync(token);
        }
    }
}