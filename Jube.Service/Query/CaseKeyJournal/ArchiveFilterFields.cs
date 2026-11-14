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

using Jube.Data.Context;
using Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class ArchiveFilterFields
    {
        private const int ParserTypeId = 5;

        private static readonly HashSet<string> types = ["string", "integer", "double", "datetime", "boolean"];

        public static async Task<IReadOnlyDictionary<string, ArchiveFilterField>> LoadAsync(DbContext dbContext,
            string userName, int modelId, CancellationToken token)
        {
            var completions = await new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext,
                userName).ExecuteAsync(modelId, ParserTypeId, true, token).ConfigureAwait(false);

            return From(completions);
        }

        public static IReadOnlyDictionary<string, ArchiveFilterField> From(
            IEnumerable<GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQueryDto> completions)
        {
            var fields = new Dictionary<string, ArchiveFilterField>(StringComparer.Ordinal);

            foreach (var completion in completions)
            {
                if (string.IsNullOrEmpty(completion.Name) || string.IsNullOrEmpty(completion.ValueSqlPath)
                                                          || !types.Contains(completion.JQueryBuilderDataType ?? string.Empty))
                {
                    continue;
                }

                fields.TryAdd(completion.Name,
                    new ArchiveFilterField(completion.Name, completion.ValueSqlPath, completion.JQueryBuilderDataType!));
            }

            return fields;
        }
    }
}
