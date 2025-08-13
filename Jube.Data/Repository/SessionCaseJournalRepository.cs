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
using System.Linq;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository;

public class SessionCaseJournalRepository
{
    private readonly DbContext _dbContext;
    private readonly int _tenantRegistryId;
    private readonly string _userName;

    public SessionCaseJournalRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public SessionCaseJournal GetByCaseWorkflowId(int id)
    {
        return _dbContext.SessionCaseJournal.FirstOrDefault(w
            => w.CreatedUser == _userName &&
               w.CaseWorkflowId == id);
    }

    public SessionCaseJournal GetByCaseWorkflowGuid(Guid guid)
    {
        return _dbContext.SessionCaseJournal.FirstOrDefault(w
            => w.CreatedUser == _userName &&
               w.CaseWorkflow.Guid == guid);
    }

    public SessionCaseJournal Upsert(SessionCaseJournal model)
    {
        var existing = _dbContext.SessionCaseJournal
            .FirstOrDefault(w => w.CaseWorkflowId == model.CaseWorkflowId
                                 && w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                                 && w.CreatedUser == _userName);


        if (existing == null)
        {
            model.CreatedDate = DateTime.Now;
            model.CreatedUser = _userName;
            model.Id = _dbContext.InsertWithInt32Identity(model);
            return model;
        }

        existing.CreatedDate = DateTime.Now;
        existing.Json = model.Json;
        _dbContext.Update(existing);
        return model;
    }
}