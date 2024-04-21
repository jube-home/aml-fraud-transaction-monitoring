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
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Validation;
using Jube.Data.Poco;
using Jube.Data.Reporting;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Repository
{
    public class VisualisationRegistryDatasourceRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public VisualisationRegistryDatasourceRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<VisualisationRegistryDatasource> Get()
        {
            return dbContext.VisualisationRegistryDatasource
                .Where(w => w.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null));
        }

        public IEnumerable<VisualisationRegistryDatasource> GetByVisualisationRegistryId(int visualisationRegistryId)
        {
            return dbContext.VisualisationRegistryDatasource
                .Where(w => w.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                            && w.VisualisationRegistryId == visualisationRegistryId &&
                            (w.Deleted == 0 || w.Deleted == null));
        }

        public IEnumerable<VisualisationRegistryDatasource> GetByVisualisationRegistryIdActiveOnly(int visualisationRegistryId)
        {
            return dbContext.VisualisationRegistryDatasource
                .Where(w => w.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                            && w.VisualisationRegistryId == visualisationRegistryId
                            && w.Active == 1 &&
                            (w.Deleted == 0 || w.Deleted == null));
        }
        
        public VisualisationRegistryDatasource GetById(int id)
        {
            return dbContext.VisualisationRegistryDatasource.FirstOrDefault(w
                => w.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                   && w.Id == id
                   && (w.Deleted == 0 || w.Deleted == null));
        }

        public async Task<VisualisationRegistryDatasource> InsertAsync(VisualisationRegistryDatasource model)
        {
            if (model.VisualisationRegistryId != null)
            {
                Dictionary<string, string> columns;
                try
                {
                    columns = await ValidateSeriesAsync(dbContext, model.VisualisationRegistryId.Value, model.Command);
                }
                catch (Exception e)
                {
                    var sqlValidationFailed = new SqlValidationFailed(e.Message);
                    throw sqlValidationFailed;
                }

                model.CreatedUser = userName;
                model.CreatedDate = DateTime.Now;
                model.Version = 1;
                model.Id = await dbContext.InsertWithInt32IdentityAsync(model);

                FillSeries(model.Id, columns);
            }

            return model;
        }

        public async Task<VisualisationRegistryDatasource> UpdateAsync(VisualisationRegistryDatasource model)
        {
            if (model.VisualisationRegistryId != null)
            {
                Dictionary<string, string> columns;
                try
                {
                    columns = await ValidateSeriesAsync(dbContext, model.VisualisationRegistryId.Value, model.Command);
                }
                catch (Exception e)
                {
                    var sqlValidationFailed = new SqlValidationFailed(e.Message);
                    throw sqlValidationFailed;
                }
                
                var existing = dbContext.VisualisationRegistryDatasource
                    .FirstOrDefault(w => w.Id
                                         == model.Id
                                         && (w.Deleted == 0 || w.Deleted == null)
                                         && (w.Locked == 0 || w.Locked == null));

                if (existing == null) throw new KeyNotFoundException();

                model.Version = existing.Version + 1;
                model.CreatedUser = userName;
                model.CreatedDate = DateTime.Now;
                model.InheritedId = existing.Id;

                Delete(existing.Id);

                var id = await dbContext.InsertWithInt32IdentityAsync(model);
                model.Id = id;
                
                FillSeries(model.Id, columns);
            }

            return model;
        }

        private void FillSeries(int id, Dictionary<string, string> columns)
        {
            foreach (var (key, value) in columns)
            {
                var visualisationRegistryDatasourceSeries = new VisualisationRegistryDatasourceSeries
                    {
                        VisualisationRegistryDatasourceId = id,
                        Name = key
                    };

                switch (value)
                {
                    case "integer":
                    case "bigint":
                        visualisationRegistryDatasourceSeries.DataTypeId = 2;
                        break;
                    case "double precision":
                        visualisationRegistryDatasourceSeries.DataTypeId = 3;
                        break;
                    default:
                    {
                        if (value.Contains("timestamp"))
                            visualisationRegistryDatasourceSeries.DataTypeId = 4;
                        else
                            visualisationRegistryDatasourceSeries.DataTypeId = value switch
                            {
                                "smallint" => 5,
                                "double precision[]" => 6,
                                _ => value.EndsWith("[]") ? 7 : 1
                            };

                        break;
                    }
                }

                dbContext.Insert(visualisationRegistryDatasourceSeries);
            }
        }

        private async Task<Dictionary<string, string>> ValidateSeriesAsync(DataConnection dataConnection, int visualisationRegistryId, string sql)
        {
            var visualisationRegistryParameterRepository = new VisualisationRegistryParameterRepository(this.dbContext);
            var parameters =
                visualisationRegistryParameterRepository.GetByVisualisationRegistryId(visualisationRegistryId);

            var parametersDefaultValues = new Dictionary<string, object>();
            foreach (var parameter in parameters)
            {
                object defaultValue = parameter.DataTypeId switch
                {
                    1 => "",
                    2 => 0,
                    3 => 0d,
                    4 => new DateTime(),
                    5 => true,
                    _ => ""
                };

                parametersDefaultValues.Add(parameter.Name.Replace(" ", "_"), defaultValue);
            }

            var postgres = new Postgres(dataConnection.ConnectionString);
            return await postgres.IntrospectAsync(sql, parametersDefaultValues);
        }
        
        public void Delete(int id)
        {
            var records = dbContext.VisualisationRegistryDatasource
                .Where(d => d.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                            && d.Id == id
                            && (d.Locked == 0 || d.Locked == null)
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Set(s => s.DeletedUser, userName)
                .Update();

            if (records == 0) throw new KeyNotFoundException();
        }
    }
}