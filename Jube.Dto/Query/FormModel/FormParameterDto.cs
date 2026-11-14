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

namespace Jube.Dto.Query.FormModel
{
    [Description("One named parameter that a choice takes: its kind, default and allowed range.")]
    [MessagePackObject]
    public class FormParameterDto
    {
        [Description("The parameter name, as it is keyed in the stored map of parameter values.")]
        [Key(0)]
        public string Name { get; set; } = string.Empty;

        [Description("The kind of value: number, integer or boolean. A boolean is stored as 1 or 0.")]
        [Key(1)]
        public string Kind { get; set; } = string.Empty;

        [Description("The value used when the parameter is left out of the map.")]
        [Key(2)]
        public double Default { get; set; }

        [Description("The lowest allowed value, or null when there is no lower limit.")]
        [Key(3)]
        public double? Minimum { get; set; }

        [Description("The highest allowed value, or null when there is no upper limit.")]
        [Key(4)]
        public double? Maximum { get; set; }

        [Description("True when the value must be greater than Minimum rather than at least Minimum.")]
        [Key(5)]
        public bool MinimumIsExclusive { get; set; }

        [Description("True when the value must be less than Maximum rather than at most Maximum.")]
        [Key(6)]
        public bool MaximumIsExclusive { get; set; }

        [Description("What the parameter does, in a sentence.")]
        [Key(7)]
        public string Description { get; set; } = string.Empty;
    }
}
