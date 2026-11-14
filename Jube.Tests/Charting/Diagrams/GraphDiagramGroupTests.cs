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

using System.Text.RegularExpressions;
using System.Xml;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Xunit;

namespace Jube.Test.Charting.Diagrams
{
    public class GraphDiagramGroupTests
    {
        private static string Render(bool withGroups = true)
        {
            var builder = Diagram.Graph().Title("Dependencies")
                .Node("Rule:1", "Rule one", group: withGroups ? "Decisions" : null)
                .Node("Counter:1", "Counter", group: withGroups ? "Data" : null, dashed: true)
                .Node("Missing:Gone", "Gone", group: withGroups ? "Missing" : null)
                .Edge("Rule:1", "Counter:1", label: "uses")
                .Edge("Rule:1", "Missing:Gone", dashed: true);

            return builder.Render();
        }

        [Fact]
        public void NodesAreColouredAndKeyedByTheirGroupWithMissingAlwaysOnTheLastColour()
        {
            var svg = Render();

            svg.Should().Contain("Decisions").And.Contain("Data").And.Contain("Missing");
            Regex.IsMatch(svg, "class=\"[^\"]*nd s0[^\"]*\"[^>]*data-node=\"Rule:1\"").Should().BeTrue();
            Regex.IsMatch(svg, "class=\"[^\"]*nd s1[^\"]*\"[^>]*data-node=\"Counter:1\"").Should().BeTrue();
            Regex.IsMatch(svg, "class=\"[^\"]*nd s7[^\"]*\"[^>]*data-node=\"Missing:Gone\"").Should().BeTrue();
        }

        [Fact]
        public void EveryNodeAndEdgeCarriesItsIdentityForTheBrowserToSelectBy()
        {
            var svg = Render();

            Regex.Matches(svg, "data-node=\"").Count.Should().Be(3);
            svg.Should().Contain("data-from=\"Rule:1\"").And.Contain("data-to=\"Counter:1\"");
        }

        [Fact]
        public void ADashedNodeIsDrawnWithADashedOutline()
        {
            Regex.IsMatch(Render(), "<rect class=\"nb dash\"").Should().BeTrue();
            Regex.Matches(Render(), "class=\"n[bt] dash\"").Count.Should().Be(2);
        }

        [Fact]
        public void WithoutGroupsTheKeyStillNamesTheRolesAsBefore()
        {
            var svg = Render(false);

            svg.Should().Contain("Feature");
            svg.Should().NotContain("Decisions");
        }

        [Fact]
        public void HostileIdsAndGroupsAreEscapedAndTheSvgStaysWellFormed()
        {
            var svg = Diagram.Graph().Node("a\"><script>alert(1)</script>", "x", group: "<b>g</b>")
                .Node("b", "y", group: "<b>g</b>").Edge("a\"><script>alert(1)</script>", "b").Render();

            svg.Should().NotContainEquivalentOf("<script");
            new XmlDocument { XmlResolver = null }.Invoking(d => d.LoadXml(svg)).Should().NotThrow();
        }
    }
}
