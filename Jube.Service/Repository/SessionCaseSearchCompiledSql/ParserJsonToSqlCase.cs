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

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Jube.Data.Context;
using Jube.Data.Query;
using Jube.Data.Repository;

namespace Jube.Service.Repository.SessionCaseSearchCompiledSql;

internal sealed class ParserJsonToSqlCase
{
    public readonly List<QueryBuilderRule> Rules = [];
    public readonly List<object> Tokens = [];
    private IEnumerable<GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery.Dto> completionDto = [];
    public string? Sql;

    public static async Task<ParserJsonToSqlCase> CreateAsync(QueryBuilderRule? rule, DbContext dbContext,
        Guid caseWorkflowGuid, string userName, CancellationToken token = default)
    {
        var parser = new ParserJsonToSqlCase();
        parser.completionDto = await parser.GetCompletionsAsync(dbContext, caseWorkflowGuid, userName, token)
            .ConfigureAwait(false);
        parser.ExtractRule(rule);
        return parser;
    }

    private async Task<IEnumerable<GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery.Dto>>
        GetCompletionsAsync(DbContext dbContext,
            Guid caseWorkflowGuid, string userName, CancellationToken token = default)
    {
        var caseWorkflowRepository = new CaseWorkflowRepository(dbContext, userName);

        var entityAnalysisModelId =
            (await caseWorkflowRepository.GetByGuidIncludingDeletedAsync(caseWorkflowGuid, token))
            .EntityAnalysisModelId;

        var getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
            = new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext, userName);

        if (entityAnalysisModelId != null)
        {
            return await getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                .ExecuteAsync(entityAnalysisModelId.Value, 5, true, token).ConfigureAwait(false);
        }

        throw new Exception(
            $"Could not lookup model for Case Workflow Id {caseWorkflowGuid} and therefore could not lookup completions.");
    }

    private void ExtractRule(QueryBuilderRule? ruleChild)
    {
        ProcessChildrenRules(ruleChild);

        if (ValidateRuleNotNull(ruleChild))
        {
            return;
        }

        AddRule(ruleChild);
        AddToken(ruleChild);
        ConcatenateSql(ruleChild);
    }

    private void ProcessChildrenRules(QueryBuilderRule? ruleChild)
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
                Sql = Sql + " " + ValidCondition(ruleChild.Condition) + " ";
            }
        }

        Sql += ")";
    }

    private static string ValidCondition(string? condition)
    {
        return condition?.Trim().ToUpperInvariant() switch
        {
            "AND" => "and",
            "OR" => "or",
            _ => throw new InvalidOperationException($"Invalid SQL condition {condition}.")
        };
    }

    public string SelectField(string? id)
    {
        return id == "CaseWorkflowStatus" ? "\"CaseWorkflowStatus\".\"Name\"" : ReturnField(id);
    }

    private void ConcatenateSql(QueryBuilderRule? ruleChild)
    {
        if (ruleChild == null)
        {
            return;
        }

        var field = ReturnField(ruleChild.Id);

        if (ruleChild.Id == "CaseWorkflowStatusGuid" && ruleChild.Operator != "order")
        {
            Sql += ruleChild.Operator switch
            {
                "equal" => $"{field} = uuid(@{Tokens.Count})",
                "not_equal" => $"not {field} = uuid(@{Tokens.Count})",
                "order" => ruleChild.Operator,
                _ => throw new InvalidOperationException($"Invalid SQL operator {ruleChild.Operator}.")
            };

            return;
        }

        Sql += ruleChild.Operator switch
        {
            "equal" => $"{field} = (@{Tokens.Count})",
            "not_equal" => $"not {field} = (@{Tokens.Count})",
            "less" => $"{field} < (@{Tokens.Count})",
            "less_or_equal" => $"{field} <= (@{Tokens.Count})",
            "greater" => $"{field} >= (@{Tokens.Count})",
            "greater_or_equal" => $"{field} >= (@{Tokens.Count})",
            "like" => $"{field} like (@{Tokens.Count})",
            "not_like" => $"not {field} like (@{Tokens.Count})",
            "order" => ruleChild.Operator,
            _ => throw new InvalidOperationException($"Invalid SQL operator {ruleChild.Operator}.")
        };
    }

    private static bool ValidateRuleNotNull([NotNullWhen(false)] QueryBuilderRule? ruleChild)
    {
        if (ruleChild?.Rules != null)
        {
            return true;
        }

        return ruleChild is not { Value: not null, Operator: not null, Field: not null };
    }

    private void AddRule(QueryBuilderRule ruleChild)
    {
        Rules.Add(ruleChild);
    }

    private void AddToken(QueryBuilderRule ruleChild)
    {
        var value = ruleChild.Value ?? string.Empty;

        switch (ruleChild.Type)
        {
            case "integer":
                Tokens.Add(int.Parse(value));
                break;
            case "double":
                Tokens.Add(double.Parse(value));
                break;
            case "string":
                Tokens.Add(value);
                break;
            case "datetime":
                var date = DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out var dto)
                    ? dto.UtcDateTime
                    : DateTime.UtcNow;
                Tokens.Add(date);
                break;
            case "boolean":
                Tokens.Add(bool.Parse(value));
                break;
            default:
                Tokens.Add(value);
                break;
        }
    }

    private string ReturnField(string? id)
    {
        if (IsCaseField(id) != null)
        {
            return $"\"Case\".\"{id}\"";
        }

        if (IsCaseWorkflowStatusField(id) != null)
        {
            return $"\"CaseWorkflowStatus\".\"{id}\"";
        }

        var matched = completionDto.FirstOrDefault(f => f.Name == id);

        if (matched != null)
        {
            return matched.ValueSqlPath;
        }

        throw new InvalidOperationException($"Not found {id} in completions list.");
    }

    private static string? IsCaseWorkflowStatusField(string? name)
    {
        return name switch
        {
            "Priority" => name,
            "CaseWorkflowStatus" => name,
            _ => null
        };
    }

    private static string? IsCaseField(string? name)
    {
        return name switch
        {
            "Id" => name,
            "EntityAnalysisModelInstanceEntryGuid" => name,
            "DiaryDate" => name,
            "CaseWorkflowId" => name,
            "CaseWorkflowStatusGuid" => name,
            "CreatedDate" => name,
            "Locked" => name,
            "LockedUser" => name,
            "LockedDate" => name,
            "ClosedStatusId" => name,
            "ClosedDate" => name,
            "ClosedUser" => name,
            "CaseKey" => name,
            "Diary" => name,
            "DiaryUser" => name,
            "Rating" => name,
            "CaseKeyValue" => name,
            "ClosedStatusMigrationDate" => name,
            _ => null
        };
    }
}