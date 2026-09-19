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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Test.Infrastructure;
using Jube.App.Endpoints.Mocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Jube.Test.Mocks
{
    public class MockEndpointsGoldenTests : IAsyncLifetime
    {
        private MocksHost Host
        {
            get => field.Required();
            set;
        }

        public async Task InitializeAsync()
        {
            Host = await MocksHost.StartAsync(_ => { }, app =>
            {
                app.MapMockHttpAdaptationEndpoints();
                app.MapMockRsaMfaEndpoints();
            });
        }

        public async Task DisposeAsync()
        {
            await Host.DisposeAsync();
        }

        public static IEnumerable<object[]> HttpAdaptationCases =>
            MocksCases.HttpAdaptation.Select(r => new object[] { r.Name });

        public static IEnumerable<object[]> MfaCases => MocksCases.Mfa.Select(r => new object[] { r.Name });

        [Theory]
        [MemberData(nameof(HttpAdaptationCases))]
        [MemberData(nameof(MfaCases))]
        public async Task ResponseIsByteIdenticalToTheLegacyControllerAsync(string name)
        {
            var request = MocksCases.All.Single(r => r.Name == name);
            var golden = MocksGoldens.All[name];

            var actual = await Host.SendAsync(request);

            actual.Status.Should().Be(golden.Status);
            actual.ContentType.Should().Be(golden.ContentType);
            actual.Body.Replace("\r\n", "\n").Should().Be(golden.Body);
            actual.Headers.Should().Be(golden.Headers);
        }

        [Fact]
        public void EveryCaseHasAGoldenAndNothingElse()
        {
            MocksGoldens.All.Keys.Should().BeEquivalentTo(MocksCases.All.Select(r => r.Name));
        }

        [Fact]
        public void HttpAdaptationRoutesAndVerbsAreExactlyTheLegacyOnes()
        {
            var expected = MocksCases.HttpAdaptation.Where(r =>
                    r.Path.Count(c => c == '/') == 3 && !r.Path.EndsWith('/') &&
                    r.Method == "POST" && r.Name == r.Path.Split('/').Last())
                .Select(r => r.Path).Concat(["/api/MockHttpAdaptation/"]);

            Endpoints("/api/MockHttpAdaptation").Select(e => e.RoutePattern.RawText).Should().BeEquivalentTo(expected);
            Endpoints("/api/MockHttpAdaptation").Count(e => Verb(e) == "GET").Should().Be(1);
            Endpoints("/api/MockHttpAdaptation").Count(e => Verb(e) == "POST").Should().Be(21);
        }

        [Fact]
        public void MfaIsASinglePostRoute()
        {
            var endpoints = Endpoints("/api/Mfa");
            endpoints.Should().ContainSingle();
            Verb(endpoints.Single()).Should().Be("POST");
        }

        [Fact]
        public void EveryMockEndpointIsExplicitlyAnonymous()
        {
            foreach (var endpoint in Endpoints("/api/M"))
            {
                endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>().Should()
                    .NotBeNull(endpoint.RoutePattern.RawText);
                endpoint.Metadata.GetOrderedMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Should()
                    .BeEmpty(endpoint.RoutePattern.RawText);
            }
        }

        private static string Verb(RouteEndpoint endpoint)
        {
            return endpoint.Metadata.GetMetadata<HttpMethodMetadata>().Required().HttpMethods.Single();
        }

        private static List<RouteEndpoint> Endpoints(string prefix)
        {
            var builder = WebApplication.CreateBuilder();
            using var app = builder.Build();
            app.MapMockHttpAdaptationEndpoints();
            app.MapMockRsaMfaEndpoints();
            return
            [
                .. ((IEndpointRouteBuilder)app).DataSources.SelectMany(d => d.Endpoints).OfType<RouteEndpoint>()
                .Where(e => e.RoutePattern.RawText.Required().StartsWith(prefix, StringComparison.Ordinal))
            ];
        }
    }
}