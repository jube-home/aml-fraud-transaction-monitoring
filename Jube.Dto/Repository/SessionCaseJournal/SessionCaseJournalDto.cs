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
namespace Jube.Dto.Repository.SessionCaseJournal
{
    public class SessionCaseJournalDto
    {
        [Description("Server-assigned identifier of the journal record. Read-only.")]
        public int Id { get; set; }

        [Description("The saved journal content as a JSON string (for example the user's chosen case grid " +
                     "column layout).")]
        public string? Json { get; set; }

        [Description("Server-assigned username of the journal's owner. Read-only.")]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned timestamp of the last save. Read-only.")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Guid of the CaseWorkflow the journal is kept against. One journal is kept per user per " +
                     "CaseWorkflow; saving again replaces it.")]
        public Guid CaseWorkflowGuid { get; set; }
    }
}