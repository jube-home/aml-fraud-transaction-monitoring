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
using System.Collections.Generic;
using System.Linq;
using Jube.Data.Context;
using LinqToDB;

namespace Jube.Data.Query;

public class GetCaseWorkflowFormEntryByCaseKeyValueQuery
{
    private readonly DbContext _dbContext;
    private readonly string _userName;

    public GetCaseWorkflowFormEntryByCaseKeyValueQuery(DbContext dbContext, string user)
    {
        _dbContext = dbContext;
        _userName = user;
    }

    public IEnumerable<Dto> Execute(string key, string value)
    {
        var query = from c in _dbContext.Case
            from n in _dbContext.CaseWorkflowFormEntry.InnerJoin(w => w.CaseId == c.Id)
            from a in _dbContext.CaseWorkflowForm.InnerJoin(w => w.Id == n.CaseWorkflowFormId)
            from i in _dbContext.CaseWorkflow.InnerJoin(w => w.Guid == c.CaseWorkflowGuid)
            from m in _dbContext.EntityAnalysisModel.InnerJoin(w =>
                w.Id == i.EntityAnalysisModelId && (w.Deleted == 0 || w.Deleted == null))
            from t in _dbContext.TenantRegistry.InnerJoin(w => w.Id == m.TenantRegistryId)
            from u in _dbContext.UserInTenant.InnerJoin(w => w.TenantRegistryId == t.Id)
            orderby c.Id descending
            where c.CaseKey == key && c.CaseKeyValue == value && u.User == _userName
            select new Dto
            {
                Id = n.Id,
                CaseId = n.CaseId.GetValueOrDefault(),
                CreatedDate = n.CreatedDate.GetValueOrDefault(),
                CreatedUser = n.CreatedUser,
                Name = a.Name
            };

        return query;
    }

    public class Dto
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public byte ResponseStatusId { get; set; }
        public string Name { get; set; }
    }
}