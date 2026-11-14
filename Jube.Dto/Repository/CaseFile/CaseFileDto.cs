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
    [MessagePackObject]
    public class CaseFileDto
    {
        [Description("Server-assigned unique identifier of the file attachment. Read-only.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Server-assigned UTC timestamp of when the file was uploaded. Read-only.")]
        [Key(1)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Name of the key field (e.g. an account number field) that identifies the transaction " +
                     "this file is attached to, matching the Archive payload's key.")]
        [Key(2)]
        public string? CaseKey { get; set; }

        [Description("Server-assigned name of the user who uploaded the file. Read-only.")]
        [Key(3)]
        public string? CreatedUser { get; set; }

        [Description("Value of the key field that identifies the transaction this file is attached to.")]
        [Key(4)]
        public string? CaseKeyValue { get; set; }

        [Description("Identifier of the Case this file is attached to.")]
        [Key(5)]
        public int CaseId { get; set; }

        [Description("Identifier of the case note this file is attached to, or null when it is attached to " +
                     "research. Exactly one of CaseNoteId and CaseResearchId is set. Read-only.")]
        [Key(6)]
        public int? CaseNoteId { get; set; }

        [Description("Identifier of the case research this file is attached to, or null when it is attached to " +
                     "a note. Exactly one of CaseNoteId and CaseResearchId is set. Read-only.")]
        [Key(7)]
        public int? CaseResearchId { get; set; }

        [Description("Server-assigned UTC timestamp of when the file was removed, if it has been. Read-only.")]
        [Key(8)]
        public DateTimeOffset? DeletedDate { get; set; }

        [Description("Server-assigned name of the user who removed the file, if it has been. Read-only.")]
        [Key(9)]
        public string? DeletedUser { get; set; }

        [Description("When true, the file has been removed and is no longer attached to the Case. Read-only.")]
        [Key(10)]
        public bool Deleted { get; set; }

        [Description("Original filename as uploaded, including extension.")]
        [Key(11)]
        public string? Name { get; set; }

        [Description("File extension, including the leading period (e.g. \".pdf\").")]
        [Key(12)]
        public string? Extension { get; set; }

        [Description("MIME content type of the file as uploaded (e.g. \"application/pdf\").")]
        [Key(13)]
        public string? ContentType { get; set; }

        [Description("Size of the file in bytes.")]
        [Key(14)]
        public int Size { get; set; }

        [Description("Lower-case hex SHA-256 of the stored bytes; null for files stored before hashing began.")]
        [Key(15)]
        public string? Hash { get; set; }
    }
}