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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Query.CaseJournal
{
    public class CaseJournalDto
    {
        [Description("Column name to declared type ('number', 'string', 'date', 'array', ...) for every column " +
                     "present in each row. Fixed columns are Id, Activation, EntityAnalysisModelInstanceEntryGuid, " +
                     "ReferenceDate, CreatedDate, ResponseElevation, EntryKeyValue and Tag; one further column is " +
                     "added for each Case Workflow XPath the caller's roles may see.")]
        public Dictionary<string, string>? Schema { get; set; }

        [Description("Journal rows, newest Archive entry first. Each row is a column name to value map matching " +
                     "Schema, with optional 'CellFormat' and 'BoldLine' entries carrying conditional formatting.")]
        public List<Dictionary<string, object?>>? Rows { get; set; }
    }
}