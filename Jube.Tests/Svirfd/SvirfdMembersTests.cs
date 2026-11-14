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
using FluentAssertions;
using Jube.Svirfd;
using Svirfd;
using Xunit;

namespace Jube.Test.Svirfd
{
    [Trait("Category", "Unit")]
    public sealed class SvirfdMembersTests
    {
        [Fact]
        public void EveryScalarOfTheRecordGraphIsCatalogued()
        {
            var expected = new List<string>();

            Walk(typeof(IpResponse), "", expected, 0);

            var catalogued = SvirfdMembers.Scalars.Where(s => !s.Array && !s.Name.StartsWith("Routing.Origin."))
                .Select(s => s.Name).ToList();

            catalogued.Should().BeEquivalentTo(expected);
            SvirfdMembers.Scalars.Should().Contain(s => s.Name == "Routing.Origin.Rpki");
            SvirfdMembers.Scalars.Should().OnlyHaveUniqueItems(s => s.Name);
        }

        [Fact]
        public void TheTimezoneAndTheOperatorDomainMatchFlagAreCatalogued()
        {
            SvirfdMembers.Scalars.Should().Contain(s => s.Name == "Timezone.Id" && s.DataTypeId == 1);
            SvirfdMembers.Scalars.Should().Contain(s => s.Name == "Timezone.Basis");
            SvirfdMembers.Scalars.Should().Contain(s => s.Name == "Asn.DomainMatchesOperator");
        }

        [Fact]
        public void PathsFollowTheWireShape()
        {
            SvirfdMembers.TryGet("Privacy.Vpn", out var vpn).Should().BeTrue();
            vpn!.JsonPath.Should().Be("privacy.vpn");
            vpn.DataTypeId.Should().Be(SvirfdMembers.BooleanType);
            vpn.JQueryBuilderDataType.Should().Be("boolean");
            vpn.SqlPath("IP").Should().Be("(\"Json\"->'svirfd'->'IP'->'privacy'->>'vpn')::boolean");

            SvirfdMembers.TryGet("Location.Measured.RttMs", out var rtt).Should().BeTrue();
            rtt!.SqlPath("IP").Should()
                .Be("(\"Json\"->'svirfd'->'IP'->'location'->'measured'->>'rttMs')::double precision");

            SvirfdMembers.TryGet("Routing.Origins.Count", out var origins).Should().BeTrue();
            origins!.Array.Should().BeTrue();
            origins.SqlPath("IP").Should().Be("jsonb_array_length(\"Json\"->'svirfd'->'IP'->'routing'->'origins')");

            SvirfdMembers.TryGet("Routing.Origin.Rpki", out var rpki).Should().BeTrue();
            rpki!.JsonPath.Should().Be("routing.origins[0].rpki");
            rpki.SqlPath("IP").Should().Be("(\"Json\"->'svirfd'->'IP'->'routing'->'origins'->0->>'rpki')");

            SvirfdMembers.TryGet("Asn.Name", out var name).Should().BeTrue();
            name!.SqlPath("IP").Should().Be("(\"Json\"->'svirfd'->'IP'->'asn'->>'name')");
        }

        [Fact]
        public void MethodsAndTokensAreRecognised()
        {
            SvirfdMembers.IsMethod("DistanceKm").Should().BeTrue();
            SvirfdMembers.IsMethod("DistanceKm(1, 2)").Should().BeTrue();
            SvirfdMembers.IsMember("WithinKm").Should().BeTrue();
            SvirfdMembers.IsMember("Privacy.Vpn").Should().BeTrue();
            SvirfdMembers.IsMember("Privacy.Nope").Should().BeFalse();
            SvirfdMembers.Tokens.Should().Contain(["Privacy", "Vpn", "DistanceKm", "Origin"]);
        }

        [Fact]
        public void SplitSeparatesEntityFromMember()
        {
            SvirfdMembers.Split("IP.Privacy.Vpn", out var entity, out var member).Should().BeTrue();
            entity.Should().Be("IP");
            member.Should().Be("Privacy.Vpn");
            SvirfdMembers.Split("IP", out _, out _).Should().BeFalse();
            SvirfdMembers.Split(null, out _, out _).Should().BeFalse();
        }

        [Fact]
        public async System.Threading.Tasks.Task ReadsNumbersAndTextFromARecord()
        {
            var record = await SvirfdFixture.RecordAsync("37.30.56.54");
            var records = new Dictionary<string, IpResponse> { ["IP"] = record };

            SvirfdMembers.TryReadText(records, "IP.Asn.Name", out var name).Should().BeTrue();
            name.Should().Be("T-Mobile Polska S.A.");
            SvirfdMembers.TryReadNumber(records, "IP.Routing.Origin.Peers", out var peers).Should().BeTrue();
            peers.Should().BeGreaterThan(0);
            SvirfdMembers.TryReadNumber(records, "IP.Privacy.Tor", out var tor).Should().BeTrue();
            tor.Should().Be(0);
            SvirfdMembers.TryReadNumber(records, "IP.Lists.Count", out var lists).Should().BeTrue();
            lists.Should().Be(0);
            SvirfdMembers.TryReadNumber(records, "IP.Location.Hub.RadiusKm", out _).Should().BeFalse();
            SvirfdMembers.TryReadNumber(records, "Other.Asn.Name", out _).Should().BeFalse();
            SvirfdMembers.TryReadNumber(records, "IP.Asn.Missing", out _).Should().BeFalse();
        }

        private static void Walk(Type type, string prefix, List<string> names, int depth)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (depth == 0 && property.Name is nameof(IpResponse.Narrative) or nameof(IpResponse.RequestId)
                        or nameof(IpResponse.ServedFrom) or nameof(IpResponse.Attribution))
                {
                    continue;
                }

                var name = prefix + property.Name;
                var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                if (propertyType.IsArray)
                {
                    continue;
                }

                if (propertyType == typeof(string) || propertyType.IsPrimitive || propertyType == typeof(DateTime) ||
                    propertyType == typeof(DateOnly) || propertyType == typeof(decimal))
                {
                    names.Add(name);

                    continue;
                }

                if (propertyType.IsClass && propertyType.Namespace!.StartsWith("Svirfd", StringComparison.Ordinal))
                {
                    Walk(propertyType, name + ".", names, depth + 1);
                }
            }
        }
    }
}
