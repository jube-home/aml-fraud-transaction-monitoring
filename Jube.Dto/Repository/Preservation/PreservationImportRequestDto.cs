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

namespace Jube.Dto.Repository.Preservation
{
    [Description("A Jube Export Model Package (.jemp) supplied to an import or an import preview, with the password " +
                 "it was exported with. Both travel in the one serialised body, never in a form, query string or header.")]
    [MessagePackObject]
    public class PreservationImportRequestDto
    {
        [Description("The password the package was exported with.")]
        [Key(0)]
        public string? Password { get; set; }

        [Description("The original name of the package file. Informational only; never used as a path.")]
        [Key(1)]
        public string? FileName { get; set; }

        [Description("The raw bytes of the .jemp package: base64 text in a JSON body, a binary value in MessagePack.")]
        [Key(2)]
        public byte[]? Package { get; set; }
    }
}
