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
using Jube.Dto.Repository.CaseFile;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseResearch
{
    [MessagePackObject]
    public class CaseResearchDto
    {
        [Description("Server-assigned identifier of the research. Read-only.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Identifier of the case the research is captured against. The research rolls up to the " +
                     "case's key and value, like a note.")]
        [Key(1)]
        public int CaseId { get; set; }

        [Description("The case key of the case. Server-assigned and read-only.")]
        [Key(2)]
        public string? CaseKey { get; set; }

        [Description("The case key value of the case. Server-assigned and read-only.")]
        [Key(3)]
        public string? CaseKeyValue { get; set; }

        [Description("The topic or question researched, up to 500 characters. Required.")]
        [Key(4)]
        public string Topic { get; set; } = string.Empty;

        [Description("What the research found, up to 100000 characters. Required.")]
        [Key(5)]
        public string Findings { get; set; } = string.Empty;

        [Description("Who captured it as an integer: 1 User, 2 AskJooby (written by the chat agent for the " +
                     "calling user). Server-assigned and read-only.")]
        [Key(6)]
        public int SourceId { get; set; }

        [Description("The sources the findings rely on, at most 10. Only attach real addresses of sources that " +
                     "were actually retrieved.")]
        [Key(7)]
        public List<CaseResearchSourceDto> Sources { get; set; } = [];

        [Description("The files attached to the research, without their content: name, type, size, who uploaded " +
                     "them and when. Download one with its Id.")]
        [Key(8)]
        public List<CaseFileDto> Files { get; set; } = [];

        [Description("Server-assigned name of the user the research was captured for. Read-only.")]
        [Key(9)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned UTC creation time. Read-only.")]
        [Key(10)]
        public DateTimeOffset? CreatedDate { get; set; }
    }
}
