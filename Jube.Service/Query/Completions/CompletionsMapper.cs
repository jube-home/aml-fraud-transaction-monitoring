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
using Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery;
using Jube.Dto.Query.Completions;

namespace Jube.Service.Query.Completions
{
    internal static class CompletionsMapper
    {
        private const int Score = 1000;
        private const string DatasetGroup = "Dataset";

        public static List<CompletionDto> ToDtos(
            IEnumerable<GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQueryDto>
                source)
        {
            return source.Select(field => new CompletionDto
                {
                    Score = Score,
                    Name = field.Name,
                    Value = field.Value,
                    Field = field.ValueSqlPath,
                    Meta = $"{field.Name}:{field.JQueryBuilderDataType}",
                    Group = field.Group,
                    DataType = field.JQueryBuilderDataType,
                    XPath = field.ValueJsonPath
                })
                .ToList();
        }

        public static List<CompletionDto> ToDatasetDtos(IEnumerable<DatasetColumnDefinition> columns)
        {
            return columns.Select(column =>
                {
                    var name = $"{DatasetGroup}.{column.Header}";
                    var dataType = column.IsNumeric ? "double" : "string";

                    var sqlPath = $"(\"Json\"-> 'values' ->> '{column.StorageKey}')"
                                  + (column.IsNumeric ? "::double precision" : string.Empty);

                    return new CompletionDto
                    {
                        Score = Score,
                        Name = name,
                        Value = name,
                        Field = sqlPath,
                        Meta = $"{name}:{dataType}",
                        Group = DatasetGroup,
                        DataType = dataType,
                        XPath = $"dataset.{column.Header}"
                    };
                })
                .ToList();
        }
    }
}