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

namespace Jube.Dto.Query.WireSchema
{
    [Description("One serialised member of a wire type: its position in the MessagePack array, the camel-cased name " +
                 "the JSON form uses, and the shape of its value.")]
    [MessagePackObject]
    public class WireFieldDto
    {
        [Description("Zero-based position of the member in the MessagePack array.")]
        [Key(0)]
        public int Index { get; set; }

        [Description("Camel-cased member name, identical to the property name in the JSON form.")]
        [Key(1)]
        public string Name { get; set; } = string.Empty;

        [Description("Shape of the value: a primitive tag (string, char, int, double, bool, guid, date, any), a wire type " +
                     "name, or either of those followed by [] for a list or {} for a string-keyed dictionary, or {}# for a list of such dictionaries sent column by column as [rowCount, columnNames, columns, sparse, nullRows], where sparse lists [column, missingRows] pairs. A trailing ? marks a nullable value type; a value type without it has no null on the wire and takes its default when the client omits it.")]
        [Key(2)]
        public string Type { get; set; } = string.Empty;
    }
}
