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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Repository.CaseNote
{
    public class CaseNoteDto
    {
        [Description("Server-assigned identifier of the case note. Read-only.")]
        public int Id { get; set; }

        [Description("Free-text note content.")]
        public string Note { get; set; } = string.Empty;

        [Description("Identifier of the CaseWorkflowAction this note is filed against; may dispatch a " +
                     "notification and/or HTTP endpoint if the action has either enabled.")]
        public int ActionId { get; set; }

        [Description("Identifier of the priority allocated to this note.")]
        public int PriorityId { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("The case key this note rolls up to, alongside CaseKeyValue -- notes are addressed by " +
                     "key/value, not by the current Case Id, since a Case Key/Value pair can span multiple cases.")]
        public string CaseKey { get; set; } = string.Empty;

        [Description("Server-assigned username of the note's author. Read-only.")]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("The case key value this note rolls up to.")]
        public string CaseKeyValue { get; set; } = string.Empty;

        [Description("Identifier of the case this note is being added against.")]
        public int CaseId { get; set; }

        [Description("Client-supplied JSON payload used to resolve notification/HTTP-endpoint token " +
                     "substitutions; not persisted.")]
        public string? Payload { get; set; }
    }
}