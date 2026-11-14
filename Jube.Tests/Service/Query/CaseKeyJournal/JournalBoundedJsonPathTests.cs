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

using FluentAssertions;
using Jube.Data.Helpers;
using Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery;
using Jube.Service.Query.CaseKeyJournal;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class JournalBoundedJsonPathTests
    {
        private const string Catastrophic = "$.payload[?(@.s =~ /^(a+)+$/)]";

        private static readonly JObject document = JObject.Parse(
            "{\"payload\":[{\"s\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaX\"}],\"c\":\"v\"}");

        [Fact]
        public void APlainPathStillSelectsTheValue()
        {
            BoundedJsonPath.Select(document, "$.c")!.ToString().Should().Be("v");
        }

        [Fact]
        public void ACatastrophicRegexFilterSelectsNothingInsteadOfHangingOrThrowing()
        {
            BoundedJsonPath.Select(document, Catastrophic).Should().BeNull();
        }

        [Fact]
        public void ANullDocumentSelectsNothing()
        {
            BoundedJsonPath.Select(null, "$.c").Should().BeNull();
        }

        [Fact]
        public void TheTransactionKeysReaderSurvivesAStoredCatastrophicPath()
        {
            var keys = JournalTransactionReader.Keys(document,
            [
                new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto
                {
                    Name = "Hostile", XPath = Catastrophic, Drill = true
                }
            ]);

            keys.Should().ContainSingle().Which.Value.Should().BeNull();
        }
    }
}
