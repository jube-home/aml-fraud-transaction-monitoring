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
    [Description("The recorded outcome of one uploaded CSV file.")]
    public sealed class EntityAnalysisModelListValueCsvUploadResultDto
    {
        [Description("Identifier of the upload record created for the file.")]
        public int Id { get; set; }

        [Description("The original file name.")]
        public string? FileName { get; set; }

        [Description("Numeric identifier of the parent List the values were loaded into.")]
        public int EntityAnalysisModelListId { get; set; }

        [Description("Number of rows successfully loaded as List values.")]
        public int Records { get; set; }

        [Description("Number of rows that were rejected (blank, over-long or refused by the database) and skipped.")]
        public int Errors { get; set; }

        [Description("The size of the file in bytes.")]
        public long Length { get; set; }
    }
}