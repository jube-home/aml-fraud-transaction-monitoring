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

using System.Collections.Generic;
using System.Linq;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository;

public class MockArchiveRepository
{
    private readonly DbContext _dbContext;

    public MockArchiveRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Insert(MockArchive model)
    {
        _dbContext.Insert(model);
    }

    public IEnumerable<string> GetJsonByEntityAnalysisModelIdRandomLimit(int entityAnalysisModelId, int limit)
    {
        return _dbContext.MockArchive
            .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId)
            .OrderBy(o => o.EntityAnalysisModelInstanceEntryGuid).Select(s => s.Json).Take(limit);
    }

    public void Delete()
    {
        _dbContext.MockArchive.Delete();
    }
}