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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Svirfd;
using Svirfd;
using Xunit;

namespace Jube.Test.Svirfd
{
    [Trait("Category", "Unit")]
    public sealed class SvirfdDirectoryHostTests
    {
        [Fact]
        public async Task LoadsTheFixtureSnapshotAndAnswersFromMemory()
        {
            await using var host = await SvirfdFixture.StartDirectoryHostAsync();

            host.Enabled.Should().BeTrue();
            host.Ready.Should().BeTrue();
            host.Mode.Should().Be(SvirfdMode.Directory);
            host.Seq.Should().BeGreaterThanOrEqualTo(10);
            host.Attribution.Should().NotBeNullOrWhiteSpace();

            host.TryLookup("37.30.56.54", out var mobile).Should().BeTrue();
            mobile!.Asn!.Asn.Should().Be("AS12912");
            mobile.Asn.Name.Should().Be("T-Mobile Polska S.A.");

            host.TryLookup("185.220.101.151", out var tor).Should().BeTrue();
            tor!.Privacy.Tor.Should().BeTrue();

            host.TryLookup("10.0.0.1", out var bogon).Should().BeTrue();
            bogon!.Bogon.Should().BeTrue();

            host.TryLookup("not-an-address", out var none).Should().BeFalse();
            none.Should().BeNull();

            (await host.LookupAsync("37.30.56.54", CancellationToken.None))!.Ip.Should().Be("37.30.56.54");
            (await host.LookupFreshAsync("::ffff:37.30.56.54", CancellationToken.None))!.Ip.Should().Be("37.30.56.54");
        }

        [Fact]
        public async Task EveryExpectedAddressResolves()
        {
            await using var host = await SvirfdFixture.StartDirectoryHostAsync();

            using var expected =
                JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(SvirfdFixture.Directory, "expected.json")));

            var addresses = expected.RootElement.EnumerateObject().Select(p => p.Name).ToList();
            addresses.Should().HaveCountGreaterThan(10);

            foreach (var address in addresses)
            {
                host.TryLookup(address, out var record).Should().BeTrue(address);

                var asn = expected.RootElement.GetProperty(address).GetProperty("asn");

                if (asn.ValueKind == JsonValueKind.Object)
                {
                    record!.Asn!.Asn.Should().Be(asn.GetProperty("asn").GetString(), address);
                }
            }
        }

        [Fact]
        public async Task DisabledSettingsNeverStartAClient()
        {
            await using var host = new SvirfdHost(new SvirfdSettings { Enabled = false },
                new System.Net.Http.HttpClient(), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

            await host.StartAsync(CancellationToken.None);

            host.Enabled.Should().BeFalse();
            host.Ready.Should().BeFalse();
            host.TryLookup("8.8.8.8", out _).Should().BeFalse();
            (await host.LookupAsync("8.8.8.8", CancellationToken.None)).Should().BeNull();
            host.Attribution.Should().Be(SvirfdCredit.Line);
        }
    }
}
