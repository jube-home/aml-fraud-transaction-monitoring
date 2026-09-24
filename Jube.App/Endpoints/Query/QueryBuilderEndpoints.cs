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

using System.Collections.Generic;
using System.Linq;
using Jube.Data.QueryBuilder;
using Jube.Dto.Query.QueryBuilder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Jube.App.Endpoints.Query
{
    public static class QueryBuilderEndpoints
    {
        private static readonly List<BuilderOperatorDto> operators = BuilderOperators.All
            .Where(o => o.Template != null)
            .Select(o => new BuilderOperatorDto
            {
                Type = o.Type, Label = o.Label, Group = o.Group,
                Arguments = o.Arguments.Select(a => new BuilderOperatorArgumentDto
                    { Name = a.Name, Kind = a.Kind.ToString().ToLowerInvariant(), Many = a.Many }).ToList(),
                ApplyTo = o.RuleTextTypes.Select(DataType).ToList(),
                Template = o.Template
            }).ToList();

        public static void MapQueryBuilderEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGroup("/api/QueryBuilder")
                .RequireAuthorization()
                .WithTags("QueryBuilder")
                .MapGet("Operators", () => Results.Ok(operators))
                .Produces<List<BuilderOperatorDto>>()
                .WithName("QueryBuilderOperators");
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