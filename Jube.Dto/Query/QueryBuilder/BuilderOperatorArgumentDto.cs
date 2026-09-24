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

namespace Jube.Dto.Query.QueryBuilder
{
    [Description("A value an operator takes.")]
    public class BuilderOperatorArgumentDto
    {
        [Description("What the value is, e.g. threshold.")]
        public string Name { get; set; } = string.Empty;

        [Description("value (the field's own type), number, integer, text, date, list (a model list) or field " +
                     "(another field of the same type).")]
        public string Kind { get; set; } = string.Empty;

        [Description("True when several values are given, separated by commas.")]
        public bool Many { get; set; }
    }
}