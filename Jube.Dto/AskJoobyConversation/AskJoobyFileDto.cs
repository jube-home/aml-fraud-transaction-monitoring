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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.AskJoobyConversation
{
    [Description("Metadata for one file attached to an AskJooby conversation. Never carries the file's " +
                 "own bytes -- those are only ever returned by the dedicated download endpoint.")]
    public class AskJoobyFileDto
    {
        [Description("Server-assigned row identifier. Read-only.")]
        public int Id { get; set; }

        [Description("The conversation this file is attached to.")]
        public int AskJoobyConversationId { get; set; }

        [Description("The original file name.")]
        public string? Name { get; set; }

        [Description("The uploaded file's content type.")]
        public string? ContentType { get; set; }

        [Description("The file size in bytes.")]
        public long? Size { get; set; }

        [Description("User who uploaded this file. Server-assigned. Read-only.")]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this file was uploaded. Server-assigned. Read-only.")]
        public DateTimeOffset? CreatedDate { get; set; }
    }
}