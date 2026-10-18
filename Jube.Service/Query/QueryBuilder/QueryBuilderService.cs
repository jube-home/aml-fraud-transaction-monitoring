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
using Jube.Data.QueryBuilder;
using Jube.Data.QueryBuilder.Models;
using Jube.Dto.Query.QueryBuilder;
using Jube.Service.Agent.Models;

namespace Jube.Service.Query.QueryBuilder
{
    public sealed class QueryBuilderService
    {
        private static readonly List<BuilderOperatorDto> catalogue = BuilderOperators.All
            .Where(builderOperator => builderOperator.Template != null)
            .Select(builderOperator => new BuilderOperatorDto
            {
                Type = builderOperator.Type,
                Label = builderOperator.Label,
                Group = builderOperator.Group,
                Arguments = builderOperator.Arguments.Select(argument => new BuilderOperatorArgumentDto
                {
                    Name = argument.Name,
                    Kind = argument.Kind.ToString().ToLowerInvariant(),
                    Many = argument.Many
                }).ToList(),
                ApplyTo = builderOperator.RuleTextTypes.Select(DataType).ToList(),
                Template = builderOperator.Template
            }).ToList();

        private static readonly int[] requiredPermissions = [];
        public static IReadOnlyList<int> RequiredPermissions => requiredPermissions;
        public static IReadOnlyList<BuilderOperatorDto> All => catalogue;

        public static Task<QueryBuilderService> CreateAsync()
        {
            return Task.FromResult(new QueryBuilderService());
        }

        [Description("Lists every operator the rule builder offers, with the rule text each one produces, the " +
                     "values it takes and the field data types it applies to. This is the operator set this " +
                     "build actually has, so an operator absent from this list cannot be used in a rule however " +
                     "it may be described elsewhere. Read it before writing or validating a rule fragment.")]
        [ServiceOperation("QueryBuilderOperatorsList", OperationKind.Read, Idempotent = true)]
        public Task<List<BuilderOperatorDto>> ListAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            return Task.FromResult(catalogue);
        }

        private static string DataType(BuilderFieldType type)
        {
            return type switch
            {
                BuilderFieldType.String => "string",
                BuilderFieldType.Integer => "integer",
                BuilderFieldType.Double => "double",
                BuilderFieldType.Boolean => "boolean",
                BuilderFieldType.DateTime => "datetime",
                _ => "list"
            };
        }
    }
}