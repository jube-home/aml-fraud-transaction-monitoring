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
    [Description("One use of an entity by another, drawn from the user to the used.")]
    public class ModelGraphEdgeDto
    {
        [Description("The id of the node that uses the other.")]
        public string From { get; set; } = string.Empty;

        [Description("The id of the node that is used.")]
        public string To { get; set; } = string.Empty;

        [Description("How it is used: the name in rule text, or the setting.")]
        public string Label { get; set; } = string.Empty;

        [Description("True for a use through a setting rather than rule text.")]
        public bool Dashed { get; set; }
    }
}