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

public class EntityAnalysisModelReprocessingRuleInstanceRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;
    private readonly string _userName;

    public EntityAnalysisModelReprocessingRuleInstanceRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public EntityAnalysisModelReprocessingRuleInstanceRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IEnumerable<EntityAnalysisModelReprocessingRuleInstance> Get()
    {
        return _dbContext.EntityAnalysisModelReprocessingRuleInstance
            .Where(w =>
                w.EntityAnalysisModelReprocessingRule.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                || !_tenantRegistryId.HasValue);
    }

    public IEnumerable<EntityAnalysisModelReprocessingRuleInstance> GetByEntityAnalysisModelsReprocessingRuleId(
        int entityAnalysisModelsReprocessingRuleId)
    {
        return _dbContext.EntityAnalysisModelReprocessingRuleInstance
            .Where(w =>
                (w.EntityAnalysisModelReprocessingRule.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                 || !_tenantRegistryId.HasValue)
                && w.EntityAnalysisModelReprocessingRuleId == entityAnalysisModelsReprocessingRuleId &&
                (w.Deleted == 0 || w.Deleted == null))
            .OrderByDescending(o => o.Id);
    }

    public EntityAnalysisModelReprocessingRuleInstance GetById(int id)
    {
        return _dbContext.EntityAnalysisModelReprocessingRuleInstance.FirstOrDefault(w =>
            (w.EntityAnalysisModelReprocessingRule.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
             || !_tenantRegistryId.HasValue)
            && w.Id == id && (w.Deleted == 0 || w.Deleted == null));
    }

    public EntityAnalysisModelReprocessingRuleInstance Insert(EntityAnalysisModelReprocessingRuleInstance model)
    {
        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;
        model.Version = 1;
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public EntityAnalysisModelReprocessingRuleInstance InsertByExistingUpdateUncompleted(
        EntityAnalysisModelReprocessingRuleInstance model)
    {
        var existing = _dbContext.EntityAnalysisModelReprocessingRuleInstance
            .FirstOrDefault(w =>
                (w.EntityAnalysisModelReprocessingRule.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                 || !_tenantRegistryId.HasValue)
                && w.EntityAnalysisModelReprocessingRuleId
                == model.EntityAnalysisModelReprocessingRuleId
                && (w.Deleted == 0 || w.Deleted == null)
                && w.StatusId != 4
            );

        if (existing != null) throw new KeyNotFoundException();

        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;
        model.Version = 1;
        model.StatusId = 0;
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public EntityAnalysisModelReprocessingRuleInstance UpdateCounts
        (int id, int sampledCount, int matchedCount, int processedCount, int errorCount, DateTime referenceDate)
    {
        var existing = _dbContext.EntityAnalysisModelReprocessingRuleInstance
            .FirstOrDefault(w => w.Id
                == id && (w.Deleted == 0 || w.Deleted == null));

        if (existing == null) throw new KeyNotFoundException();

        existing.SampledCount = sampledCount;
        existing.MatchedCount = matchedCount;
        existing.ProcessedCount = processedCount;
        existing.ErrorCount = errorCount;
        existing.ReferenceDate = referenceDate;
        existing.UpdatedDate = DateTime.Now;
        existing.StatusId = 3;

        _dbContext.Update(existing);

        return existing;
    }

    public EntityAnalysisModelReprocessingRuleInstance Update(EntityAnalysisModelReprocessingRuleInstance model)
    {
        var existing = _dbContext.EntityAnalysisModelReprocessingRuleInstance
            .FirstOrDefault(w => w.Id
                                 == model.Id
                                 && (w.EntityAnalysisModelReprocessingRule.EntityAnalysisModel.TenantRegistryId ==
                                     _tenantRegistryId
                                     || !_tenantRegistryId.HasValue)
                                 && (w.Deleted == 0 || w.Deleted == null));

        if (existing == null) throw new KeyNotFoundException();

        model.Version = existing.Version + 1;
        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;
        model.Id = existing.Id;

        var id = _dbContext
            .InsertWithInt32Identity(model);

        Delete(existing.Id);

        model.Id = id;

        return model;
    }

    public void UpdateReferenceDateCount(int id, long availableCount, DateTime referenceDate)
    {
        var records = _dbContext.EntityAnalysisModelReprocessingRuleInstance
            .Where(d =>
                d.Id == id)
            .Set(s => s.AvailableCount, availableCount)
            .Set(s => s.StatusId, (byte)2)
            .Set(s => s.ReferenceDate, referenceDate)
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }

    public void UpdateCompleted(int id)
    {
        var records = _dbContext.EntityAnalysisModelReprocessingRuleInstance
            .Where(d =>
                d.Id == id)
            .Set(s => s.CompletedDate, DateTime.Now)
            .Set(s => s.StatusId, (byte)4)
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }

    public void Delete(int id)
    {
        var records = _dbContext.EntityAnalysisModelReprocessingRuleInstance
            .Where(d =>
                (d.EntityAnalysisModelReprocessingRule.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                 || !_tenantRegistryId.HasValue)
                && d.Id == id
                && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Set(s => s.DeletedUser, _userName)
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }
}