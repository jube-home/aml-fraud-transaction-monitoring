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
namespace Jube.Dto.Query.CaseByCaseKeyValue
{
    [Description("A Case matched by its Case Key and Case Key Value, restricted to Cases whose Case Workflow and " +
                 "Case Workflow Status the caller's role is entitled to see.")]
    public class CaseByCaseKeyValueDto
    {
        [Description("Server-assigned integer identifier of the Case.")]
        public int Id { get; set; }

        [Description(
            "Guid of the Entity Analysis Model Instance Entry (the archived transaction) that created the Case.")]
        public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }

        [Description("Date the Case is diarised until; the default DateTime value when unset.")]
        public DateTime DiaryDate { get; set; }

        [Description("Guid of the Case Workflow the Case belongs to.")]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Name of the Case Workflow Status the Case is currently in.")]
        public string? CaseWorkflowStatusName { get; set; }

        [Description("Date the Case was created; the default DateTime value when unset.")]
        public DateTime CreatedDate { get; set; }

        [Description("True when the Case is currently locked by a user.")]
        public bool Locked { get; set; }

        [Description("User holding the lock on the Case; empty when unlocked.")]
        public string? LockedUser { get; set; }

        [Description("Date the Case was locked; the default DateTime value when unset.")]
        public DateTime LockedDate { get; set; }

        [Description("Identifier of the closed status the Case was closed with; zero when open.")]
        public byte ClosedStatusId { get; set; }

        [Description("Date the Case was closed; the default DateTime value when unset.")]
        public DateTime ClosedDate { get; set; }

        [Description("User that closed the Case; empty when open.")]
        public string? ClosedUser { get; set; }

        [Description("Case Key naming the payload field the Case is keyed on.")]
        public string? CaseKey { get; set; }

        [Description("True when the Case has been diarised.")]
        public bool Diary { get; set; }

        [Description("User that diarised the Case; empty when not diarised.")]
        public string? DiaryUser { get; set; }

        [Description("Rating assigned to the Case.")]
        public byte Rating { get; set; }

        [Description("Case Key Value the Case is keyed on.")]
        public string? CaseKeyValue { get; set; }

        [Description("Closed status the Case last held before it was reopened; zero when never closed.")]
        public byte LastClosedStatus { get; set; }

        [Description("Date the closed status was last migrated; the default DateTime value when unset.")]
        public DateTime ClosedStatusMigrationDate { get; set; }

        [Description("Foreground colour of the current Case Workflow Status.")]
        public string? ForeColor { get; set; }

        [Description("Background colour of the current Case Workflow Status.")]
        public string? BackColor { get; set; }
    }
}