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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.SessionCaseSearchCompiledSql
{
    [Description("A Session Case Search compiled to parameterised SQL. Owned by the user that created it.")]
    [MessagePackObject]
    public class SessionCaseSearchCompiledSqlDto
    {
        [Description("Identifier of the compiled search.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Guid of the compiled search, used to execute it.")]
        [Key(1)]
        public Guid Guid { get; set; }

        [Description("The query-builder's filter-condition configuration, as JSON. Required on create.")]
        [Key(2)]
        public string? FilterJson { get; set; }

        [Description("The ordered positional parameter values of the compiled SQL, as JSON. Server generated.")]
        [Key(3)]
        public string? FilterTokens { get; set; }

        [Description("The query-builder's selected-column and ordering configuration, as JSON. Required on create.")]
        [Key(4)]
        public string? SelectJson { get; set; }

        [Description("1 when the compiled SQL was successfully prepared against the database; otherwise 0.")]
        [Key(5)]
        public byte Prepared { get; set; }

        [Description("The database error raised when preparing the compiled SQL; null when prepared.")]
        [Key(6)]
        public string? Error { get; set; }

        [Description("Guid of the Case Workflow the search runs against. Required on create.")]
        [Key(7)]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Guid of the saved Case Workflow Filter the search was derived from, if any.")]
        [Key(8)]
        public Guid? CaseWorkflowFilterGuid { get; set; }

        [Description("The user that created the compiled search.")]
        [Key(9)]
        public string? CreatedUser { get; set; }

        [Description("When the compiled search was created (UTC).")]
        [Key(10)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("1 when the compiled search is flagged for rebuild.")]
        [Key(11)]
        public byte Rebuild { get; set; }

        [Description("When the compiled search was last rebuilt (UTC).")]
        [Key(12)]
        public DateTimeOffset? RebuildDate { get; set; }

        [Description("True when the caller has no compiled search yet (returned only by the last-search lookup).")]
        [Key(13)]
        public bool NotFound { get; set; }
    }
}