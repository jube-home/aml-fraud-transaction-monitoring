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

namespace Jube.Dto.Query.RuleVocabulary
{
    [Description("A parameter of a rule function.")]
    public class RuleWordParameterDto
    {
        [Description("The parameter's name.")] public string Name { get; set; } = string.Empty;

        [Description("The parameter's Visual Basic type.")]
        public string Type { get; set; } = string.Empty;

        [Description("Whether it can be left out.")]
        public bool Optional { get; set; }

        [Description("Its value when left out.")]
        public string? DefaultValue { get; set; }
    }
}