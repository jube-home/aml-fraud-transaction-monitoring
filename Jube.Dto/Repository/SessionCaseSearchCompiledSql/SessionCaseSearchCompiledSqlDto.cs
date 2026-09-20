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
namespace Jube.Dto.Repository.SessionCaseSearchCompiledSql
{
    [Description("A Session Case Search compiled to parameterised SQL. Owned by the user that created it.")]
    public class SessionCaseSearchCompiledSqlDto
    {
        [Description("Identifier of the compiled search.")]
        public int Id { get; set; }

        [Description("Guid of the compiled search, used to execute it.")]
        public Guid Guid { get; set; }

        [Description("The query-builder's filter-condition configuration, as JSON. Required on create.")]
        public string? FilterJson { get; set; }

        [Description("The ordered positional parameter values of the compiled SQL, as JSON. Server generated.")]
        public string? FilterTokens { get; set; }

        [Description("The query-builder's selected-column and ordering configuration, as JSON. Required on create.")]
        public string? SelectJson { get; set; }

        [Description("1 when the compiled SQL was successfully prepared against the database; otherwise 0.")]
        public byte Prepared { get; set; }

        [Description("The database error raised when preparing the compiled SQL; null when prepared.")]
        public string? Error { get; set; }

        [Description("Guid of the Case Workflow the search runs against. Required on create.")]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Guid of the saved Case Workflow Filter the search was derived from, if any.")]
        public Guid? CaseWorkflowFilterGuid { get; set; }

        [Description("The user that created the compiled search.")]
        public string? CreatedUser { get; set; }

        [Description("When the compiled search was created (UTC).")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("1 when the compiled search is flagged for rebuild.")]
        public byte Rebuild { get; set; }

        [Description("When the compiled search was last rebuilt (UTC).")]
        public DateTimeOffset? RebuildDate { get; set; }

        [Description("True when the caller has no compiled search yet (returned only by the last-search lookup).")]
        public bool NotFound { get; set; }
    }
}