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

namespace Jube.Dto.Query.Svirfd
{
    [Description("The IP intelligence record for one address, exactly as svirfd.com returns it, with its narrative and the credit that must be shown wherever it is displayed.")]
    [MessagePackObject]
    public class SvirfdLookupDto
    {
        [Description("The address that was looked up.")]
        [Key(0)]
        public string Ip { get; set; } = string.Empty;

        [Description("The whole record as svirfd.com JSON (camelCase), unchanged. A null means no public source says, never false.")]
        [Key(1)]
        public string RecordJson { get; set; } = string.Empty;

        [Description("The one-line plain-English headline of the record.")]
        [Key(2)]
        public string? Headline { get; set; }

        [Description("The deterministic narrative groups of the record.")]
        [Key(3)]
        public List<SvirfdNarrativeGroupDto> Narrative { get; set; } = [];

        [Description("The version of the narrative wording.")]
        [Key(4)]
        public int NarrativeVersion { get; set; }

        [Description("The credit that must be shown wherever this data is shown.")]
        [Key(5)]
        public string Attribution { get; set; } = string.Empty;

        [Description("Where the data licence is published.")]
        [Key(6)]
        public string LicenceUrl { get; set; } = string.Empty;

        [Description("How the lookup was answered: Local, Directory or Remote.")]
        [Key(7)]
        public string Mode { get; set; } = string.Empty;

        [Description("The change-log sequence the local index is at, or -1 when answered remotely.")]
        [Key(8)]
        public long Seq { get; set; }

        [Description("An SVG chart of each probe's round trip time against its distance from the placed location, with the physical bound, or empty when there are fewer than two located probes.")]
        [Key(9)]
        public string? RttChartSvg { get; set; }
    }
}
