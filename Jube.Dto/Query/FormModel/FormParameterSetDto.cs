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
    [Description("The parameters one value of a choice field takes, such as the parameters of one aggregation " +
                 "function. A value with no entry takes no parameters.")]
    [MessagePackObject]
    public class FormParameterSetDto
    {
        [Description("The value of the choice field this set belongs to.")]
        [Key(0)]
        public int Value { get; set; }

        [Description("The name of the choice, as the catalogue knows it.")]
        [Key(1)]
        public string Name { get; set; } = string.Empty;

        [Description("What the choice computes, in a sentence or two.")]
        [Key(2)]
        public string Description { get; set; } = string.Empty;

        [Description("True when the result depends on the order the values arrive in.")]
        [Key(3)]
        public bool OrderSensitive { get; set; }

        [Description("The parameters the choice takes, in declaration order; empty when it takes none.")]
        [Key(4)]
        public List<FormParameterDto> Parameters { get; set; } = [];
    }
}
