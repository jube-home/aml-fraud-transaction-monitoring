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

using Jube.Analytics.Datasets.Models;

namespace Jube.Analytics.Datasets
{
    public sealed class DatasetParseReport
    {
        public bool Success { get; set; }
        public char Delimiter { get; set; } = ',';
        public long RowCount { get; set; }
        public long BytesRead { get; set; }
        public long BlankLineCount { get; set; }
        public List<DatasetColumnDefinition> Columns { get; set; } = [];
        public List<string> Warnings { get; set; } = [];
        public List<DatasetParseError> Errors { get; set; } = [];
    }
}