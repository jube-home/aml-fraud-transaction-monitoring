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
    [Description("What the tried SQL returned for the case, or why it was refused.")]
    public class CaseWorkflowSqlTestResultDto
    {
        [Description("True when the SQL was accepted and ran.")]
        public bool Ok { get; set; }

        [Description("Why the SQL was refused or failed, in the page language.")]
        public string? Error { get; set; }

        [Description("The columns the SQL returned.")]
        public List<CaseWorkflowSqlColumnDto> Columns { get; set; } = [];

        [Description("The first rows returned, each keyed by column name.")]
        public List<Dictionary<string, object?>> Rows { get; set; } = [];

        [Description("How many rows the SQL returned, up to the limit.")]
        public int RowCount { get; set; }

        [Description("True when the SQL returned more rows than the limit and the rest were left out.")]
        public bool Truncated { get; set; }

        [Description("Milliseconds the SQL took.")]
        public int Milliseconds { get; set; }
    }
}
