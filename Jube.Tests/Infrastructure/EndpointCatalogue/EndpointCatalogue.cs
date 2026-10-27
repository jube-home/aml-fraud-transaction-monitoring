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
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Jube.Test.Infrastructure.EndpointCatalogue
{
    public sealed class EndpointCatalogue
    {
        private static readonly Lazy<EndpointCatalogue> Lazy = new(Build);

        private readonly List<RouteEntry> routes;

        private EndpointCatalogue(List<RouteEntry> routes, List<string> failures)
        {
            this.routes = routes;
            Failures = failures;
        }

        public static EndpointCatalogue Instance => Lazy.Value;

        public IReadOnlyList<string> Failures { get; }

        public IReadOnlyList<RouteEntry> Routes => routes;

        public bool Has(string method, string route)
        {
            return routes.Any(entry => entry.Method == method &&
                                       string.Equals(entry.Pattern, route, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasUnder(string method, string route)
        {
            return routes.Any(entry => entry.Method == method &&
                                       (string.Equals(entry.Pattern, route, StringComparison.OrdinalIgnoreCase) ||
                                        entry.Pattern.StartsWith(route + "/", StringComparison.OrdinalIgnoreCase)));
        }

        private static EndpointCatalogue Build()
        {
            var builder = WebApplication.CreateBuilder();

            builder.Host.UseServiceProviderFactory(new EveryClassIsAServiceProviderFactory());

            IEndpointRouteBuilder application = builder.Build();
            var failures = new List<string>();

            foreach (var method in typeof(Jube.App.Startup).Assembly.GetTypes()
                         .Where(type => type is { IsAbstract: true, IsSealed: true })
                         .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                         .Where(method => method.Name.StartsWith("Map", StringComparison.Ordinal) &&
                                          method.Name.EndsWith("Endpoints", StringComparison.Ordinal))
                         .Where(method =>
                         {
                             var parameters = method.GetParameters();

                             return parameters.Length == 1 && parameters[0].ParameterType == typeof(IEndpointRouteBuilder);
                         }))
            {
                try
                {
                    method.Invoke(null, [application]);
                }
                catch (Exception exception)
                {
                    failures.Add($"{method.DeclaringType?.Name}.{method.Name}: {exception.InnerException?.Message ?? exception.Message}");
                }
            }

            var entries = application.DataSources
                .SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>()
                .SelectMany(endpoint =>
                {
                    var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [];

                    var parameters = endpoint.Metadata.GetMetadata<MethodInfo>()?.GetParameters()
                        .Select(parameter => parameter.Name ?? string.Empty).ToList() ?? [];

                    return methods.Select(method => new RouteEntry(method, Normalise(endpoint.RoutePattern), parameters));
                })
                .Distinct()
                .ToList();

            return new EndpointCatalogue(entries, failures);
        }

        private static string Normalise(RoutePattern pattern)
        {
            var text = pattern.RawText ?? string.Empty;

            var rooted = text.StartsWith('/') ? text : "/" + text;

            return rooted.Length > 1 ? rooted.TrimEnd('/') : rooted;
        }
    }
}
