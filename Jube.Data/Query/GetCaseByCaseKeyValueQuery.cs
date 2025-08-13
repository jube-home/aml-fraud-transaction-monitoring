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

public class GetCaseByCaseKeyValueQuery
{
    private readonly DbContext _dbContext;
    private readonly string _userName;

    public GetCaseByCaseKeyValueQuery(DbContext dbContext, string user)
    {
        _dbContext = dbContext;
        _userName = user;
    }

    public IEnumerable<Dto> Execute(string key, string value)
    {
        var query = from c in _dbContext.Case
            from i in _dbContext.CaseWorkflow.InnerJoin(w => w.Guid == c.CaseWorkflowGuid)
            from m in _dbContext.EntityAnalysisModel.InnerJoin(w =>
                w.Id == i.EntityAnalysisModelId && (w.Deleted == 0 || w.Deleted == null))
            from t in _dbContext.TenantRegistry.InnerJoin(w => w.Id == m.TenantRegistryId)
            from u in _dbContext.UserInTenant.InnerJoin(w => w.TenantRegistryId == t.Id)
            from s in _dbContext.CaseWorkflowStatus.LeftJoin(w =>
                w.Guid == c.CaseWorkflowStatusGuid && w.CaseWorkflowId == i.Id &&
                (w.Deleted == 0 || w.Deleted == null))
            orderby c.Id descending
            where c.CaseKey == key && c.CaseKeyValue == value && u.User == _userName
            select new Dto
            {
                Id = c.Id,
                EntityAnalysisModelInstanceEntryGuid = c.EntityAnalysisModelInstanceEntryGuid,
                DiaryDate = c.DiaryDate.GetValueOrDefault(),
                CaseWorkflowGuid = c.CaseWorkflowGuid,
                CaseWorkflowStatusName = s.Name,
                CreatedDate = c.CreatedDate.GetValueOrDefault(),
                Locked = c.Locked.GetValueOrDefault() == 1,
                LockedUser = c.LockedUser ?? "",
                LockedDate = c.LockedDate.GetValueOrDefault(),
                ClosedStatusId = c.ClosedStatusId.GetValueOrDefault(),
                ClosedDate = c.ClosedDate.GetValueOrDefault(),
                ClosedUser = c.ClosedUser ?? "",
                CaseKey = c.CaseKey,
                Diary = c.Diary.GetValueOrDefault() == 1,
                DiaryUser = c.DiaryUser ?? "",
                Rating = c.Rating.GetValueOrDefault(),
                CaseKeyValue = c.CaseKeyValue,
                LastClosedStatus = c.LastClosedStatus.GetValueOrDefault(),
                ClosedStatusMigrationDate = c.ClosedStatusMigrationDate.GetValueOrDefault(),
                ForeColor = s.ForeColor,
                BackColor = s.BackColor
            };

        return query;
    }

    public class Dto
    {
        public int Id { get; set; }
        public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        public DateTime DiaryDate { get; set; }
        public Guid CaseWorkflowGuid { get; set; }
        public string CaseWorkflowStatusName { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool Locked { get; set; }
        public string LockedUser { get; set; }
        public DateTime LockedDate { get; set; }
        public byte ClosedStatusId { get; set; }
        public DateTime ClosedDate { get; set; }
        public string ClosedUser { get; set; }
        public string CaseKey { get; set; }
        public bool Diary { get; set; }
        public string DiaryUser { get; set; }
        public byte Rating { get; set; }
        public string CaseKeyValue { get; set; }
        public byte LastClosedStatus { get; set; }
        public DateTime ClosedStatusMigrationDate { get; set; }
        public string ForeColor { get; set; }
        public string BackColor { get; set; }
    }
}