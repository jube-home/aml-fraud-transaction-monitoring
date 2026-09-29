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

using System.Net;
using FluentAssertions;
using Jube.Engine.BackgroundTasks.TaskStarters.Metrics;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Engine.Metrics
{
    public class EtcdApiResponseTests
    {
        private const string UserNameEmptyBody =
            """{"error":"etcdserver: user name is empty","code":3,"message":"etcdserver: user name is empty"}""";

        private const string InvalidAuthTokenBody =
            """{"error":"etcdserver: invalid auth token","code":16,"message":"etcdserver: invalid auth token"}""";

        private const string MaintenanceStatusBody =
            """
            {"header":{"cluster_id":"16645438370749438781","member_id":"11188960661303352088","revision":"1",
            "raft_term":"2"},"version":"3.5.34","dbSize":"20480","leader":"11188960661303352088","raftIndex":"16",
            "raftTerm":"2","raftAppliedIndex":"16"}
            """;

        [Fact]
        public void AMissingCredentialIsRejectedRatherThanReadAsAPayload()
        {
            var read = EtcdApiResponse.TryRead(HttpStatusCode.BadRequest, UserNameEmptyBody, out var payload,
                out var error);

            read.Should().BeFalse();
            payload.Should().BeNull();
            error.Should().Be("HTTP 400 BadRequest: etcdserver: user name is empty (etcd code 3)");
        }

        [Fact]
        public void AnInvalidTokenIsRejectedRatherThanReadAsAPayload()
        {
            var read = EtcdApiResponse.TryRead(HttpStatusCode.Unauthorized, InvalidAuthTokenBody, out var payload,
                out var error);

            read.Should().BeFalse();
            payload.Should().BeNull();
            error.Should().Be("HTTP 401 Unauthorized: etcdserver: invalid auth token (etcd code 16)");
        }

        [Fact]
        public void AnAuthenticatedMaintenanceStatusIsRead()
        {
            var read = EtcdApiResponse.TryRead(HttpStatusCode.OK, MaintenanceStatusBody, out var payload,
                out var error);

            read.Should().BeTrue();
            error.Should().BeNull();
            var status = payload.Required();
            status["header"].Required().Value<string>("member_id").Should().Be("11188960661303352088");
            status.Value<string>("raftIndex").Should().Be("16");
            status.Value<string>("dbSize").Should().Be("20480");
        }

        [Fact]
        public void AnErrorEnvelopeReturnedWithASuccessStatusIsStillRejected()
        {
            var read = EtcdApiResponse.TryRead(HttpStatusCode.OK, UserNameEmptyBody, out var payload, out var error);

            read.Should().BeFalse();
            payload.Should().BeNull();
            error.Should().Contain("etcdserver: user name is empty");
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, "")]
        [InlineData(HttpStatusCode.OK, "   ")]
        [InlineData(HttpStatusCode.OK, null)]
        [InlineData(HttpStatusCode.InternalServerError, "")]
        public void AnEmptyBodyIsRejected(HttpStatusCode statusCode, string? body)
        {
            var read = EtcdApiResponse.TryRead(statusCode, body, out var payload, out var error);

            read.Should().BeFalse();
            payload.Should().BeNull();
            error.Should().Contain("empty response body");
        }

        [Theory]
        [InlineData("<html><body>502 Bad Gateway</body></html>")]
        [InlineData("not json at all")]
        [InlineData("[1,2,3]")]
        public void ABodyThatIsNotAJsonObjectIsRejected(string body)
        {
            var read = EtcdApiResponse.TryRead(HttpStatusCode.BadGateway, body, out var payload, out var error);

            read.Should().BeFalse();
            payload.Should().BeNull();
            error.Should().Contain("not a JSON object");
        }

        [Fact]
        public void AServerErrorWithNoEtcdEnvelopeStillReportsItsStatus()
        {
            var read = EtcdApiResponse.TryRead(HttpStatusCode.ServiceUnavailable, """{"header":{}}""",
                out var payload, out var error);

            read.Should().BeFalse();
            payload.Should().BeNull();
            error.Should().Be("HTTP 503 ServiceUnavailable");
        }
    }
}