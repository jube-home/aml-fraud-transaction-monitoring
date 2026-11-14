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


// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Preservation.Models
{
    public sealed class DiffResult
    {
        public IReadOnlyList<DiffSection> Sections { get; init; } = [];
        public IReadOnlyList<DiffEntry> Entries { get; init; } = [];
        public int Added { get; init; }
        public int Removed { get; init; }
        public int Changed { get; init; }
        public int Unchanged { get; init; }
        public int ReferencesIgnored { get; init; }
        public bool Truncated { get; init; }
        public bool Identical => Added == 0 && Removed == 0 && Changed == 0;
    }
}
