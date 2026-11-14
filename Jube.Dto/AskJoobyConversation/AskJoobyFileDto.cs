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
    [Description("Metadata for one file attached to an AskJooby conversation. Never carries the file's " +
                 "own bytes -- those are only ever returned by the dedicated download endpoint.")]
    [MessagePackObject]
    public class AskJoobyFileDto
    {
        [Description("Server-assigned row identifier. Read-only.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("The conversation this file is attached to.")]
        [Key(1)]
        public int AskJoobyConversationId { get; set; }

        [Description("The original file name.")]
        [Key(2)]
        public string? Name { get; set; }

        [Description("The uploaded file's content type.")]
        [Key(3)]
        public string? ContentType { get; set; }

        [Description("The file size in bytes.")]
        [Key(4)]
        public long? Size { get; set; }

        [Description("User who uploaded this file. Server-assigned. Read-only.")]
        [Key(5)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this file was uploaded. Server-assigned. Read-only.")]
        [Key(6)]
        public DateTimeOffset? CreatedDate { get; set; }
    }
}