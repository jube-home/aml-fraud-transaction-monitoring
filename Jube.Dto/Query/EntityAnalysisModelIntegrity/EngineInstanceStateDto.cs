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

namespace Jube.Dto.Query.EntityAnalysisModelIntegrity
{
    [Description("What one engine instance has loaded for the model.")]
    public class EngineInstanceStateDto
    {
        [Description("The instance's host name.")]
        public string Instance { get; set; } = string.Empty;

        [Description("Whether the instance has started the model.")]
        public bool Started { get; set; }

        [Description("When this state was read or recorded.")]
        public DateTime CapturedDate { get; set; }

        [Description("How many entities of each kind are loaded.")]
        public List<EngineLoadedCountDto> Loaded { get; set; } = [];
    }
}