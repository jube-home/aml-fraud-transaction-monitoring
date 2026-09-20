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
namespace Jube.Dto.Repository.CaseWorkflowFormEntry
{
    public class CaseWorkflowFormEntryDto
    {
        [Description("Server-assigned identifier of the form entry. Read-only.")]
        public int Id { get; set; }

        [Description("The submitted field name/value pairs, keyed by the CaseWorkflowForm's HTML field ids; " +
                     "persisted as one CaseWorkflowFormEntryValue row per non-null entry and also used to resolve " +
                     "notification/HTTP-endpoint token substitutions.")]
        public Dictionary<string, object?>? Payload { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned username of the submitter. Read-only.")]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("The case key this entry rolls up to, alongside CaseKeyValue.")]
        public string CaseKey { get; set; } = string.Empty;

        [Description("Identifier of the case this form is being submitted against.")]
        public int CaseId { get; set; }

        [Description("Identifier of the CaseWorkflowForm template this entry is a submission of; dispatches that " +
                     "form's notification and/or HTTP endpoint, if either is enabled.")]
        public int CaseWorkflowFormId { get; set; }

        [Description("The case key value this entry rolls up to.")]
        public string CaseKeyValue { get; set; } = string.Empty;
    }
}