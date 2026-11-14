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

namespace Jube.Dto.Repository.CaseTag
{
    [MessagePackObject]
    public class CaseTagDto
    {
        [Description("Server-assigned identifier of the case tag. Read-only.")]
        [Key(0)]
        public long Id { get; set; }

        [Description("Identifier of the case the tag is applied to.")]
        [Key(1)]
        public int CaseId { get; set; }

        [Description("The tag name, one of the model's tag vocabulary.")]
        [Key(2)]
        public string? Name { get; set; }

        [Description("User who applied the tag. Read-only.")]
        [Key(3)]
        public string? CreatedUser { get; set; }

        [Description("When the tag was applied. Read-only.")]
        [Key(4)]
        public DateTime? CreatedDate { get; set; }
    }
}
