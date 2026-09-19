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

using Jube.Dto.Query.Completions;

namespace Jube.Service.Query.Completions
{
    internal static class CompletionsMapper
    {
        private const int Score = 1000;

        public static List<CompletionDto> ToDtos(
            IEnumerable<global::Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery.Dto>
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
    }
}