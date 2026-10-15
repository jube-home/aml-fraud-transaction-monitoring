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
using Jube.Engine.AdvancedAnalytics.RScript;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.RScript
{
    public class RScriptResultParserTests
    {
        [Fact]
        [Trait("Covers", "Parse")]
        public void ParseReturnsNullWhenNoConsoleOutput()
        {
            var result = RScriptResultParser.Parse(null);

            result.ResultJson.Should().BeNull();
            result.Warning.Should().BeNull();
        }

        [Fact]
        [Trait("Covers", "Parse")]
        public void ParseReturnsNullWhenMarkersAreAbsent()
        {
            var result = RScriptResultParser.Parse("just some console output, no markers here");

            result.ResultJson.Should().BeNull();
            result.Warning.Should().BeNull();
        }

        [Fact]
        [Trait("Covers", "Parse")]
        public void ParseExtractsValidJsonBetweenMarkers()
        {
            var consoleOutput = "some log line\n" +
                                RScriptResultParser.StartMarker + "\n" +
                                "{\"mean\":1.5,\"n\":10}" + "\n" +
                                RScriptResultParser.EndMarker + "\n" +
                                "done";

            var result = RScriptResultParser.Parse(consoleOutput);

            result.ResultJson.Should().Be("{\"mean\":1.5,\"n\":10}");
            result.Warning.Should().BeNull();
        }

        [Fact]
        [Trait("Covers", "Parse")]
        public void ParseReturnsWarningWhenMarkersWrapInvalidJson()
        {
            var consoleOutput = RScriptResultParser.StartMarker + "not json at all" + RScriptResultParser.EndMarker;

            var result = RScriptResultParser.Parse(consoleOutput);

            result.ResultJson.Should().BeNull();
            result.Warning.Should().Be(RScriptResultParser.InvalidJsonWarning);
        }

        [Fact]
        [Trait("Covers", "Parse")]
        public void ParseReturnsNullWhenEndMarkerPrecedesStartMarker()
        {
            var consoleOutput = RScriptResultParser.EndMarker + "{}" + RScriptResultParser.StartMarker;

            var result = RScriptResultParser.Parse(consoleOutput);

            result.ResultJson.Should().BeNull();
            result.Warning.Should().BeNull();
        }

        [Fact]
        [Trait("Covers", "Parse")]
        public void ParseReturnsNullWhenOnlyStartMarkerIsPresent()
        {
            var consoleOutput = RScriptResultParser.StartMarker + "{\"a\":1}";

            var result = RScriptResultParser.Parse(consoleOutput);

            result.ResultJson.Should().BeNull();
            result.Warning.Should().BeNull();
        }
    }
}