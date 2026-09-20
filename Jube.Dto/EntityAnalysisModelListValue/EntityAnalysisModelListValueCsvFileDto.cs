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
namespace Jube.Dto.EntityAnalysisModelListValue
{
    [Description("One CSV file supplied for upload into a List. The transport layer resolves the multipart " +
                 "form binding; the service consumes only the parsed stream and its metadata.")]
    public sealed class EntityAnalysisModelListValueCsvFileDto
    {
        [Description("The original file name, recorded against the upload for audit.")]
        public string? FileName { get; set; }

        [Description("The size of the file in bytes as reported by the transport, recorded against the upload.")]
        public long Length { get; set; }

        [Description("Readable stream over the CSV content. Each line is one List value: the first comma-separated " +
                     "column is the value, the optional second column an ISO 8601 round-trip (O) delete expiry date.")]
        public Stream Content { get; set; } = Stream.Null;
    }
}