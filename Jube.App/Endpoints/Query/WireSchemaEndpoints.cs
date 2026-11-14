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
using System.Linq;
using Jube.App.Wire;
using Jube.Dto.Query.WireSchema;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Jube.App.Endpoints.Query
{
    public static class WireSchemaEndpoints
    {
        private const string Base = "/api/WireSchema";

        private const int MaximumShapes = 64;

        private const int MaximumShapeLength = 512;

        public static void MapWireSchemaEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("WireSchema");

            group.MapGet("", Get)
                .Produces<WireSchemaDto>()
                .WithName("WireSchemaGet");

            group.MapGet("Head", GetHead)
                .Produces<WireSchemaDto>()
                .WithName("WireSchemaHead");

            group.MapGet("Shapes", GetShapes)
                .Produces<WireSchemaDto>()
                .WithName("WireSchemaShapes");
        }

        private static IResult Get(HttpContext httpContext, WireTypeRegistry registry)
        {
            var schema = registry.GetSchema();

            var entityTag = $"\"{schema.Hash}\"";

            httpContext.Response.Headers.CacheControl =
                httpContext.Request.Query["hash"] == schema.Hash
                    ? "private, max-age=31536000, immutable"
                    : "private, no-cache";
            httpContext.Response.Headers.ETag = entityTag;

            return httpContext.Request.Headers.IfNoneMatch == entityTag
                ? TypedResults.StatusCode(StatusCodes.Status304NotModified)
                : TypedResults.Ok(schema);
        }

        private static IResult GetHead(HttpContext httpContext, WireTypeRegistry registry)
        {
            var head = registry.GetHead();

            var entityTag = $"\"{head.Hash}-head\"";

            httpContext.Response.Headers.CacheControl =
                httpContext.Request.Query["hash"] == head.Hash
                    ? "private, max-age=31536000, immutable"
                    : "private, no-cache";
            httpContext.Response.Headers.ETag = entityTag;

            return httpContext.Request.Headers.IfNoneMatch == entityTag
                ? TypedResults.StatusCode(StatusCodes.Status304NotModified)
                : TypedResults.Ok(head);
        }

        private static IResult GetShapes(HttpContext httpContext, WireTypeRegistry registry)
        {
            var requested = httpContext.Request.Query["shape"]
                .Where(shape => !string.IsNullOrEmpty(shape))
                .Select(shape => shape!)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (requested.Count == 0 || requested.Count > MaximumShapes ||
                requested.Any(shape => shape.Length > MaximumShapeLength))
            {
                return TypedResults.BadRequest();
            }

            var shapes = registry.GetShapes(requested);

            httpContext.Response.Headers.CacheControl =
                httpContext.Request.Query["hash"] == shapes.Hash
                    ? "private, max-age=31536000, immutable"
                    : "private, no-cache";

            return TypedResults.Ok(shapes);
        }
    }
}
