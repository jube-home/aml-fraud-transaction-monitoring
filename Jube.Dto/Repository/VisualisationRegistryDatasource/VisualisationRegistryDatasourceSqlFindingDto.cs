/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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

namespace Jube.Dto.Repository.VisualisationRegistryDatasource
{
    [MessagePackObject]
    public class VisualisationRegistryDatasourceSqlFindingDto
    {
        [Description("A stable code naming the kind of fault, for example SqlUnknownColumn.")]
        [Key(0)]
        public string Code { get; set; } = string.Empty;

        [Description("What is wrong, in the caller's language. It never contains database error text.")]
        [Key(1)]
        public string Message { get; set; } = string.Empty;

        [Description("The line, counting from 1, of the SQL Command where the fault is. Null when it has no position.")]
        [Key(2)]
        public int? Line { get; set; }

        [Description("The column, counting from 1, on Line. Null when Line is null.")]
        [Key(3)]
        public int? Column { get; set; }

        [Description("The nearest permitted name to the one that was written, when there is one. Only names the " +
                     "caller may already see are ever suggested.")]
        [Key(4)]
        public string? Suggestion { get; set; }
    }
}
