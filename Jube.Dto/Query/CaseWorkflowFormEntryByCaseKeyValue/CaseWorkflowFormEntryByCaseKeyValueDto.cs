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

namespace Jube.Dto.Query.CaseWorkflowFormEntryByCaseKeyValue
{
    [MessagePackObject]
    public class CaseWorkflowFormEntryByCaseKeyValueDto
    {
        [Description("Server-assigned integer identifier of the Case Workflow Form Entry.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Identifier of the Case the Case Workflow Form Entry was made against.")]
        [Key(1)]
        public int CaseId { get; set; }

        [Description("UTC date and time at which the Case Workflow Form Entry was created.")]
        [Key(2)]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("User name that created the Case Workflow Form Entry.")]
        [Key(3)]
        public string? CreatedUser { get; set; }

        [Description("Legacy response status identifier. Retained for wire compatibility; always 0.")]
        [Key(4)]
        public byte ResponseStatusId { get; set; }

        [Description("Name of the Case Workflow Form the entry was made against.")]
        [Key(5)]
        public string? Name { get; set; }
    }
}