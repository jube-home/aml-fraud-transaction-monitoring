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
using Jube.Dto.Query.Svirfd;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseKeyJournal
{
    [Description("The IP intelligence record archived with a transaction beside a fresh lookup of the same address, with the members that changed.")]
    [MessagePackObject]
    public class CaseKeyJournalSvirfdDto
    {
        [Description("The Svirfd entry name.")]
        [Key(0)]
        public string Field { get; set; } = string.Empty;

        [Description("The address that was archived.")]
        [Key(1)]
        public string Ip { get; set; } = string.Empty;

        [Description("The archived record as svirfd.com JSON, or null when the transaction has none.")]
        [Key(2)]
        public string? StoredJson { get; set; }

        [Description("When the archived record's index was loaded.")]
        [Key(3)]
        public DateTimeOffset? StoredAt { get; set; }

        [Description("A fresh lookup of the same address, or null when lookups are unavailable.")]
        [Key(4)]
        public SvirfdLookupDto? Fresh { get; set; }

        [Description("The members whose value differs between the archived and the fresh record.")]
        [Key(5)]
        public List<SvirfdDriftItemDto> Drift { get; set; } = [];
    }
}
