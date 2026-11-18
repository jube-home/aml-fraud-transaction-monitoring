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
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Icons;
using Jube.Charting.Diagrams.Models;
using Xunit;

namespace Jube.Test.Charting.Diagrams
{
    public class GraphIconTests
    {
        private static string Render(string? icon)
        {
            return Diagram.Graph()
                .Layout(GraphLayoutKind.Force)
                .Node("a", "Account", GraphNodeShape.Circle, group: "AccountId", icon: icon)
                .Node("b", "Device", GraphNodeShape.Circle, group: "DeviceId", icon: icon)
                .Edge("a", "b")
                .Render();
        }

        [Theory]
        [InlineData("AccountId", "landmark")]
        [InlineData("ToAccountId", "landmark")]
        [InlineData("DeviceId", "smartphone")]
        [InlineData("IpAddress", "globe")]
        [InlineData("Network", "globe")]
        [InlineData("Email", "mail")]
        [InlineData("CardNumber", "credit-card")]
        [InlineData("Case", "briefcase")]
        [InlineData("Payee", "hand-coins")]
        [InlineData("Banana", "circle-dot")]
        [InlineData("", "circle-dot")]
        [InlineData(null, "circle-dot")]
        public void NodeKeysFallBackToAMeaningfulIconByTheirName(string? key, string expected)
        {
            GraphIcons.ForNodeKey(key).Should().Be(expected);
        }

        [Fact]
        public void EveryIconTheFallbackCanChooseExistsInTheGeneratedTable()
        {
            foreach (var key in new[]
                         { "Case", "AccountId", "Payee", "DeviceId", "Ip", "Email", "Phone", "Card", "Merchant", "Customer", "Company", "Address", "Wallet", "Document", "Session", "Tag", "Unknown" })
            {
                GraphIcons.Exists(GraphIcons.ForNodeKey(key)).Should().BeTrue(key);
            }
        }

        [Fact]
        public void TheGeneratedTableCarriesStrokeGeometryForEveryIcon()
        {
            LucideIcons.Elements.Count.Should().BeGreaterThan(150);
            LucideIcons.Elements.Values.Should().OnlyContain(e => e.Count > 0 && e.All(x => x.Attributes.Length > 0));
        }

        [Fact]
        public void ANodeWithAnIconDrawsThatIconInsideItsGlyph()
        {
            var svg = Render("smartphone");

            Regex.Matches(svg, "class=\"ic\"").Count.Should().Be(2);
            svg.Should().Contain("aria-hidden=\"true\"");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("not-a-real-icon")]
        [InlineData("<script>alert(1)</script>")]
        public void ANodeWithNoKnownIconRendersExactlyAsBefore(string? icon)
        {
            var svg = Render(icon);

            svg.Should().NotContain("class=\"ic\"").And.NotContain("<script>");
        }
    }
}
