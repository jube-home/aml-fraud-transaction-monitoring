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

namespace Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Dto.Options;
    using GetArchiveKeyFieldsByEntityAnalysisModelIdQuery;
    using LinqToDB;
    using LinqToDB.Async;

    public class GetCaseWorkflowXPathByCaseWorkflowIdQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public GetCaseWorkflowXPathByCaseWorkflowIdQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task<IEnumerable<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto>> ExecuteAsync(Guid caseWorkflowGuid,
            CancellationToken token = default)
        {
            var rows = await dbContext.CaseWorkflowXPath
                .Where(w =>
                    w.CaseWorkflow.Guid == caseWorkflowGuid
                    && (w.CaseWorkflow.Deleted == 0 || w.CaseWorkflow.Deleted == null)
                    && (w.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                        w.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                    && w.Active == 1
                    && (w.Deleted == 0 || w.Deleted == null)
                    && w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                    && dbContext.CaseWorkflowXPathRole
                        .Where(r => r.CaseWorkflowXPathGuid == w.Guid
                                    && (r.Deleted == 0 || r.Deleted == null))
                        .Any(r => dbContext.RoleRegistry
                            .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                            .Any(rr => dbContext.UserRegistry
                                .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                    && dbContext.CaseWorkflowRole
                        .Where(r => r.CaseWorkflowGuid == w.CaseWorkflow.Guid
                                    && (r.Deleted == 0 || r.Deleted == null))
                        .Any(r => dbContext.RoleRegistry
                            .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                            .Any(rr => dbContext.UserRegistry
                                .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                )
                .Select(s => new
                {
                    s.Name,
                    s.SourceKindId,
                    s.SourceGuid,
                    s.CaseWorkflow.EntityAnalysisModelId,
                    ConditionalRegularExpressionFormatting = s.ConditionalRegularExpressionFormatting == 1,
                    s.ConditionalFormatForeColor,
                    s.ConditionalFormatBackColor,
                    s.RegularExpression,
                    ForeRowColorScope = s.ForeRowColorScope == 1,
                    BackRowColorScope = s.BackRowColorScope == 1,
                    s.BoldLineFormatForeColor,
                    s.BoldLineFormatBackColor,
                    BoldLineMatched = s.BoldLineMatched == 1,
                    s.RoleId,
                    Segment = s.Segment == 1
                })
                .ToListAsync(token);

            var modelId = rows.Select(r => r.EntityAnalysisModelId).FirstOrDefault(id => id.HasValue);

            if (modelId is null)
            {
                return [];
            }

            var fields = (await new GetArchiveKeyFieldsByEntityAnalysisModelIdQuery(dbContext)
                    .ExecuteAsync(modelId.Value, token).ConfigureAwait(false))
                .GroupBy(f => (f.Kind, f.Guid)).ToDictionary(g => g.Key, g => g.First());

            return rows.Select(s => s.SourceGuid.HasValue && s.SourceKindId.HasValue
                              && fields.TryGetValue(((ArchiveKeySourceKind)s.SourceKindId.Value, s.SourceGuid.Value),
                                  out var field)
                    ? new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto
                    {
                        Name = s.Name,
                        XPath = field.SelectTokenPath,
                        Key = field.Name,
                        DataTypeId = field.DataTypeId,
                        ProcessingTypeId = field.ProcessingTypeId,
                        SourceKindId = (int)field.Kind,
                        SourceGuid = field.Guid,
                        ConditionalRegularExpressionFormatting = s.ConditionalRegularExpressionFormatting,
                        ConditionalFormatForeColor = s.ConditionalFormatForeColor,
                        ConditionalFormatBackColor = s.ConditionalFormatBackColor,
                        RegularExpression = s.RegularExpression,
                        ForeRowColorScope = s.ForeRowColorScope,
                        BackRowColorScope = s.BackRowColorScope,
                        BoldLineFormatForeColor = s.BoldLineFormatForeColor,
                        BoldLineFormatBackColor = s.BoldLineFormatBackColor,
                        BoldLineMatched = s.BoldLineMatched,
                        Drill = field.IsArchiveKey,
                        RoleId = s.RoleId,
                        Segment = s.Segment
                    }
                    : null)
                .Where(dto => dto is not null)
                .ToList();
        }
    }
}