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

namespace Jube.Dto.Repository.CaseWorkflowXPath
{
    [MessagePackObject]
    public class CaseWorkflowXPathFieldDto
    {
        [Description("The Guid of the model entity; the model Guid when the field is the entry key.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("The field name, which is the archive key.")]
        [Key(1)]
        public string? Name { get; set; }

        [Description("The text shown for the field: its name, its kind and whether it is flagged Archive Key.")]
        [Key(2)]
        public string? Label { get; set; }

        [Description("The kind of model entity, by the EntityApprovalKind numbering.")]
        [Key(3)]
        public int KindId { get; set; }

        [Description("The field data type: 1 String, 2 Integer, 3 Float, 4 Date, 5 Boolean, 6 Latitude, 7 Longitude.")]
        [Key(4)]
        public int DataTypeId { get; set; }

        [Description("Whether the field is flagged Archive Key and so can be drilled and looked up.")]
        [Key(5)]
        public bool ArchiveKey { get; set; }
    }
}
