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

namespace Jube.Dto.Repository.CaseFile
{
    [Description("A file to attach to a case note or case research of a Case, with the transaction identity it " +
                 "belongs to, carried in one serialised body.")]
    [MessagePackObject]
    public class CaseFileUploadRequestDto
    {
        [Description("Name of the key field identifying the transaction the file attaches to.")]
        [Key(0)]
        public string? CaseKey { get; set; }

        [Description("Value of the key field identifying the transaction the file attaches to.")]
        [Key(1)]
        public string? CaseKeyValue { get; set; }

        [Description("Identifier of the Case the file attaches to.")]
        [Key(2)]
        public int CaseId { get; set; }

        [Description("Identifier of the case note, written by the caller on that Case, to attach the file to. " +
                     "Omit to have a note created for the file, or when attaching to case research.")]
        [Key(3)]
        public int? CaseNoteId { get; set; }

        [Description("Identifier of the case research, captured by the caller on that Case, to attach the file to. " +
                     "Send either a note or research, never both.")]
        [Key(4)]
        public int? CaseResearchId { get; set; }

        [Description("The original name of the file; sanitised before it is stored.")]
        [Key(5)]
        public string? FileName { get; set; }

        [Description("The media type the client declared for the file.")]
        [Key(6)]
        public string? ContentType { get; set; }

        [Description("The raw bytes of the file: base64 text in a JSON body, a binary value in MessagePack.")]
        [Key(7)]
        public byte[]? Content { get; set; }
    }
}
