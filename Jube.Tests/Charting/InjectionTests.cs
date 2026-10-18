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
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Interfaces;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting
{
    public class InjectionTests
    {
        public static IEnumerable<object[]> Hostile()
        {
            var values = new[]
            {
                "<script>alert(1)</script>",
                "\"><script>alert(1)</script>",
                "' onload='alert(1)",
                "a & b &amp; c &lt;",
                "]]><script>x</script><![CDATA[",
                "<![CDATA[ x ]]>",
                "<!-- comment -->",
                "<?xml version=\"1.0\"?>",
                "</text></svg><script>alert(1)</script>",
                "</style><script>alert(1)</script>",
                "<foreignObject><body onload=alert(1)></foreignObject>",
                "<a href=\"javascript:alert(1)\">x</a>",
                "\u0000\u0001\u0008\u000B\u000C\u001F\u007F\u0085",
                "\uD800 lone surrogate \uDC00",
                "￾￿",
                "‮evil‬ ⁦x⁩",
                "مرحبا بالعالم שלום",
                "日本語のテキスト 中文 한국어",
                "😀😀😀 emoji 👨‍👩‍👧‍👦",
                "line1\nline2\r\nline3\ttab",
                new string('x', 100_000),
                new string('<', 5_000),
                string.Empty,
                "   "
            };

            return values.Select(v => new object[] { v });
        }

        public static IEnumerable<ISvgChart> Charts(string text)
        {
            yield return Chart.Line().Title(text).Subtitle(text).XAxisTitle(text).YAxisTitle(text).Description(text)
                .Series(text, [1.0, 2, 3]);

            yield return Chart.Line().Title(text).Categories([text, text + "b", "c"])
                .Series(text, [new DataPoint(0, 1, text), new DataPoint(1, 2, text), new DataPoint(2, 3, text)]);

            yield return Chart.Area().Title(text).Categories([text, "b"]).Stacked().Series(text, [1.0, 2])
                .Series("x" + text, [1.0, 2]);

            yield return Chart.Column().Title(text).Categories([text, text, "b"]).Series(text, [1.0, 2, 3]);
            yield return Chart.Bar().Title(text).Categories([text, "b"]).Series(text, [1.0, -2]).ShowValues();

            yield return Chart.Scatter().Title(text).XAxisTitle(text)
                .Series(text, [new DataPoint(1, 2, text), new DataPoint(2, 3, text)]);

            yield return Chart.Histogram().Title(text).Values(text, [1.0, 2, 3, 4, 5]);
            yield return Chart.Pie().Title(text).Slice(text, 3).Slice(text + "2", 2);
            yield return Chart.Donut().Title(text).CenterLabel(text).Slice(text, 3).Slice("b", 2);

            yield return Chart.Heatmap().Title(text).Rows([text, "b"]).Columns([text, "d"])
                .Values([[1.0, 2], [3.0, 4]]);

            yield return Chart.Roc().Title(text)
                .Curve(text, [0.0, 0.5, 1], [0.0, 0.8, 1], [double.PositiveInfinity, 0.5, 0.1]);

            yield return Chart.PrecisionRecall().Title(text)
                .Curve(text, [0.0, 0.5, 1], [1.0, 0.8, 0.5], null, 0.7, 0.3);

            yield return Chart.Calibration().Title(text).Curve(text, [0.1, 0.5, 0.9], [0.2, 0.5, 0.8], [10.0, 20, 30]);

            yield return Chart.ConfusionMatrix().Title(text).Labels([text, "b"])
                .Matrix(new[,] { { 5.0, 1 }, { 2, 8 } });

            yield return Chart.LearningCurve().Title(text).Metric(text).Train([3.0, 2, 1]).Validation([3.5, 2.5, 2]);

            yield return Chart.Combo().Title(text).Categories([text, "b"]).Columns(text, [1.0, 2])
                .Line(text + "l", [2.0, 1], null, true);

            yield return Chart.BoxPlot().Title(text).FromValues(text, [1.0, 2, 3, 4, 50]);
            yield return Chart.Candlestick().Title(text).Candle(text, 1, 2, 0, 1.5);
            yield return Chart.Waterfall().Title(text).Step(text, 5).Total(text + "t");
            yield return Chart.Funnel().Title(text).Stage(text, 10).Stage("b", 4);
            yield return Chart.Radar().Title(text).Axes([text, "b", "c"]).Series(text, [1.0, 2, 3]);
            yield return Chart.Polar().Title(text).Series(text, [new DataPoint(10, 3, text)]);
            yield return Chart.Gauge().Title(text).Value(5, text).Band(0, 5, text);
            yield return Chart.Bullet().Title(text).Item(text, 5, 6, [3.0, 8]);
            yield return Chart.Sparkline().Title(text).Name(text).Values([1.0, 2, 3], [text, text, text]);
            yield return Chart.Treemap().Title(text).Item(text, 5).Item("b", 3);
            yield return Chart.Treemap().Title(text).Group(text, [(text, 5.0), ("x", 2.0)]);

            yield return Chart.Gantt().Title(text).Task(text, new DateTime(2026, 1, 1), new DateTime(2026, 1, 9), text)
                .Marker(new DateTime(2026, 1, 4), text);

            yield return Chart.Line().Title(text).Series("a", [1.0, 2]).Annotate(1, 1, text).HorizontalBand(0, 1, text)
                .HorizontalLine(1, text);
        }

        [Theory]
        [MemberData(nameof(Hostile))]
        public void HostileTextNeverProducesMarkup(string text)
        {
            foreach (var chart in Charts(text))
            {
                var svg = chart.Render();
                SvgAssert.IsSafe(svg, false);
                svg.Length.Should().BeLessThan(400_000);
            }
        }

        [Fact]
        public void ScriptTextAppearsOnlyEscaped()
        {
            var svg = Chart.Column().Title("<script>alert(1)</script>").Categories(["<b>x</b>"])
                .Series("\"quoted\" & 'single'", [1.0]).Render();
            svg.Should().Contain("&lt;script&gt;");
            svg.Should().Contain("&lt;b&gt;");
            svg.Should().Contain("&quot;quoted&quot; &amp; &apos;single&apos;");
            svg.Should().NotContain("<script");
            svg.Should().NotContain("<b>");
        }

        [Fact]
        public void CdataTerminatorIsEscaped()
        {
            var svg = Chart.Line().Title("a]]>b").Series("s", [1.0, 2]).Render();
            svg.Should().NotContain("]]>");
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void ControlCharactersAreRemovedFromText()
        {
            var svg = Chart.Line().Title("a\u0000b\u0001c\u001Fd").Series("s", [1.0, 2]).Render();
            svg.Any(c => c < 0x20 && c != '\t' && c != '\n' && c != '\r').Should().BeFalse();
            svg.Should().Contain("abcd");
        }

        [Fact]
        public void RightToLeftTextIsKeptButOverridesAreStripped()
        {
            var svg = Chart.Line().Title("‮abc שלום").Series("s", [1.0, 2]).Render();
            svg.Should().Contain("שלום");
            svg.Should().NotContain("‮");
        }

        [Theory]
        [InlineData("red")]
        [InlineData("#12345")]
        [InlineData("#ggg")]
        [InlineData("url(#x)")]
        [InlineData("#fff;} </style><script>")]
        [InlineData("expression(alert(1))")]
        public void InvalidColoursAreRejected(string colour)
        {
            var act = () => Chart.Line().Series("s", [1.0, 2], colour).Render();
            act.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void ValidColoursAreNormalisedIntoTheStylesheet()
        {
            var svg = Chart.Line().Series("s", [1.0, 2], "#ABC").Render();
            svg.Should().Contain("--sc:#aabbcc");
        }

        [Fact]
        public void LongTextIsTruncatedWithAnEllipsis()
        {
            var svg = Chart.Line().Title(new string('w', 500)).Series("s", [1.0, 2]).Render();
            svg.Should().Contain("…");
        }
    }
}