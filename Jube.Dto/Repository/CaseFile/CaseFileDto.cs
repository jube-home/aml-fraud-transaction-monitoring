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
namespace Jube.Dto.Repository.CaseFile
{
    public class CaseFileDto
    {
        [Description("Server-assigned unique identifier of the file attachment. Read-only.")]
        public int Id { get; set; }

        [Description("Server-assigned UTC timestamp of when the file was uploaded. Read-only.")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Name of the key field (e.g. an account number field) that identifies the transaction " +
                     "this file is attached to, matching the Archive payload's key.")]
        public string? CaseKey { get; set; }

        [Description("Server-assigned name of the user who uploaded the file. Read-only.")]
        public string? CreatedUser { get; set; }

        [Description("Value of the key field that identifies the transaction this file is attached to.")]
        public string? CaseKeyValue { get; set; }

        [Description("Identifier of the Case this file is attached to.")]
        public int CaseId { get; set; }

        [Description("Server-assigned UTC timestamp of when the file was removed, if it has been. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }

        [Description("Server-assigned name of the user who removed the file, if it has been. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("When true, the file has been removed and is no longer attached to the Case. Read-only.")]
        public bool Deleted { get; set; }

        [Description("Original filename as uploaded, including extension.")]
        public string? Name { get; set; }

        [Description("File extension, including the leading period (e.g. \".pdf\").")]
        public string? Extension { get; set; }

        [Description("MIME content type of the file as uploaded (e.g. \"application/pdf\").")]
        public string? ContentType { get; set; }

        [Description("Size of the file in bytes.")]
        public int Size { get; set; }
    }
}