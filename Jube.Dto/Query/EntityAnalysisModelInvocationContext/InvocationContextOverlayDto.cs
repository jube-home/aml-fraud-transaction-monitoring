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

namespace Jube.Dto.Query.EntityAnalysisModelInvocationContext
{
    [Description("A request to set values in a context by completion name, e.g. to test a rule with " +
                 "Abstraction.CountLastHour set to 6.")]
    public class InvocationContextOverlayDto
    {
        [Description("The context to change, as returned by any of the context operations.")]
        public InvocationContextDto? Context { get; set; }

        [Description("The values to set: completion name to invariant text (null clears the value).")]
        public Dictionary<string, string?> Values { get; set; } = [];
    }
}