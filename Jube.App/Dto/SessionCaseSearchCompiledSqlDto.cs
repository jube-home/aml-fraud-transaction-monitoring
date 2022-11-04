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

namespace Jube.App.Dto
{
    public class SessionCaseSearchCompiledSqlDto
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public string FilterJson { get; set; }
        public string FilterTokens { get; set; }
        public string SelectJson { get; set; }
        public string FilterSql { get; set; }
        public string SelectSqlSearch { get; set; }
        public string SelectSqlDisplay { get; set; }
        public string WhereSql { get; set; }
        public string OrderSql { get; set; }
        public byte Prepared { get; set; }
        public string Error { get; set; }
        public int CaseWorkflowId { get; set; }
        public string CreatedUser { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}