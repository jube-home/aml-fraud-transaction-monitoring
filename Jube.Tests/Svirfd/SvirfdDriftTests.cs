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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Svirfd;
using Xunit;

namespace Jube.Test.Svirfd
{
    [Trait("Category", "Unit")]
    public sealed class SvirfdDriftTests
    {
        [Fact]
        public async Task IdenticalRecordsHaveNoDrift()
        {
            var record = await SvirfdFixture.RecordAsync("37.30.56.54");
            var again = SvirfdJson.Deserialize(SvirfdJson.Serialize(record));

            SvirfdDrift.Compare(record, again).Should().BeEmpty();
        }

        [Fact]
        public async Task ChangedScalarsAreReportedByPathWithVolatileFieldsIgnored()
        {
            var record = await SvirfdFixture.RecordAsync("37.30.56.54");

            var changed = record with
            {
                Asn = record.Asn! with { Name = "Google Cloud" },
                Privacy = record.Privacy with { Vpn = true },
                AsOf = record.AsOf with { IndexLoaded = record.AsOf.IndexLoaded.AddDays(1) },
                RequestId = "other"
            };

            var drift = SvirfdDrift.Compare(record, changed);

            drift.Should().HaveCount(2);
            drift.Should().ContainSingle(d => d.Path == "Asn.Name" && d.Stored == "T-Mobile Polska S.A." && d.Fresh == "Google Cloud");
            drift.Should().ContainSingle(d => d.Path == "Privacy.Vpn" && d.Stored == "false" && d.Fresh == "true");
        }

        [Fact]
        public async Task MissingRecordsCompareAsNoDrift()
        {
            var record = await SvirfdFixture.RecordAsync("37.30.56.54");

            SvirfdDrift.Compare(record, null).Should().BeEmpty();
            SvirfdDrift.Compare(null, record).Should().BeEmpty();
        }
    }
}
