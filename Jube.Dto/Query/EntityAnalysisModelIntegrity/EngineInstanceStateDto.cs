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
    [Description("What one engine instance has loaded for the model.")]
    [MessagePackObject]
    public class EngineInstanceStateDto
    {
        [Description("The instance's host name.")]
        [Key(0)]
        public string Instance { get; set; } = string.Empty;

        [Description("Whether the instance has started the model.")]
        [Key(1)]
        public bool Started { get; set; }

        [Description("When this state was read or recorded.")]
        [Key(2)]
        public DateTime CapturedDate { get; set; }

        [Description("How many entities of each kind are loaded.")]
        [Key(3)]
        public List<EngineLoadedCountDto> Loaded { get; set; } = [];
    }
}