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
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jube.App.Wire
{
    public static class WireEndpointCatalogue
    {
        private static readonly Lazy<IReadOnlyList<Type>> Declared = new(() =>
        {
            try
            {
                return PayloadTypesOf(Map([]));
            }
            catch (Exception)
            {
                return [];
            }
        });

        public static IReadOnlyList<Type> PayloadTypes => Declared.Value;

        public static bool IsNotAPayload(Type type)
        {
            return type == typeof(string) || type == typeof(byte[]) || type == typeof(Stream) || type == typeof(object)
                   || type == typeof(ProblemDetails) || type == typeof(ValidationProblemDetails)
                   || typeof(IResult).IsAssignableFrom(type) || typeof(IActionResult).IsAssignableFrom(type) || type == typeof(IFormFile)
                   || type == typeof(IFormFileCollection) || type == typeof(HttpRequest)
                   || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        public static WebApplication Map(List<string> couldNotMap)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.Host.UseServiceProviderFactory(new WireCatalogueProviderFactory());
            var app = builder.Build();

            var mappers = typeof(WireEndpointCatalogue).Assembly.GetTypes()
                .Where(type => type is { IsAbstract: true, IsSealed: true } && type.Namespace != null
                               && type.Namespace.StartsWith("Jube.App.Endpoints", StringComparison.Ordinal))
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(method => method.Name.StartsWith("Map", StringComparison.Ordinal)
                                 && method.Name.EndsWith("Endpoints", StringComparison.Ordinal)
                                 && method.GetParameters().Length == 1
                                 && method.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(WebApplication)))
                .OrderBy(method => method.DeclaringType!.FullName, StringComparer.Ordinal)
                .ThenBy(method => method.Name, StringComparer.Ordinal)
                .ToList();

            foreach (var mapper in mappers)
            {
                try
                {
                    mapper.Invoke(null, [app]);
                }
                catch (Exception exception)
                {
                    couldNotMap.Add($"{mapper.DeclaringType!.Name}.{mapper.Name}: {exception.GetBaseException().Message}");
                }
            }

            return app;
        }

        public static IReadOnlyList<RouteEndpoint> EndpointsOf(WebApplication app)
        {
            return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>().ToList();
        }

        public static IReadOnlyList<(string Route, Type Type, bool Request)> DeclaredPayloads(WebApplication app)
        {
            var found = new List<(string, Type, bool)>();

            foreach (var endpoint in EndpointsOf(app))
            {
                var route = $"{string.Join("/", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [])} " +
                            endpoint.RoutePattern.RawText;

                foreach (var produces in endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>())
                {
                    if (produces.StatusCode is >= 200 and < 300 && produces.Type != null
                        && produces.Type != typeof(void) && !IsNotAPayload(produces.Type))
                    {
                        found.Add((route, produces.Type, false));
                    }
                }

                var accepts = endpoint.Metadata.GetMetadata<IAcceptsMetadata>();

                if (accepts?.RequestType != null
                    && accepts.ContentTypes.Any(c => c.Contains("json", StringComparison.OrdinalIgnoreCase))
                    && !IsNotAPayload(accepts.RequestType))
                {
                    found.Add((route, accepts.RequestType, true));
                }
            }

            return found;
        }

        public static IReadOnlyList<Type> PayloadTypesOf(WebApplication app)
        {
            return DeclaredPayloads(app).Select(payload => payload.Type).Distinct()
                .OrderBy(type => type.FullName, StringComparer.Ordinal).ToList();
        }
    }
}
