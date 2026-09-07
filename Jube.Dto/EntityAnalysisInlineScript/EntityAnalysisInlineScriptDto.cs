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

namespace Jube.Dto.EntityAnalysisInlineScript
{
    [Description("A registered, compiled Inline Script available system-wide for any Model to invoke. Read-only " +
                 "-- rows are created and compiled outside the API.")]
    public class EntityAnalysisInlineScriptDto
    {
        [Description("Server-assigned row identifier.")]
        public int Id { get; set; }

        [Description("Display name of the Inline Script, shown in the selection dropdown when registering it " +
                     "against a Model.")]
        public string? Name { get; set; }
    }
}