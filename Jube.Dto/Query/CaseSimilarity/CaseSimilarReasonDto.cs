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

namespace Jube.Dto.Query.CaseSimilarity
{
    [Description("One fact two similar cases share.")]
    [MessagePackObject]
    public class CaseSimilarReasonDto
    {
        [Description("The kind of fact: 1 Rule, 2 Model, 3 Tag, 4 Entity, 5 Category, 6 Measure, 7 Feature, 8 Band.")]
        [Key(0)]
        public byte KindId { get; set; }

        [Description("The rule, model, tag or field the two cases share. Never a field value.")]
        [Key(1)]
        public string Name { get; set; } = string.Empty;
    }
}
