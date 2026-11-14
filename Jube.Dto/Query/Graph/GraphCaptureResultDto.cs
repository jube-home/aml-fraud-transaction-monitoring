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

namespace Jube.Dto.Query.Graph
{
    [MessagePackObject]
    public class GraphCaptureResultDto
    {
        [Description("The case file holding the SVG.")]
        [Key(0)]
        public int SvgCaseFileId { get; set; }

        [Description("The case file holding the capture definition (query, layout, positions, archive ids) as JSON.")]
        [Key(1)]
        public int DefinitionCaseFileId { get; set; }

        [Description("The case note both files hang from.")]
        [Key(2)]
        public int CaseNoteId { get; set; }

        [Description("Lower-case hex SHA-256 of the SVG bytes.")]
        [Key(3)]
        public string? Hash { get; set; }

        [Description("Nodes drawn.")]
        [Key(4)]
        public int Nodes { get; set; }

        [Description("Edges drawn.")]
        [Key(5)]
        public int Edges { get; set; }
    }
}
