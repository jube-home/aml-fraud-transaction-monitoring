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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseLink
{
    [Description("A request to link two cases, or to remove the link between them.")]
    [MessagePackObject]
    public class CaseLinkRequestDto
    {
        [Description("Identifier of the case being viewed. Required.")]
        [Key(0)]
        public int CaseId { get; set; }

        [Description("Identifier of the case to link it to, or to unlink it from. Required, and not the same case.")]
        [Key(1)]
        public int OtherCaseId { get; set; }

        [Description("Optional free text on why the cases are related, at most 500 characters.")]
        [Key(2)]
        public string? Reason { get; set; }
    }

    [Description("A case linked to the case being viewed.")]
    [MessagePackObject]
    public class CaseLinkDto
    {
        [Description("Identifier of the linked case.")]
        [Key(0)]
        public int OtherCaseId { get; set; }

        [Description("The key field name of the linked case, for example the account.")]
        [Key(1)]
        public string? CaseKey { get; set; }

        [Description("The key value of the linked case.")]
        [Key(2)]
        public string? CaseKeyValue { get; set; }

        [Description("Closed status of the linked case: 0 open, 1 suspended, 2 reopened, 3 closed, 4 bypassed.")]
        [Key(3)]
        public byte ClosedStatusId { get; set; }

        [Description("Why the cases were linked.")]
        [Key(4)]
        public string? Reason { get; set; }

        [Description("Who made the link.")]
        [Key(5)]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("When the link was made (UTC).")]
        [Key(6)]
        public DateTimeOffset CreatedDate { get; set; }
    }
}
