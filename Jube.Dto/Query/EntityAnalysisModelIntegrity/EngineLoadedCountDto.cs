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

namespace Jube.Dto.Query.EntityAnalysisModelIntegrity
{
    [Description("How many entities of a kind an engine instance has loaded.")]
    [MessagePackObject]
    public class EngineLoadedCountDto
    {
        [Description("The kind of entity.")] [Key(0)] public string Kind { get; set; } = string.Empty;
        [Description("How many are loaded.")] [Key(1)] public int Count { get; set; }

        [Description("The entities loaded, ordered by name.")]
        [Key(2)]
        public List<EngineLoadedEntityDto> Entities { get; set; } = [];
    }
}