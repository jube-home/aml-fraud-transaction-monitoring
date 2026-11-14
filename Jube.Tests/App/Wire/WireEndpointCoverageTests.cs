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
using FluentAssertions;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Jube.Data.Query;
using Jube.Service.EntityApproval.Models;
using Jube.App.Wire;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireEndpointCoverageTests(ITestOutputHelper output)
    {
        [Fact]
        public void EveryDeclaredApiResponseAndJsonRequestTypeCanTravelAsMessagePack()
        {
            var couldNotMap = new List<string>();
            var app = WireEndpointCatalogue.Map(couldNotMap);
            var registry = new WireTypeRegistry();
            registry.Attach([]);

            var endpoints = WireEndpointCatalogue.EndpointsOf(app);

            var unencodable = new SortedSet<string>(
                WireEndpointCatalogue.DeclaredPayloads(app)
                    .Where(payload => registry.ShapeOf(payload.Type) == null)
                    .Select(payload => $"{(payload.Request ? "request" : "response")} {payload.Type.Name} " +
                                       $"{(payload.Request ? "to" : "from")} {payload.Route}"),
                StringComparer.Ordinal);

            foreach (var line in couldNotMap)
            {
                output.WriteLine("could not map: " + line);
            }

            couldNotMap.Should().BeEmpty("a mapper that cannot run leaves a hole in this guard");
            endpoints.Count.Should().BeGreaterThan(150, "every endpoint mapper should have run");
            unencodable.Should().BeEmpty(
                "every API payload except the documented exceptions must be a MessagePackObject, so it never " +
                "falls back to JSON without anyone noticing");
        }

        private static readonly (string RoutePart, string Reason)[] UndeclaredByDesign =
        [
            ("VersionHistory", "rows are Dictionary<string, object> and VersionFieldChange lists, whose wire " +
                               "encodability VersionHistoryPayloadsCanTravelAsMessagePack proves"),
            ("/api/Invoke/", "the public ingestion API, which speaks JSON by design"),
            ("/Stream", "server-sent events"),
            ("/api/AntiforgeryToken", "bootstrap that runs before the browser has a schema"),
            ("/api/Authentication/", "the sign-in flow, which runs before the browser has a schema")
        ];

        [Fact]
        public void EveryEndpointThatDeclaresNoResponseTypeIsAKnownReviewedException()
        {
            var app = WireEndpointCatalogue.Map([]);
            var undeclared = WireEndpointCatalogue.EndpointsOf(app)
                .Where(endpoint => !endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
                    .Any(produces => produces.StatusCode is >= 200 and < 300))
                .Select(endpoint => $"{string.Join("/", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [])} " +
                                    endpoint.RoutePattern.RawText)
                .ToList();

            var unexplained = undeclared
                .Where(route => !UndeclaredByDesign.Any(exception =>
                    route.Contains(exception.RoutePart, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(route => route, StringComparer.Ordinal)
                .ToList();

            var stale = UndeclaredByDesign
                .Where(exception => !undeclared.Any(route =>
                    route.Contains(exception.RoutePart, StringComparison.OrdinalIgnoreCase)))
                .Select(exception => exception.RoutePart)
                .ToList();

            foreach (var exception in UndeclaredByDesign.Where(e => e.Reason.StartsWith("not yet reviewed",
                         StringComparison.Ordinal)))
            {
                output.WriteLine($"still to review: {exception.RoutePart}");
            }

            unexplained.Should().BeEmpty(
                "an endpoint that declares no response type can return a payload that falls back to JSON " +
                "unseen, so declare it with Produces<T> or add a reviewed exception here");
            stale.Should().BeEmpty("an exception that matches no endpoint is out of date");
        }

        [Fact]
        public void VersionHistoryPayloadsCanTravelAsMessagePack()
        {
            var registry = new WireTypeRegistry();
            registry.Attach([]);

            registry.ShapeOf(typeof(Dictionary<string, object>)).Should().NotBeNull();
            registry.ShapeOf(typeof(List<Dictionary<string, object>>)).Should().EndWith("{}#");
            registry.ShapeOf(typeof(IReadOnlyList<VersionFieldChange>)).Should().NotBeNull();
            registry.ShapeOf(typeof(EntityApprovalChanges)).Should().NotBeNull();
        }
    }
}
