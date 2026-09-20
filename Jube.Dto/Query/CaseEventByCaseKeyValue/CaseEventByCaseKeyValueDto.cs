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
namespace Jube.Dto.Query.CaseEventByCaseKeyValue
{
    public class CaseEventByCaseKeyValueDto
    {
        [Description("Server-assigned integer identifier of the case event.")]
        public int Id { get; set; }

        [Description("Identifier of the case this event belongs to.")]
        public int CaseId { get; set; }

        [Description(
            "Human-readable case event type, for example 'Workflow Change' or 'Closed'; 'Unknown' when the type is unrecognised.")]
        public string? CaseEventType { get; set; }

        [Description("UTC timestamp at which the event was created.")]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("User name that caused the event.")]
        public string? CreatedUser { get; set; }

        [Description("Serialised state of the case before the event, where recorded.")]
        public string? Before { get; set; }

        [Description("Serialised state of the case after the event, where recorded.")]
        public string? After { get; set; }
    }
}