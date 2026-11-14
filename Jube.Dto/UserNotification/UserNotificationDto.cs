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

using System;
using System.ComponentModel;
using MessagePack;

namespace Jube.Dto.UserNotification
{
    [Description("An in-app notification for the signed-in user.")]
    [MessagePackObject]
    public class UserNotificationDto
    {
        [Description("The identifier used to mark the notification read.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("What happened, as a NotificationKind: 1 case service level breached.")]
        [Key(1)]
        public short KindId { get; set; }

        [Description("Priority, as a NotificationPriority: 1 low, 2 normal, 3 high.")]
        [Key(2)]
        public short PriorityId { get; set; }

        [Description("The case the notification is about, when it is about one.")]
        [Key(3)]
        public int? CaseId { get; set; }

        [Description("JSON facts the client renders into text, for example the service level dimension.")]
        [Key(4)]
        public string? ArgumentsJson { get; set; }

        [Description("When the notification was created (UTC).")]
        [Key(5)]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("True once the signed-in user has opened the notification.")]
        [Key(6)]
        public bool Read { get; set; }
    }
}
