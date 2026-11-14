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
using LinqToDB;

namespace Jube.Test.Service.Query.CaseChecklist
{
    public sealed class CaseChecklistOwnItems(DbContext dbContext) : IAsyncDisposable
    {
        private readonly List<Guid> checklistGuids = [];
        private readonly List<Guid> itemGuids = [];

        public async Task<Guid> AddAsync(Guid statusGuid, string name, bool enableShow, string? showSql,
            bool enableValidation, string? validateSql)
        {
            var checklistGuid = Guid.NewGuid();
            var itemGuid = Guid.NewGuid();

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowChecklist
            {
                Guid = checklistGuid, CaseWorkflowStatusGuid = statusGuid, Name = $"zzFlag{name}", Priority = 0,
                Active = 1, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = "zzFlag"
            });
            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowChecklistItem
            {
                Guid = itemGuid, CaseWorkflowChecklistGuid = checklistGuid, Name = name, Priority = 0,
                EnableShow = (byte)(enableShow ? 1 : 0), ShowSql = showSql,
                EnableValidation = (byte)(enableValidation ? 1 : 0), ValidateSql = validateSql,
                ValidateMessage = "Flag test message", Active = 1, Deleted = 0, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = "zzFlag"
            });

            checklistGuids.Add(checklistGuid);
            itemGuids.Add(itemGuid);

            return itemGuid;
        }

        public Task SetEnableShowAsync(Guid itemGuid, bool enabled)
        {
            return dbContext.GetTable<Data.Poco.CaseWorkflowChecklistItem>().Where(w => w.Guid == itemGuid)
                .Set(s => s.EnableShow, (byte)(enabled ? 1 : 0)).UpdateAsync();
        }

        public Task SetEnableValidationAsync(Guid itemGuid, bool enabled)
        {
            return dbContext.GetTable<Data.Poco.CaseWorkflowChecklistItem>().Where(w => w.Guid == itemGuid)
                .Set(s => s.EnableValidation, (byte)(enabled ? 1 : 0)).UpdateAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await dbContext.GetTable<Data.Poco.CaseWorkflowChecklistItem>()
                .Where(w => itemGuids.Contains(w.Guid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowChecklist>()
                .Where(w => checklistGuids.Contains(w.Guid)).DeleteAsync();
        }
    }
}
