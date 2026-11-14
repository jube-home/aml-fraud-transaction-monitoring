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

namespace Jube.Dto.Query.CaseNoteByCaseKeyValue
{
    [MessagePackObject]
    public class CaseNoteByCaseKeyValueDto
    {
        [Description("Server-assigned integer identifier of the case note.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Integer identifier of the case the note is attached to.")]
        [Key(1)]
        public int CaseId { get; set; }

        [Description("UTC date and time at which the note was created.")]
        [Key(2)]
        public DateTime CreatedDate { get; set; }

        [Description("Name of the user who created the note.")]
        [Key(3)]
        public string? CreatedUser { get; set; }

        [Description("Free-text body of the note.")]
        [Key(4)]
        public string? Note { get; set; }

        [Description("Integer identifier of the Case Workflow Action taken when the note was written.")]
        [Key(5)]
        public int ActionId { get; set; }

        [Description("Display name of the Case Workflow Action taken when the note was written.")]
        [Key(6)]
        public string? Action { get; set; }

        [Description("Priority of the note as an integer: 1 High, 2 Medium, 3 Low; any other value reads as Medium.")]
        [Key(7)]
        public int PriorityId { get; set; }

        [Description("Priority of the note as text: High, Medium or Low.")]
        [Key(8)]
        public string? Priority { get; set; }

        [Description("Who wrote the note as an integer: 1 User, 2 System, 3 AskJooby.")]
        [Key(9)]
        public int SourceId { get; set; }

        [Description("What the note is as an integer: 1 Note, 2 Narrative (an AskJooby case narrative, one row per " +
                     "version), 4 Document (carries uploaded files).")]
        [Key(10)]
        public int KindId { get; set; }

        [Description("True when the author marked their own note as a finding.")]
        [Key(11)]
        public bool IsFinding { get; set; }

        [Description("Revision number of a Narrative note within its Case, starting at 1. 1 for every other kind.")]
        [Key(12)]
        public int Version { get; set; }

        [Description("Id of the earlier Narrative note this one replaces, or null for the first version and every " +
                     "other kind.")]
        [Key(13)]
        public int? SupersedesNoteId { get; set; }

        [Description("The files attached to the note, without their content: name, type, size, who uploaded them " +
                     "and when. Download one with its Id.")]
        [Key(14)]
        public List<CaseFileDto> Files { get; set; } = [];

        [Description("The recorded AskJooby run a Narrative note was generated from, or null for every other " +
                     "note.")]
        [Key(15)]
        public int? AskJoobyConversationId { get; set; }
    }
}