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

namespace Jube.Dto.Repository.CaseNote
{
    [MessagePackObject]
    public class CaseNoteDto
    {
        [Description("Server-assigned identifier of the case note. Read-only.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Free-text note content.")]
        [Key(1)]
        public string Note { get; set; } = string.Empty;

        [Description("Identifier of the CaseWorkflowAction this note is filed against; may dispatch a " +
                     "notification and/or HTTP endpoint if the action has either enabled.")]
        [Key(2)]
        public int ActionId { get; set; }

        [Description("Identifier of the priority allocated to this note.")]
        [Key(3)]
        public int PriorityId { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [Key(4)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("The case key this note rolls up to, alongside CaseKeyValue -- notes are addressed by " +
                     "key/value, not by the current Case Id, since a Case Key/Value pair can span multiple cases.")]
        [Key(5)]
        public string CaseKey { get; set; } = string.Empty;

        [Description("Server-assigned username of the note's author. Read-only.")]
        [Key(6)]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("The case key value this note rolls up to.")]
        [Key(7)]
        public string CaseKeyValue { get; set; } = string.Empty;

        [Description("Identifier of the case this note is being added against.")]
        [Key(8)]
        public int CaseId { get; set; }

        [Description("True when the author marks their own note as a finding. Can also be changed afterwards by " +
                     "the author through the finding operation.")]
        [Key(9)]
        public bool IsFinding { get; set; }

        [Description("Who wrote the note as an integer: 1 User, 2 System, 3 AskJooby. Server-assigned and " +
                     "read-only; a value supplied by the caller is ignored.")]
        [Key(10)]
        public int SourceId { get; set; }

        [Description("What the note is as an integer: 1 Note, 2 Narrative, 4 Document. Server-assigned and read-only; " +
                     "a value supplied by the caller is ignored and the note is stored as a Note.")]
        [Key(11)]
        public int KindId { get; set; }

        [Description("Revision number of a Narrative note within its Case. Server-assigned and read-only.")]
        [Key(12)]
        public int Version { get; set; }

        [Description("The files attached to the note, without their content. Read-only.")]
        [Key(13)]
        public List<CaseFileDto>? Files { get; set; }

        [Description("Client-supplied JSON payload used to resolve notification/HTTP-endpoint token " +
                     "substitutions; not persisted.")]
        [Key(14)]
        public string? Payload { get; set; }
    }
}