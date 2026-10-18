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

using System.Text.Json;
using Jube.Analytics.Datasets.Models;
using Jube.Data.Poco;
using Jube.Data.Repository.Interfaces;
using Jube.Data.Repository.Models;
using Jube.Dto.ExhaustiveInstanceDataset;

namespace Jube.Service.Repository.ExhaustiveInstanceDataset
{
    internal static class ExhaustiveInstanceDatasetMapper
    {
        public static List<DatasetColumnDefinition> ReadColumns(string? columnsJson)
        {
            return string.IsNullOrWhiteSpace(columnsJson)
                ? []
                : JsonSerializer.Deserialize<List<DatasetColumnDefinition>>(columnsJson) ?? [];
        }

        public static List<string> ReadWarnings(string? reportJson)
        {
            if (string.IsNullOrWhiteSpace(reportJson))
            {
                return [];
            }

            return JsonSerializer.Deserialize<DatasetStoredReport>(reportJson)?.Warnings ?? [];
        }

        public static ExhaustiveInstanceDatasetDto ToDto(IDatasetStore poco, bool includeColumns)
        {
            var columns = ReadColumns(poco.ColumnsJson);

            return new ExhaustiveInstanceDatasetDto
            {
                Id = poco.Id,
                Guid = poco.Guid,
                Name = poco.Name,
                Kind = DatasetStores.KindOf(poco).ToString(),
                FileName = (poco as ExhaustiveInstanceCsv)?.FileName,
                SourceLabel = (poco as ExhaustiveInstanceJson)?.SourceLabel,
                ContentHash = (poco as ExhaustiveInstanceJson)?.ContentHash,
                RowCount = poco.RowCount,
                SizeBytes = poco.SizeBytes,
                ColumnCount = columns.Count,
                StatusId = poco.StatusId,
                Delimiter = poco is ExhaustiveInstanceCsv csv
                    ? csv.DelimiterId switch
                    {
                        ExhaustiveCsvDelimiter.Semicolon => ';',
                        _ => ','
                    }
                    : null,
                Warnings = includeColumns ? ReadWarnings(poco.ReportJson) : [],
                Columns = includeColumns ? columns.Select(ToDto).ToList() : [],
                CreatedUser = poco.CreatedUser,
                CreatedDate = poco.CreatedDate.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(poco.CreatedDate.Value, DateTimeKind.Utc))
                    : null,
                Version = poco.Version.GetValueOrDefault()
            };
        }

        public static ExhaustiveInstanceDatasetColumnDto ToDto(DatasetColumnDefinition column)
        {
            return new ExhaustiveInstanceDatasetColumnDto
            {
                Index = column.Index,
                Header = column.Header,
                Kind = column.Kind,
                NumericCount = column.NumericCount,
                TextCount = column.TextCount,
                MissingCount = column.MissingCount,
                Min = column.Min,
                Max = column.Max,
                Mean = column.Mean,
                ZeroCount = column.ZeroCount,
                OneCount = column.OneCount,
                IsBinary = column.IsBinary,
                IsConstant = column.IsConstant,
                FormulaLikeCount = column.FormulaLikeCount,
                Include = column.Include,
                Name = column.Name,
                ProcessingTypeId = column.ProcessingTypeId,
                IsCategorical = column.IsCategorical,
                DistinctValues = column.DistinctValues
                    .Select(s => new DatasetDistinctValueDto { Value = s.Value, Count = s.Count })
                    .ToList()
            };
        }

        public sealed class DatasetStoredReport
        {
            public List<string> Warnings { get; set; } = [];
            public long BlankLineCount { get; set; }
        }
    }
}