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
using AutoMapper;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class EntityAnalysisModelRepository
    {
        private readonly DbContext _dbContext;
        private readonly int? _tenantRegistryId;
        private readonly string _userName;

        public EntityAnalysisModelRepository(DbContext dbContext, string userName)
        {
            _dbContext = dbContext;
            _userName = userName;
            _tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == _userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelRepository(DbContext dbContext, int tenantRegistryId)
        {
            _dbContext = dbContext;
            _tenantRegistryId = tenantRegistryId;
        }

        public EntityAnalysisModelRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IEnumerable<EntityAnalysisModel> Get()
        {
            return _dbContext.EntityAnalysisModel.Where(w =>
                (w.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                && (w.Deleted == null || w.Deleted == 0));
        }

        public EntityAnalysisModel GetById(int id)
        {
            return _dbContext.EntityAnalysisModel.FirstOrDefault(w
                => (w.TenantRegistryId == _tenantRegistryId || !w.TenantRegistryId.HasValue)
                   && w.Id == id && (w.Deleted == null || w.Deleted == 0));
        }

        public EntityAnalysisModel Insert(EntityAnalysisModel model)
        {
            model.CreatedUser = _userName;
            model.TenantRegistryId = _tenantRegistryId;
            model.Version = 1;
            model.CreatedDate = DateTime.Now;
            model.Guid = Guid.NewGuid();
            model.Id = _dbContext.InsertWithInt32Identity(model);
            return model;
        }

        public EntityAnalysisModel Update(EntityAnalysisModel model)
        {
            var existing = _dbContext.EntityAnalysisModel
                .FirstOrDefault(w => w.Id
                                     == model.Id
                                     && (w.TenantRegistryId == _tenantRegistryId || !w.TenantRegistryId.HasValue)
                                     && (w.Deleted == 0 || w.Deleted == null)
                                     && (w.Locked == 0 || w.Locked == null));

            if (existing == null) throw new KeyNotFoundException();

            model.TenantRegistryId = _tenantRegistryId;
            model.CreatedUser = _userName;
            model.CreatedDate = DateTime.Now;
            model.Version = existing.Version + 1;
            model.CreatedUser = _userName;
            model.Guid = existing.Guid;

            _dbContext.Update(model);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EntityAnalysisModel, EntityAnalysisModelVersion>();
            });
            var mapper = new Mapper(config);
            
            var audit = mapper.Map<EntityAnalysisModelVersion>(existing);
            audit.EntityAnalysisModelId = existing.Id;
            
            _dbContext.Insert(audit);

            return model;
        }

        public void Delete(int id)
        {
            var records = _dbContext.EntityAnalysisModel
                .Where(d => (d.TenantRegistryId == _tenantRegistryId || !d.TenantRegistryId.HasValue)
                            && d.Id == id
                            && (d.Deleted == 0 || d.Deleted == null)
                            && (d.Locked == 0 || d.Locked == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Set(s => s.DeletedUser, _userName)
                .Update();

            if (records == 0) throw new KeyNotFoundException();
        }
    }
}