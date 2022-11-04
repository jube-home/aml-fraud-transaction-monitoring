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

using System.Collections.Generic;
using System.Linq;
using Jube.Data.Context;

namespace Jube.Data.Query
{
    public class GetCaseWorkflowXPathByCaseWorkflowIdQuery
    {
        private readonly DbContext _dbContext;
        private readonly int _tenantRegistryId;

        public GetCaseWorkflowXPathByCaseWorkflowIdQuery(DbContext dbContext, string userName)
        {
            _dbContext = dbContext;
            _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<Dto> Execute(int caseWorkflowId)
        {
            return _dbContext.CaseWorkflowXPath.Where(w
                    => w.CaseWorkflowId == caseWorkflowId &&
                       w.Active == 1 && (w.Deleted == 0 || w.Deleted == null) &&
                       w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                )
                .Select(s => new Dto
                {
                    Name = s.Name,
                    XPath = FormatXPath(s.XPath),
                    ConditionalRegularExpressionFormatting = s.ConditionalRegularExpressionFormatting == 1,
                    ConditionalFormatForeColor = s.ConditionalFormatForeColor,
                    ConditionalFormatBackColor = s.ConditionalFormatBackColor,
                    RegularExpression = s.RegularExpression,
                    ForeRowColorScope = s.ForeRowColorScope == 1,
                    BackRowColorScope = s.BackRowColorScope == 1,
                    BoldLineFormatForeColor = s.BoldLineFormatForeColor,
                    BoldLineFormatBackColor = s.BoldLineFormatBackColor,
                    BoldLineMatched = s.BoldLineMatched == 1
                });
        }

        private string FormatXPath(string xPath)
        {
            var splits = xPath.Split(".");
            return splits[0].ToLower() switch
            {
                "data" => splits[1],
                _ => xPath
            };
        }

        public class Dto
        {
            public string Name { get; set; }
            public string XPath { get; set; }
            public bool ConditionalRegularExpressionFormatting { get; set; }
            public string ConditionalFormatForeColor { get; set; }
            public string ConditionalFormatBackColor { get; set; }
            public string RegularExpression { get; set; }
            public bool ForeRowColorScope { get; set; }
            public bool BackRowColorScope { get; set; }
            public string BoldLineFormatForeColor { get; set; }
            public string BoldLineFormatBackColor { get; set; }
            public bool BoldLineMatched { get; set; }
        }
    }
}