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
using System.Text.Json.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.EntityAnalysisModelDictionaryKvp
{
    [Description("One CSV file to import into a Dictionary. Each line is 'key,value[,deleteExpiryDate]' where " +
                 "deleteExpiryDate is an ISO 8601 round-trip ('O') UTC date time.")]
    public class EntityAnalysisModelDictionaryKvpCsvUploadFileDto
    {
        [Description("Original file name, recorded against the upload.")]
        public string? FileName { get; set; }

        [Description("Length of the file in bytes, recorded against the upload.")]
        public long Length { get; set; }

        [Description("Readable stream of the CSV content. Not serialised; supplied by the caller.")]
        [JsonIgnore]
        public Stream? Content { get; set; }
    }
}