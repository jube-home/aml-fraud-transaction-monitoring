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
namespace Jube.Dto.EntityAnalysisModelDictionaryKvp
{
    [Description("Outcome of importing one CSV file.")]
    public class EntityAnalysisModelDictionaryKvpCsvUploadResultDto
    {
        [Description("Identifier of the upload record created for the file.")]
        public int Id { get; set; }

        [Description("Original file name.")] public string? FileName { get; set; }

        [Description("Lines processed without error.")]
        public int Records { get; set; }

        [Description("Error count recorded against the upload (always zero, as in the legacy behaviour; failed " +
                     "lines are logged and excluded from Records).")]
        public int Errors { get; set; }
    }
}