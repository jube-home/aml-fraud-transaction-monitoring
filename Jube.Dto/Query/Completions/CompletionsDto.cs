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
namespace Jube.Dto.Query.Completions
{
    [Description("A single code completion entry describing a payload field or inline script property that " +
                 "can be referenced in a filter or expression. Read-only.")]
    public class CompletionDto
    {
        [Description("Value inserted when the completion is chosen, e.g. Payload.Amount.")]
        public string? Value { get; set; }

        [Description("Display name of the completion, e.g. Payload.Amount.")]
        public string? Name { get; set; }

        [Description("Meta text shown beside the completion, as name:dataType.")]
        public string? Meta { get; set; }

        [Description("Ranking score of the completion; always 1000.")]
        public int Score { get; set; }

        [Description("Filter builder data type: string, integer, double, datetime or boolean.")]
        public string? DataType { get; set; }

        [Description("Group the completion belongs to, e.g. Payload.")]
        public string? Group { get; set; }

        [Description("PostgreSQL JSON path expression used to filter on the field.")]
        public string? Field { get; set; }

        [Description("Dot separated JSON path of the field within the archived payload.")]
        public string? XPath { get; set; }
    }
}