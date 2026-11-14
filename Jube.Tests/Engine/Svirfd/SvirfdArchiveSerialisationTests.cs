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

using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.Helpers;
using Svirfd;
using Xunit;

namespace Jube.Test.Engine.Svirfd
{
    [Trait("Category", "Unit")]
    public sealed class SvirfdArchiveSerialisationTests
    {
        [Fact]
        public async Task TheArchiveWritesOnlyTheReportTableRecordsAsCamelCaseWireJsonAsync()
        {
            var record = await Jube.Test.Svirfd.SvirfdFixture.RecordAsync("37.30.56.54");
            var other = await Jube.Test.Svirfd.SvirfdFixture.RecordAsync("185.220.101.151");

            var payload = new EntityAnalysisModelInstanceEntryPayload
            {
                Svirfd = new PooledDictionary<string, IpResponse> { ["IP"] = record, ["Other"] = other },
                SvirfdArchiveNames = ["IP"]
            };

            var json = Encoding.UTF8.GetString(BuildJsonResponses.BuildFullJson(payload,
                new JsonSerializationHelper().ArchiveJsonSerializer));

            using var document = System.Text.Json.JsonDocument.Parse(json);

            var svirfd = document.RootElement.GetProperty("svirfd");
            svirfd.TryGetProperty("Other", out _).Should().BeFalse();
            svirfd.GetProperty("IP").GetProperty("asn").GetProperty("name").GetString().Should().Be("T-Mobile Polska S.A.");
        }
    }
}
