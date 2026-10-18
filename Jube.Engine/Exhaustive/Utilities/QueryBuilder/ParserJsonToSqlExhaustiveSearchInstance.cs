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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Engine.Exhaustive.Utilities.QueryBuilder.Models;
using Newtonsoft.Json.Linq;
using Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery;

namespace Jube.Engine.Exhaustive.Utilities.QueryBuilder
{
    public sealed record CompletionSqlField(string Name, string ValueSqlPath);

    public class ParserJsonToSqlEntityAnalysisModel
    {
        private readonly IReadOnlyList<CompletionSqlField> fields;
        private readonly List<object> tokens = [];
        public string Sql;

        private ParserJsonToSqlEntityAnalysisModel(IReadOnlyList<CompletionSqlField> fields)
        {
            this.fields = fields;
        }

        public static async Task<ParserJsonToSqlEntityAnalysisModel> CreateAsync(Rule rule, DbContext dbContext,
            int tenantRegistryId, int entityAnalysisModelId, CancellationToken token = default)
        {
            var completions = await GetCompletionsAsync(dbContext, tenantRegistryId, entityAnalysisModelId, token)
                .ConfigureAwait(false);

            var mapped = completions
                .Select(c => new CompletionSqlField(c.Name, c.ValueSqlPath))
                .ToList();

            var parser = new ParserJsonToSqlEntityAnalysisModel(mapped);
            parser.ExtractRule(rule);

            return parser;
        }

        public static ParserJsonToSqlEntityAnalysisModel CreateForFields(Rule rule,
            IReadOnlyList<CompletionSqlField> fields)
        {
            ArgumentNullException.ThrowIfNull(fields);
            var parser = new ParserJsonToSqlEntityAnalysisModel(fields);
            parser.ExtractRule(rule);

            return parser;
        }

        private static Task<IEnumerable<GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQueryDto>>
            GetCompletionsAsync(DbContext dbContext, int tenantRegistryId, int entityAnalysisModelId,
                CancellationToken token = default)
        {
            var getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                = new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext, tenantRegistryId);

            return getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                .ExecuteAsync(entityAnalysisModelId, 5, true, token);
        }

        private void ExtractRule(Rule ruleChild)
        {
            ProcessChildrenRules(ruleChild);

            if (ValidateRuleNotNull(ruleChild))
            {
                return;
            }

            var startCount = tokens.Count;
            var added = AddToken(ruleChild);
            ConcatenateSql(ruleChild, startCount, added);
        }

        private void ProcessChildrenRules(Rule ruleChild)
        {
            if (ruleChild?.Rules == null)
            {
                return;
            }

            Sql += "(";

            for (var j = 0; j < ruleChild.Rules.Count; j++)
            {
                ExtractRule(ruleChild.Rules.ElementAt(j));

                if (j < ruleChild.Rules.Count - 1)
                {
                    Sql = Sql + " " + ruleChild.Condition + " ";
                }
            }

            Sql += ")";
        }

        private void ConcatenateSql(Rule ruleChild, int startCount, int added)
        {
            if (ruleChild == null)
            {
                return;
            }

            var field = ReturnField(ruleChild.Id);

            if (ruleChild.Operator is "in" or "not_in")
            {
                var placeholders = string.Join(", ",
                    Enumerable.Range(startCount + 1, added)
                        .Select(i => "@" + i.ToString(CultureInfo.InvariantCulture)));

                Sql += (ruleChild.Operator == "not_in" ? "not " : string.Empty) + $"{field} in ({placeholders})";

                return;
            }

            Sql += ruleChild.Operator switch
            {
                "equal" => $"{field} = (@{tokens.Count})",
                "not_equal" => $"not {field} = (@{tokens.Count})",
                "less" => $"{field} < (@{tokens.Count})",
                "less_or_equal" => $"{field} <= (@{tokens.Count})",
                "greater" => $"{field} > (@{tokens.Count})",
                "greater_or_equal" => $"{field} >= (@{tokens.Count})",
                "like" => $"{field} like (@{tokens.Count})",
                "not_like" => $"not {field} like (@{tokens.Count})",
                "order" => ruleChild.Operator,
                _ => throw new InvalidOperationException($"Invalid SQL operator {ruleChild.Operator}.")
            };
        }

        private static bool ValidateRuleNotNull(Rule ruleChild)
        {
            if (ruleChild?.Rules != null)
            {
                return true;
            }

            return ruleChild is not { Value: not null, Operator: not null, Field: not null };
        }

        private int AddToken(Rule ruleChild)
        {
            if (ruleChild.Operator is "in" or "not_in")
            {
                var raw = ruleChild.Value is JArray array
                    ? array.Select(v => v.ToString()).ToList()
                    : new List<string> { ruleChild.Value?.ToString() };

                if (raw.Count == 0)
                {
                    throw new InvalidOperationException("An in/not_in rule needs at least one value.");
                }

                foreach (var value in raw)
                {
                    tokens.Add(ConvertValue(ruleChild.Type, value));
                }

                return raw.Count;
            }

            tokens.Add(ConvertValue(ruleChild.Type, ruleChild.Value?.ToString()));

            return 1;
        }

        private static object ConvertValue(string type, string value)
        {
            return type switch
            {
                "integer" => int.Parse(value ?? "0"),
                "double" => double.Parse(value ?? "0"),
                "string" => value,
                "datetime" => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal,
                    out var dto)
                    ? dto.UtcDateTime
                    : DateTime.UtcNow,
                "boolean" => bool.Parse(value ?? "false"),
                _ => value
            };
        }

        private string ReturnField(string id)
        {
            var matched = fields.FirstOrDefault(f => f.Name == id);

            return matched != null
                ? matched.ValueSqlPath
                : throw new InvalidOperationException($"Not found {id} in completions list.");
        }
    }
}