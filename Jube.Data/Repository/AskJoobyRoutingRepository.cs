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
    public class AskJoobyRoutingRepository(DbContext dbContext)
    {
        public Task<List<AskJoobyRouting>> ListByConversationAsync(int askJoobyConversationId,
            CancellationToken token = default)
        {
            return dbContext.AskJoobyRouting
                .Where(w => w.AskJoobyConversationId == askJoobyConversationId)
                .OrderBy(o => o.Id)
                .ToListAsync(token);
        }

        public Task<AskJoobyRouting> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.AskJoobyRouting.Where(w => w.Id == id).FirstOrDefaultAsync(token);
        }

        public async Task<AskJoobyRouting> InsertAsync(AskJoobyRouting model,
            CancellationToken token = default)
        {
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }
    }
}