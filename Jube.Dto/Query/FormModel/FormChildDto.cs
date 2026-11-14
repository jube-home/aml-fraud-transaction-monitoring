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

namespace Jube.Dto.Query.FormModel
{
    [Description("A kind of record that belongs under another, reached by drilling down from it.")]
    [MessagePackObject]
    public class FormChildDto
    {
        [Description("The area of the child records, as named in its own form model.")]
        [Key(0)]
        public string Area { get; set; } = string.Empty;

        [Description("The text shown for the child kind, such as on a hub card.")]
        [Key(1)]
        public string Label { get; set; } = string.Empty;

        [Description("The name of the group of child kinds this one is shown in; empty when it stands alone.")]
        [Key(2)]
        public string Hub { get; set; } = string.Empty;

        [Description("Where the child kind sits among its siblings; lower first.")]
        [Key(3)]
        public int Order { get; set; }

        [Description("The property of the child records that holds the parent's identifier, in camel case.")]
        [Key(4)]
        public string ParentKey { get; set; } = string.Empty;

        [Description("The route that lists the child records of one parent; the parent's identifier is added to the " +
                     "end.")]
        [Key(5)]
        public string ListRoute { get; set; } = string.Empty;

        [Description("Which property of the parent record is added to the end of the route: id or guid.")]
        [Key(6)]
        public string ParentField { get; set; } = "id";

        public FormChildDto Copy()
        {
            return (FormChildDto)MemberwiseClone();
        }
    }
}
