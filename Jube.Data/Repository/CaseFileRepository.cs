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

public class CaseFileRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;
    private readonly string _userName;

    public CaseFileRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public CaseFileRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }

    public CaseFileRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IEnumerable<CaseEvent> Get()
    {
        return _dbContext.CaseEvent.Where(w =>
            w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
            !_tenantRegistryId.HasValue);
    }

    public IEnumerable<CaseFile> GetByCaseKeyValue(string key, string value)
    {
        return _dbContext.CaseFile.Where(w
                => (w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                    !_tenantRegistryId.HasValue)
                   && (w.Case.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                       w.Case.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                   && w.CaseKey == key && w.CaseKeyValue == value && (w.Deleted == 0 || w.Deleted == null))
            .OrderByDescending(o => o.Id);
    }

    public CaseFile GetById(int id)
    {
        return _dbContext.CaseFile.FirstOrDefault(w
            => (w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                !_tenantRegistryId.HasValue)
               && w.Id == id && (w.Deleted == 0 || w.Deleted == null));
    }

    public void Delete(int id)
    {
        var records = _dbContext.CaseFile
            .Where(d => d.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                        && d.Id == id
                        && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Set(s => s.DeletedUser, _userName)
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }

    public CaseFile Insert(CaseFile model)
    {
        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }
}