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

namespace Jube.Dto.Query.CaseEventByCaseKeyValue
{
    [MessagePackObject]
    public class CaseEventByCaseKeyValueDto
    {
        [Description("Server-assigned integer identifier of the case event.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Identifier of the case this event belongs to.")]
        [Key(1)]
        public int CaseId { get; set; }

        [Description(
            "Numeric case event type; the display of Before and After depends on it. 1=Automatic Unlock, 2=Skim Case, 3=Automatic Lock, 4=Full Case Fetch, 5=Closed, 6=Manual Lock, 7=Diary, 8=Workflow Change, 9=Status Change, 10=Diary Date Change, 11=Rating Change, 12=Suspend Closed, 13=Suspend Open, 14=Allocate Lock, 15=Process Expired Case, 16=Peek, 17=Automatic Escalation, 18=Lock Broken, 19=Diary Cleared, 20=QA Sampled, 21 to 28 the checklist events.")]
        [Key(2)]
        public int CaseEventTypeId { get; set; }

        [Description(
            "Human-readable case event type, for example 'Workflow Change' or 'Closed'; 'Unknown' when the type is unrecognised.")]
        [Key(3)]
        public string? CaseEventType { get; set; }

        [Description("UTC timestamp at which the event was created.")]
        [Key(4)]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("User name that caused the event.")]
        [Key(5)]
        public string? CreatedUser { get; set; }

        [Description("State of the case before the event, where recorded. Stored as the raw value: an ISO 8601 UTC date, an enumeration id, a status Guid, a user name or plain text, according to CaseEventTypeId.")]
        [Key(6)]
        public string? Before { get; set; }

        [Description("State of the case after the event, where recorded. Stored as the raw value in the same way as Before.")]
        [Key(7)]
        public string? After { get; set; }
    }
}