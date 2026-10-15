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
using Jube.Dto.Query.QueryBuilder;
using Jube.Service.Query.QueryBuilder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Jube.App.Endpoints.Query
{
    public static class QueryBuilderEndpoints
    {
        public static void MapQueryBuilderEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGroup("/api/QueryBuilder")
                .RequireAuthorization()
                .WithTags("QueryBuilder")
                .MapGet("Operators", () => Results.Ok(QueryBuilderService.All))
                .Produces<List<BuilderOperatorDto>>()
                .WithName("QueryBuilderOperators");
        }
    }
}