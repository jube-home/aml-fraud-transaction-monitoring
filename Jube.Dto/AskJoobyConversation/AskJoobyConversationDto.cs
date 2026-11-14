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

namespace Jube.Dto.AskJoobyConversation
{
    [MessagePackObject]
    public class AskJoobyConversationDto
    {
        [Description("Server-assigned row identifier. Read-only.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Globally-addressable identifier for this conversation. Server-assigned. Read-only.")]
        [Key(1)]
        public Guid? Guid { get; set; }

        [Description("A short, auto-generated title summarising the conversation's first message. Null " +
                     "until the first reply completes.")]
        [Key(2)]
        public string? Title { get; set; }

        [Description("User who started this conversation. Server-assigned. Read-only.")]
        [Key(3)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this conversation was started. Server-assigned. Read-only.")]
        [Key(4)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last sent a message in this conversation. Server-assigned. Read-only.")]
        [Key(5)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) of the most recent message. Server-assigned. Read-only.")]
        [Key(6)]
        public DateTimeOffset? UpdatedDate { get; set; }
    }
}