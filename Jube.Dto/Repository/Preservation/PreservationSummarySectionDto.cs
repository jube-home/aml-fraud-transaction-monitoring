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
    [MessagePackObject]
    public class PreservationSummarySectionDto
    {
        [Description("Which include switch the section belongs to: models, visualisations, roles, lists, " +
                     "dictionaries, exhaustive, advancedAnalytics or approvals.")]
        [Key(0)]
        public string Key { get; set; } = string.Empty;

        [Description("How many top level entities, such as models, lists or roles.")]
        [Key(1)]
        public int Count { get; set; }

        [Description("How many entities are nested beneath them, such as list values or permissions.")]
        [Key(2)]
        public int Rows { get; set; }

        [Description("The size of the section serialised, before compression and encryption.")]
        [Key(3)]
        public long Bytes { get; set; }
    }
}
