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

using System.Collections.Generic;
using FluentAssertions;
using Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery;
using Jube.Service.Query.CaseKeyJournal;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class JournalLocationTests
    {
        private static GetCaseWorkflowXPathByCaseWorkflowIdQueryDto XPath(string name, string path)
        {
            return new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto { Name = name, XPath = path };
        }

        [Theory]
        [InlineData("$.payload.Lat", "Lat")]
        [InlineData("$.payload['Lat']", "Lat")]
        [InlineData("$['payload']['Lat']", "Lat")]
        [InlineData("$ . payload . Lat ", "Lat")]
        [InlineData("payload.Lat", "Lat")]
        [InlineData("payload['Lat']", "Lat")]
        [InlineData("data.Lat", null)]
        [InlineData("payload.Geo.Lat", null)]
        [InlineData("$.payload.Geo.Lat", null)]
        [InlineData("$.activation.Lat", null)]
        [InlineData("$.payload", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void OnlyAPlainPayloadFieldHasAName(string? path, string? expected)
        {
            JournalLocation.PayloadName(path).Should().Be(expected);
        }

        [Fact]
        public void TheColumnsAreFoundByTheFieldsTheModelUses()
        {
            var xPaths = new List<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto>
            {
                XPath("Where", "$.payload.Lat"), XPath("Whence", "payload.Lon"), XPath("Other", "$.payload.Amount")
            };

            JournalLocation.Find(xPaths, "Lat", "Lon").Should().Be(new JournalLocation("Where", "Whence"));
        }

        [Fact]
        public void NoLocationIsReportedUnlessBothColumnsAreVisibleAndNamed()
        {
            var one = new List<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> { XPath("Where", "$.payload.Lat") };

            JournalLocation.Find(one, "Lat", "Lon").Should().BeNull();
            JournalLocation.Find(one, "Lat", null).Should().BeNull();
            JournalLocation.Find(one, null, "Lon").Should().BeNull();
            JournalLocation.Find([], "Lat", "Lon").Should().BeNull();
        }
    }
}
