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

namespace Jube.Dto.Query.CaseSearch
{
    [Description("One entry of the Cases page rail with how many cases it holds.")]
    [MessagePackObject]
    public class CaseSearchCountDto
    {
        [Description("Kind of entry: View for a built-in view, Workflow for a Case Workflow Filter an administrator " +
                     "defined, Own for a search the caller saved or one shared with their role.")]
        [Key(0)]
        public string Kind { get; set; } = string.Empty;

        [Description("Key to run it: the view number for a View, the Guid for the others.")]
        [Key(1)]
        public string Key { get; set; } = string.Empty;

        [Description("Name shown in the rail.")]
        [Key(2)]
        public string Name { get; set; } = string.Empty;

        [Description("How many open and closed cases it matches.")]
        [Key(3)]
        public long Count { get; set; }

        [Description("True when the caller owns it (Own entries only).")]
        [Key(4)]
        public bool Mine { get; set; }

        [Description("True when counting failed, for example because a saved search no longer compiles.")]
        [Key(5)]
        public bool Failed { get; set; }
    }
}
