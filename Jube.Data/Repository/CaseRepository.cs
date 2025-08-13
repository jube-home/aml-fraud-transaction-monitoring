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
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository;

public class CaseRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;
    private readonly string _userName;

    public CaseRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public CaseRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }

    public CaseRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void UpdateExpiredCaseDiary(int id, byte closedStatus, byte lastClosedStatus)
    {
        _dbContext.Case
            .Where(d => d.Id == id)
            .Set(s => s.ClosedStatusId, closedStatus)
            .Set(s => s.LastClosedStatus, lastClosedStatus)
            .Update();
    }

    public void LockToUser(int id)
    {
        _dbContext.Case
            .Where(d => d.Id == id)
            .Set(s => s.Locked, (byte)1)
            .Set(s => s.LockedUser, _userName)
            .Set(s => s.LockedDate, DateTime.Now)
            .Update();
    }

    public IEnumerable<Case> Get()
    {
        return _dbContext.Case.Where(w =>
            w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
            !_tenantRegistryId.HasValue);
    }

    public Case GetById(int id)
    {
        return _dbContext.Case.FirstOrDefault(w
            => (w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                !_tenantRegistryId.HasValue)
               && w.Id == id);
    }

    public IEnumerable<Case> GetByExpired()
    {
        return _dbContext.Case.Where(w
            => (w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                !_tenantRegistryId.HasValue)
               && (w.ClosedStatusId == 0 ||
                   w.ClosedStatusId == 1 ||
                   w.ClosedStatusId == 2 ||
                   w.ClosedStatusId == 4)
               && DateTime.Now >= w.DiaryDate
               && w.Diary == 1);
    }

    public Case Insert(Case model)
    {
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public Case Update(Case model)
    {
        _dbContext.Update(model);
        return model;
    }
}