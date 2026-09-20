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
namespace Jube.Dto.Query.CaseNoteByCaseKeyValue
{
    public class CaseNoteByCaseKeyValueDto
    {
        [Description("Server-assigned integer identifier of the case note.")]
        public int Id { get; set; }

        [Description("Integer identifier of the case the note is attached to.")]
        public int CaseId { get; set; }

        [Description("UTC date and time at which the note was created.")]
        public DateTime CreatedDate { get; set; }

        [Description("Name of the user who created the note.")]
        public string? CreatedUser { get; set; }

        [Description("Free-text body of the note.")]
        public string? Note { get; set; }

        [Description("Integer identifier of the Case Workflow Action taken when the note was written.")]
        public int ActionId { get; set; }

        [Description("Display name of the Case Workflow Action taken when the note was written.")]
        public string? Action { get; set; }

        [Description("Priority of the note as an integer: 1 High, 2 Medium, 3 Low; any other value reads as Medium.")]
        public int PriorityId { get; set; }

        [Description("Priority of the note as text: High, Medium or Low.")]
        public string? Priority { get; set; }
    }
}