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

namespace Jube.Data.Query
{
    public class GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery
    {
        private readonly DbContext _dbContext;
        private readonly int? _tenantRegistryId;

        public GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(DbContext dbContext, string userName)
        {
            _dbContext = dbContext;
            _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private IEnumerable<Dto> Execute
            (int promotedExhaustiveSearchInstanceTrialInstanceId)
        {
            var query = from v in
                    _dbContext.ExhaustiveSearchInstanceVariable
                join t in _dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
                    on v.Id equals t.ExhaustiveSearchInstanceVariableId
                from p in _dbContext.ExhaustiveSearchInstanceTrialInstanceVariablePrescription
                    .Where(w1 => w1.ExhaustiveSearchInstanceTrialInstanceVariableId == t.Id).DefaultIfEmpty()
                from s in _dbContext.ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
                    .Where(w2 => w2.ExhaustiveSearchInstanceTrialInstanceVariableId == t.Id).DefaultIfEmpty()
                where (t.Removed == 0 || t.Removed == null)
                      && t.ExhaustiveSearchInstanceTrialInstanceId == promotedExhaustiveSearchInstanceTrialInstanceId
                orderby v.VariableSequence
                select new Dto
                {
                    Id = v.Id,
                    Name = v.Name,
                    Mean = Math.Round(p.Mean ?? v.Mean.Value, 2),
                    Maximum = Math.Round(v.Maximum ?? v.Maximum.Value, 2),
                    Minimum = Math.Round(v.Minimum ?? v.Minimum.Value, 2),
                    StandardDeviation = Math.Round(v.StandardDeviation ?? v.StandardDeviation.Value, 2),
                    NormalisationTypeId = v.NormalisationTypeId.GetValueOrDefault(),
                    EmptyRange = v.Maximum + v.Minimum == 0,
                    VariableSequence = v.VariableSequence.GetValueOrDefault(),
                    ProcessingTypeId = v.ProcessingTypeId.GetValueOrDefault()
                };
            return query;
        }

        public IEnumerable<Dto> ExecuteByExhaustiveSearchInstanceTrialInstanceId(
            int exhaustiveSearchInstanceTrialInstanceId)
        {
            return Execute(exhaustiveSearchInstanceTrialInstanceId);
        }

        public IEnumerable<Dto> ExecuteByExhaustiveSearchInstanceId(
            int exhaustiveSearchInstanceId)
        {
            var promotedExhaustiveSearchInstanceTrialInstanceId = _dbContext
                .ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Id == exhaustiveSearchInstanceId
                    && w.Active == 1
                    && (w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue))
                .OrderByDescending(o => o.Id)
                .Select(s => s.ExhaustiveSearchInstanceTrialInstanceId.GetValueOrDefault())
                .FirstOrDefault();

            return Execute(promotedExhaustiveSearchInstanceTrialInstanceId);
        }

        public class Dto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double Mean { get; set; }
            public double Maximum { get; set; }
            public double Minimum { get; set; }
            public double StandardDeviation { get; set; }
            public byte NormalisationTypeId { get; set; }
            public bool EmptyRange { get; set; }
            public int VariableSequence { get; set; }
            public int ProcessingTypeId { get; set; }
        }
    }
}