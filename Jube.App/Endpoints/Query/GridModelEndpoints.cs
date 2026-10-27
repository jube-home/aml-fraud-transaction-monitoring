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
using Jube.Dto.Query.GridModel;
using Jube.Resources;
using Jube.Service.Query.GridModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class GridModelEndpoints
    {
        private const string Base = "/api/GridModel";

        public static void MapGridModelEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("GridModel");

            group.MapGet("", GetAreas)
                .Produces<List<string>>()
                .WithName("GridModelGetAreas");

            group.MapGet("{area}", GetByArea)
                .Produces<GridModelDto>()
                .Produces(StatusCodes.Status404NotFound)
                .WithName("GridModelGetByArea");
        }

        private static IResult GetAreas(GridModelRegistry registry)
        {
            return TypedResults.Ok(registry.Areas.Order(StringComparer.OrdinalIgnoreCase).ToList());
        }

        private static IResult GetByArea(string area, GridModelRegistry registry, IStringLocalizerFactory localizers)
        {
            var strings = localizers.Create(typeof(FormModelResources));

            return registry.TryGetLocalised(area, CultureInfo.CurrentUICulture.Name, strings, out var model) &&
                   model != null
                ? TypedResults.Ok(model)
                : TypedResults.NotFound();
        }
    }
}
