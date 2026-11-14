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
using Jube.Svirfd;
using Svirfd;
using Xunit;

namespace Jube.Test.Svirfd
{
    [Trait("Category", "Unit")]
    public sealed class SvirfdSupportTests
    {
        [Fact]
        public void SettingsReadEveryKeyWithDefaults()
        {
            var values = new Dictionary<string, string?>
            {
                ["EnableSvirfd"] = "False",
                ["SvirfdApiKey"] = "key",
                ["SvirfdMode"] = "local",
                ["SvirfdBaseUrl"] = "https://example.test/",
                ["SvirfdDirectory"] = "/data",
                ["SvirfdPollInterval"] = "1000",
                ["SvirfdSite"] = "node-a",
                ["EnableSvirfdRemoteEngineLookup"] = "False",
                ["SvirfdRemoteLookupTimeout"] = "250",
                ["SvirfdRemoteCacheMinutes"] = "5",
                ["SvirfdRemoteCacheMaximumEntries"] = "10",
                ["SvirfdWaitForReady"] = "True",
                ["SvirfdStartupWait"] = "3000",
                ["SvirfdCacheDirectory"] = "/var/cache/svirfd",
                ["SvirfdLocalRetryPause"] = "5000",
                ["SvirfdStaleAfter"] = "60000"
            };

            var settings = SvirfdSettings.From(key => values.GetValueOrDefault(key));

            settings.Enabled.Should().BeFalse();
            settings.ApiKey.Should().Be("key");
            settings.Mode.Should().Be(SvirfdMode.Local);
            settings.BaseUrl.Should().Be("https://example.test/");
            settings.Directory.Should().Be("/data");
            settings.PollInterval.Should().Be(TimeSpan.FromSeconds(1));
            settings.Site.Should().Be("node-a");
            settings.RemoteEngineLookup.Should().BeFalse();
            settings.RemoteLookupTimeout.Should().Be(TimeSpan.FromMilliseconds(250));
            settings.RemoteCacheLifetime.Should().Be(TimeSpan.FromMinutes(5));
            settings.RemoteCacheMaximumEntries.Should().Be(10);
            settings.WaitForReady.Should().BeTrue();
            settings.StartupWait.Should().Be(TimeSpan.FromSeconds(3));
            settings.CacheDirectory.Should().Be("/var/cache/svirfd");
            settings.LocalRetryPause.Should().Be(TimeSpan.FromSeconds(5));
            settings.StaleAfter.Should().Be(TimeSpan.FromMinutes(1));
            settings.ToOptions().CacheDirectory.Should().Be("/var/cache/svirfd");
            settings.ToOptions().LocalRetryPause.Should().Be(TimeSpan.FromSeconds(5));
            settings.ToOptions().StaleAfter.Should().Be(TimeSpan.FromMinutes(1));

            var defaults = SvirfdSettings.From(_ => null);

            defaults.Enabled.Should().BeTrue();
            defaults.Mode.Should().Be(SvirfdMode.Auto);
            defaults.RemoteEngineLookup.Should().BeTrue();
            defaults.BaseUrl.Should().Be("https://svirfd.com");
            defaults.Site.Should().Be("jube");
            defaults.ToOptions().PollInterval.Should().Be(TimeSpan.FromHours(1));
            defaults.ToOptions().Application.Should().Be("Jube");
            defaults.ToOptions().BackgroundStart.Should().BeTrue();
            defaults.ToOptions().LocalRetryPause.Should().Be(TimeSpan.FromSeconds(30));
            defaults.ToOptions().StaleAfter.Should().Be(TimeSpan.FromHours(3));
        }

        [Fact]
        public void CacheExpiresAndEvictsWhenFull()
        {
            var cache = new SvirfdRemoteCache(TimeSpan.FromMinutes(1), 4);
            var now = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
            var record = SvirfdJson.Deserialize(
                "{\"ip\":\"1.1.1.1\",\"location\":{\"cityConfidence\":\"none\",\"kind\":\"registryCountry\"},\"connection\":{},\"privacy\":{},\"lists\":[],\"asOf\":{}}")!;

            for (var i = 0; i < 4; i++)
            {
                cache.Set($"1.1.1.{i}", record, now.AddSeconds(i));
            }

            cache.Count.Should().Be(4);
            cache.TryGet("1.1.1.0", now.AddSeconds(30), out _).Should().BeTrue();
            cache.TryGet("1.1.1.0", now.AddMinutes(2), out _).Should().BeFalse();

            cache.Set("1.1.1.9", record, now.AddSeconds(10));
            cache.Count.Should().BeLessThanOrEqualTo(4);
        }

        [Fact]
        public void CircuitBacksOffOnRepeatedRateLimits()
        {
            var circuit = new SvirfdRemoteCircuit();
            var now = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

            circuit.OpenForRateLimit(now).Should().Be(TimeSpan.FromSeconds(1));
            circuit.IsOpen(now.AddMilliseconds(500)).Should().BeTrue();
            circuit.IsOpen(now.AddSeconds(2)).Should().BeFalse();
            circuit.OpenForRateLimit(now.AddSeconds(2)).Should().Be(TimeSpan.FromSeconds(2));
            circuit.OpenForRateLimit(now.AddSeconds(5)).Should().Be(TimeSpan.FromSeconds(4));
            circuit.OpenForRateLimit(now.AddMinutes(10)).Should().Be(TimeSpan.FromSeconds(1));

            circuit.OpenIndefinitely();
            circuit.IsOpen(DateTime.MaxValue.AddDays(-1)).Should().BeTrue();
        }

        [Fact]
        public async Task NarrativeAdapterKeepsGroupOrderAndTitles()
        {
            var record = await SvirfdFixture.RecordAsync("185.220.101.151");

            var narrative = SvirfdNarrative.Render(record);

            narrative.Should().NotBeNull();
            narrative!.Headline.Should().Contain("Tor");
            narrative.Groups.Should().NotBeEmpty();
            narrative.Groups[0].Id.Should().Be("identity");
            narrative.Groups[0].Title.Should().Be("Identity");
            narrative.Groups.Should().OnlyContain(g => SvirfdNarrative.GroupIds.Contains(g.Id));
            narrative.Groups.Should().OnlyContain(g => g.Paragraphs.Count > 0);
            SvirfdNarrative.Render(null).Should().BeNull();
        }

        [Fact]
        public async Task JsonRoundTripsTheWireShape()
        {
            var record = await SvirfdFixture.RecordAsync("37.30.56.54");
            var json = SvirfdJson.Serialize(record);

            json.Should().StartWith("{\"ip\":\"37.30.56.54\"");
            json.Should().Contain("\"privacy\":{\"vpn\":");
            json.Should().NotContain("\"Ip\"");
            SvirfdJson.Deserialize(json)!.Asn!.Asn.Should().Be("AS12912");
            SvirfdJson.Deserialize("not json").Should().BeNull();
            SvirfdJson.Deserialize(null).Should().BeNull();
        }

        [Fact]
        public void NullLookupIsInert()
        {
            var lookup = new SvirfdNullLookup();

            lookup.Enabled.Should().BeFalse();
            lookup.TryLookup("8.8.8.8", out var record).Should().BeFalse();
            record.Should().BeNull();
            lookup.Attribution.Should().Be(SvirfdCredit.Line);
        }
    }
}
