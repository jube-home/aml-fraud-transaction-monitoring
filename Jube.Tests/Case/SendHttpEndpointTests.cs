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

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Case;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Case;

[Trait("Category", "Unit")]
public sealed class SendHttpEndpointTests
{
    private const string SensitivePayload = "{\"accountNumber\":\"4111111111111111\",\"narrative\":\"secret\"}";
    private const string UnreachableEndpoint = "http://127.0.0.1:1/never";

    [Fact]
    public async Task PostAsync_OnFailure_NeverLogsThePayloadAsync()
    {
        var log = new TestLog();

        await SendHttpEndpoint.PostAsync(UnreachableEndpoint, SensitivePayload, log);

        var error = log.Entries.Single(e => e.Level == "ERROR");
        error.Message.Should().NotContain(SensitivePayload);
        error.Message.Should().NotContain("secret");
        error.Message.Should().Contain("127.0.0.1");
        error.Message.Should().Contain("correlationId=");
    }

    [Fact]
    public async Task GetAsync_OnFailure_NeverLogsTheFullUrlAsync()
    {
        var log = new TestLog();
        const string endpointWithSecretQueryString = UnreachableEndpoint + "?token=super-secret-token";

        await SendHttpEndpoint.GetAsync(endpointWithSecretQueryString, log);

        var error = log.Entries.Single(e => e.Level == "ERROR");
        error.Message.Should().NotContain("super-secret-token");
        error.Message.Should().Contain("127.0.0.1");
        error.Message.Should().Contain("correlationId=");
    }
}