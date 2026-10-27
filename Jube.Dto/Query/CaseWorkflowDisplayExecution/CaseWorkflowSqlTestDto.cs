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

using System.ComponentModel;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseWorkflowDisplayExecution
{
    [Description("Request to try the SQL of a Form, Display, Macro, Action or Status against a case, without saving it.")]
    public class CaseWorkflowSqlTestDto
    {
        [Description("The SQL to try: a single SELECT that may use @Id, the id of the case, and @CaseKeyValue.")]
        public string Sql { get; set; } = string.Empty;

        [Description("Integer Id of the case to run it for; supplies @Id and @CaseKeyValue.")]
        public int CaseId { get; set; }
    }
}
