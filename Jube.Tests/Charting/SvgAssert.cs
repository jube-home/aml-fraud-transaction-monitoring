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
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;

namespace Jube.Test.Charting
{
    public static class SvgAssert
    {
        private static readonly HashSet<string> allowedElements =
        [
            "svg", "title", "desc", "style", "rect", "text", "line", "path", "circle", "g"
        ];

        private static readonly HashSet<string> allowedAttributes =
        [
            "xmlns", "class", "viewBox", "preserveAspectRatio", "width", "height", "data-theme", "data-node", "data-from",
            "data-to", "role",
            "aria-labelledby", "aria-label", "id", "x", "y", "x1", "y1", "x2", "y2", "d", "transform", "rx", "r", "cx",
            "cy", "style", "tabindex"
        ];

        public static XDocument WellFormed(string svg)
        {
            return XDocument.Parse(svg);
        }

        public static void IsSafe(string svg, bool strict = true)
        {
            var document = WellFormed(svg);
            svg.Should().NotContain("<script");
            svg.Should().NotContain("<foreignObject");
            svg.Should().NotContain("<image");
            svg.Should().NotContain(" href=\"");
            svg.Should().NotContain("xlink:");

            if (strict)
            {
                svg.Should().NotContain("javascript:");
                svg.Should().NotContain("href");
                svg.Should().NotContain("foreignObject");
                svg.Should().NotContain("@import");
                svg.Should().NotContain("NaN");
                svg.Should().NotContain("Infinity");
                svg.Should().NotContain("url(");
                svg.Replace("http://www.w3.org/2000/svg", string.Empty).Should().NotContain("http");
            }

            foreach (var element in document.Descendants())
            {
                allowedElements.Should().Contain(element.Name.LocalName);

                foreach (var attribute in element.Attributes())
                {
                    allowedAttributes.Should().Contain(attribute.Name.LocalName);
                    attribute.Name.LocalName.Should().NotStartWith("on");

                    if (strict && attribute.Name.LocalName != "xmlns")
                    {
                        attribute.Value.Should().NotContain("javascript:");
                        attribute.Value.Should().NotContain("http");
                        attribute.Value.Should().NotContain("url(");
                    }
                }
            }

            document.Descendants().Count(e => e.Name.LocalName == "style").Should().Be(1);
        }
    }
}