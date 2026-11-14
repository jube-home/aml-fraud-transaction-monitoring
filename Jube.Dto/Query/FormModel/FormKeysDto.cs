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

namespace Jube.Dto.Query.FormModel
{
    [Description("The properties that identify a record and its parent.")]
    [MessagePackObject]
    public class FormKeysDto
    {
        [Description("The property holding the record identifier.")]
        [Key(0)]
        public string Id { get; set; } = "id";

        [Description("The property holding the identifier of the parent record, when the record belongs to one.")]
        [Key(1)]
        public string? Parent { get; set; }

        [Description("The property that names the record uniquely among its siblings.")]
        [Key(2)]
        public string? NaturalKey { get; set; }
    }
}
