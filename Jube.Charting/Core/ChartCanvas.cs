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

using Jube.Charting.Core.Models;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme;
using Jube.Charting.Theme.Models;

namespace Jube.Charting.Core
{
    internal sealed class ChartCanvas
    {
        public const double Pad = 16;

        public ChartCanvas(ChartOptions options, string id, string summary, IReadOnlyList<LegendEntry> legend,
            int seriesCount, IReadOnlyList<string?> colours, bool ramps = false)
        {
            Options = options;
            Id = id;
            this.summary = summary;
            this.legend = legend;
            this.seriesCount = seriesCount;
            this.colours = colours;
            this.ramps = ramps;
            Width = options.Width;
            Height = options.Height;

            legendPosition = options.Legend == LegendPosition.Auto
                ? legend.Count >= 2 ? LegendPosition.Top : LegendPosition.None
                : options.Legend;

            if (legend.Count == 0)
            {
                legendPosition = LegendPosition.None;
            }

            Interactive = options.Interactive;
        }

        public string Id { get; }
        public double Width { get; }
        public double Height { get; }
        public SvgWriter W { get; } = new();
        public Rect Plot { get; private set; }
        public bool Interactive { get; private set; }
        public ChartOptions Options { get; }

        public string? ExtraCss { get; set; }
        public StyleFamily Family { get; set; }
        public bool External => Options.StyleMode == ChartStyleMode.External;
        public string K => "." + Id;

        public double EstimatedPlotHeight => Math.Max(60, Height - TopBlock() - Pad - 40);
        public double EstimatedPlotWidth => Math.Max(60, Width - 2 * Pad - 40);

        public bool LimitInteractive(long marks)
        {
            Interactive = Options.Interactive && marks <= ChartLimits.InteractiveMarksPerChart;

            return Interactive;
        }

        public Rect Frame(double leftLabelWidth, double bottomLabelHeight, double rightReserve = 0)
        {
            var left = Pad;

            if (!string.IsNullOrWhiteSpace(Options.YAxisTitle))
            {
                left += AxisTitleHeight;
            }

            if (leftLabelWidth > 0)
            {
                left += leftLabelWidth + 8;
            }
            else
            {
                left += 4;
            }

            var right = Width - Pad - rightReserve;

            if (legendPosition == LegendPosition.Right)
            {
                right -= LegendLayoutFor().Width + 12;
            }

            var bottom = Height - Pad - bottomLabelHeight;

            if (!string.IsNullOrWhiteSpace(Options.XAxisTitle))
            {
                bottom -= AxisTitleHeight;
            }

            if (legendPosition == LegendPosition.Bottom)
            {
                bottom -= LegendLayoutFor().Height + 8;
            }

            var top = TopBlock();

            if (right - left < 40)
            {
                right = left + 40;
            }

            if (bottom - top < 40)
            {
                bottom = top + 40;
            }

            Plot = new Rect(left, top, right, bottom);

            return Plot;
        }

        public void SetPlot(Rect plot)
        {
            Plot = plot;
        }

        public void FramePlain(double inset = 0)
        {
            Frame(0, 0);
            Plot = new Rect(Plot.Left - 4 + inset, Plot.Top, Plot.Right - inset, Plot.Bottom);
        }

        public void Begin()
        {
            var themeAttribute = Options.Theme switch
            {
                ThemeMode.Light => "light",
                ThemeMode.Dark => "dark",
                _ => null
            };

            var attrs = new List<(string, string?)>
            {
                ("xmlns", "http://www.w3.org/2000/svg"),
                ("class", External ? RootClass() : Id),
                ("viewBox", "0 0 " + Num.F(Width) + " " + Num.F(Height)),
                ("preserveAspectRatio", "xMidYMid meet")
            };

            if (Options.Sizing == ChartSizing.Fixed)
            {
                attrs.Add(("width", Num.F(Width)));
                attrs.Add(("height", Num.F(Height)));
            }

            attrs.Add(("data-theme", themeAttribute));

            if (External)
            {
                var custom = ChartCss.ExternalCustomColours(colours, seriesCount);
                attrs.Add(("style", custom.Length == 0 ? null : custom));
            }

            attrs.Add(("role", "img"));
            attrs.Add(("aria-labelledby", Id + "-t " + Id + "-d"));
            W.Open("svg", [.. attrs]);
            var title = string.IsNullOrWhiteSpace(Options.Title) ? "Chart" : Options.Title;
            W.Element("title", XmlText.Clean(title, 300), ("id", Id + "-t"));
            var desc = string.IsNullOrWhiteSpace(Options.Description) ? summary : Options.Description;
            W.Element("desc", XmlText.Clean(desc, 1200), ("id", Id + "-d"));

            if (!External)
            {
                var css = ChartCss.Build(Id, Options, seriesCount, colours);

                if (ramps)
                {
                    css += ChartCss.SequentialClasses(K);
                }

                if (Family != StyleFamily.None)
                {
                    css += StyleFamilies.Rules(Family, K);
                }

                if (ExtraCss is not null)
                {
                    css += ExtraCss;
                }

                W.Style(css);
            }

            if (!Options.TransparentBackground)
            {
                W.Empty("rect", ("class", "bg"), ("x", "0"), ("y", "0"), ("width", Num.F(Width)),
                    ("height", Num.F(Height)), ("rx", "8"));
            }

            var y = Pad;

            if (!string.IsNullOrWhiteSpace(Options.Title))
            {
                y += 16;

                W.Element("text", TextMetrics.Truncate(Options.Title, Width - 2 * Pad, 16, true),
                    ("class", "ttl"), ("x", Num.F(Pad)), ("y", Num.F(y)));

                y += 8;
            }

            if (!string.IsNullOrWhiteSpace(Options.Subtitle))
            {
                y += 10;

                W.Element("text", TextMetrics.Truncate(Options.Subtitle, Width - 2 * Pad, 12),
                    ("class", "sub"), ("x", Num.F(Pad)), ("y", Num.F(y)));
            }

            if (legendPosition == LegendPosition.Top)
            {
                DrawLegend(Pad, TopBlock() - 8 - LegendLayoutFor().Height);
            }
            else if (legendPosition == LegendPosition.Bottom)
            {
                DrawLegend(Pad, Height - Pad - LegendLayoutFor().Height);
            }
            else if (legendPosition == LegendPosition.Right)
            {
                DrawLegend(Width - Pad - LegendLayoutFor().Width, Plot.Top);
            }
        }

        public string End()
        {
            W.Close();
            var result = W.ToString();
            ChartLimits.Output(result.Length);

            return result;
        }

        public void Text(double x, double y, string? text, string cls, string? extra = null)
        {
            W.Element("text", text, ("class", cls), ("x", Num.F(x)), ("y", Num.F(y)), ("transform", extra));
        }

        public void YAxis(IReadOnlyList<Tick> ticks, bool grid = true)
        {
            foreach (var tick in ticks)
            {
                if (!double.IsFinite(tick.Position))
                {
                    continue;
                }

                if (grid)
                {
                    W.Empty("line", ("class", "gl"), ("x1", Num.F(Plot.Left)), ("y1", Num.F(tick.Position)),
                        ("x2", Num.F(Plot.Right)), ("y2", Num.F(tick.Position)));
                }

                W.Element("text", tick.Label, ("class", "tk end mid"), ("x", Num.F(Plot.Left - 8)),
                    ("y", Num.F(tick.Position)));
            }
        }

        public void XAxis(IReadOnlyList<Tick> ticks, bool grid = false)
        {
            var lastEnd = double.NegativeInfinity;

            foreach (var tick in ticks)
            {
                if (!double.IsFinite(tick.Position))
                {
                    continue;
                }

                if (grid)
                {
                    W.Empty("line", ("class", "gl"), ("x1", Num.F(tick.Position)), ("y1", Num.F(Plot.Top)),
                        ("x2", Num.F(tick.Position)), ("y2", Num.F(Plot.Bottom)));
                }

                var half = TextMetrics.Width(tick.Label, 11) / 2;

                if (tick.Position - half < lastEnd + 6)
                {
                    continue;
                }

                lastEnd = tick.Position + half;

                W.Element("text", tick.Label, ("class", "tk ctr"), ("x", Num.F(tick.Position)),
                    ("y", Num.F(Plot.Bottom + 16)));
            }
        }

        public void Baseline(double y)
        {
            W.Empty("line", ("class", "ax"), ("x1", Num.F(Plot.Left)), ("y1", Num.F(y)), ("x2", Num.F(Plot.Right)),
                ("y2", Num.F(y)));
        }

        public void VerticalBaseline(double x)
        {
            W.Empty("line", ("class", "ax"), ("x1", Num.F(x)), ("y1", Num.F(Plot.Top)), ("x2", Num.F(x)),
                ("y2", Num.F(Plot.Bottom)));
        }

        public void AxisTitles(double bottomLabelHeight = 20)
        {
            if (!string.IsNullOrWhiteSpace(Options.XAxisTitle))
            {
                var text = TextMetrics.Truncate(Options.XAxisTitle, Plot.Width, 12);
                Text(Plot.CenterX, Plot.Bottom + bottomLabelHeight + 14, text, "at ctr");
            }

            if (!string.IsNullOrWhiteSpace(Options.YAxisTitle))
            {
                var text = TextMetrics.Truncate(Options.YAxisTitle, Plot.Height, 12);
                var x = Pad + 8;

                W.Element("text", text, ("class", "at ctr"), ("x", "0"), ("y", "0"),
                    ("transform", "translate(" + Num.F(x) + " " + Num.F(Plot.CenterY) + ") rotate(-90)"));
            }
        }

        public void OpenSeries(int series)
        {
            W.Open("g", ("class", "sr s" + series % ChartLimits.MaxSeries));
        }

        public void OpenMark(int series, string label, string? extraClass = null, string? style = null)
        {
            var cls = "mk" + (series >= 0 ? " s" + series % ChartLimits.MaxSeries : string.Empty) +
                      (extraClass is null ? string.Empty : " " + extraClass);

            if (External)
            {
                cls = string.Join(' ', cls.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }

            if (!Interactive)
            {
                W.Open("g", ("class", series >= 0 ? "s" + series % ChartLimits.MaxSeries : null), ("style", style));

                return;
            }

            W.Open("g", ("class", cls), ("tabindex", "0"), ("role", "graphics-symbol"),
                ("aria-label", XmlText.Clean(label, 300)), ("style", style));

            W.Element("title", XmlText.Clean(label, 300));
        }

        public void Tooltip(double anchorX, double anchorY, IReadOnlyList<TipRow> rows, bool right = false,
            double gap = 10)
        {
            if (!Interactive || rows.Count == 0)
            {
                return;
            }

            const double fontSize = 12;
            var texts = rows.Select(r => TextMetrics.Truncate(r.Text, 260, fontSize, r.Bold)).ToArray();
            var textWidth = 0.0;

            for (var i = 0; i < rows.Count; i++)
            {
                var w = TextMetrics.Width(texts[i], fontSize, rows[i].Bold) + (rows[i].Series.HasValue ? 14 : 0);
                textWidth = Math.Max(textWidth, w);
            }

            var boxWidth = textWidth + 20;
            var boxHeight = rows.Count * 17 + 10;
            double x;
            double y;

            if (right)
            {
                x = anchorX + gap;

                if (x + boxWidth > Plot.Right)
                {
                    x = anchorX - boxWidth - gap;
                }

                y = anchorY - (double)boxHeight / 2;
            }
            else
            {
                x = anchorX - boxWidth / 2;
                y = anchorY - boxHeight - gap;

                if (y < Plot.Top - 6)
                {
                    y = anchorY + gap;
                }
            }

            var minX = boxWidth <= Plot.Width ? Plot.Left : 4;
            var maxX = boxWidth <= Plot.Width ? Plot.Right - boxWidth : Width - boxWidth - 4;
            x = Math.Clamp(x, minX, Math.Max(minX, maxX));
            var minY = boxHeight <= Plot.Height ? Plot.Top : 4;
            var maxY = boxHeight <= Plot.Height ? Plot.Bottom - boxHeight : Height - boxHeight - 4;
            y = Math.Clamp(y, minY, Math.Max(minY, maxY));

            W.Open("g", ("class", "tt"));

            W.Empty("rect", ("class", "tb"), ("x", Num.F(x)), ("y", Num.F(y)), ("width", Num.F(boxWidth)),
                ("height", Num.F(boxHeight)), ("rx", "6"));

            for (var i = 0; i < rows.Count; i++)
            {
                var ty = y + 18 + i * 17;
                var tx = x + 10;

                if (rows[i].Series.HasValue)
                {
                    W.Empty("rect", ("class", "tsw s" + rows[i].Series!.Value % ChartLimits.MaxSeries),
                        ("x", Num.F(tx)), ("y", Num.F(ty - 9)), ("width", "9"), ("height", "9"), ("rx", "2"));

                    tx += 14;
                }

                W.Element("text", texts[i], ("class", rows[i].Bold ? "tx b" : "tx"), ("x", Num.F(tx)),
                    ("y", Num.F(ty)));
            }

            W.Close();
        }

        public void CloseMark()
        {
            W.Close();
        }

        private const double TitleHeight = 24;
        private const double SubtitleHeight = 18;
        private const double LegendRowHeight = 20;
        private const double AxisTitleHeight = 18;
        private readonly IReadOnlyList<string?> colours;
        private readonly IReadOnlyList<LegendEntry> legend;
        private readonly LegendPosition legendPosition;

        private readonly bool ramps;
        private readonly int seriesCount;
        private readonly string summary;
        private LegendLayout? legendLayout;

        private double TopBlock()
        {
            var top = Pad;

            if (!string.IsNullOrWhiteSpace(Options.Title))
            {
                top += TitleHeight;
            }

            if (!string.IsNullOrWhiteSpace(Options.Subtitle))
            {
                top += SubtitleHeight;
            }

            if (legendPosition == LegendPosition.Top)
            {
                top += LegendLayoutFor().Height + 4;
            }

            return top + 8;
        }

        private int LegendRows()
        {
            if (Options.LegendMaxRows >= 0)
            {
                return Options.LegendMaxRows;
            }

            return legendPosition == LegendPosition.Right
                ? Math.Max(2, (int)((Height - 2 * Pad - 60) / LegendRowHeight))
                : 3;
        }

        private LegendLayout LegendLayoutFor()
        {
            legendLayout ??= LegendLayout.Compute(legend, legendPosition, Width - 2 * Pad, LegendRows());

            return legendLayout;
        }

        private string RootClass()
        {
            return Family == StyleFamily.None ? "jc" : "jc " + StyleFamilies.Token(Family);
        }

        private void DrawLegend(double x0, double y0)
        {
            var layout = LegendLayoutFor();

            foreach (var item in layout.Items)
            {
                var x = x0 + item.X;
                var y = y0 + item.Y;
                var more = item.Entry.Shape == LegendShape.None;

                W.Open("g",
                    ("class", more ? External ? "lg" : "lg more" : "lg s" + item.Entry.Series % ChartLimits.MaxSeries));

                W.Empty("rect", ("class", "hit"), ("x", Num.F(x - 2)), ("y", Num.F(y)), ("width", Num.F(item.Width)),
                    ("height", Num.F(LegendRowHeight - 2)));

                if (more)
                {
                    W.Element("text", item.Text, ("class", "lgt"), ("x", Num.F(x)), ("y", Num.F(y + 13)));
                    W.Close();
                    continue;
                }

                if (item.Entry.Shape == LegendShape.Line)
                {
                    W.Empty("rect", ("class", "sw"), ("x", Num.F(x)), ("y", Num.F(y + 7)), ("width", "14"),
                        ("height", "3"), ("rx", "1.5"));
                }
                else
                {
                    W.Empty("rect", ("class", "sw"), ("x", Num.F(x)), ("y", Num.F(y + 4)), ("width", "10"),
                        ("height", "10"), ("rx", "2"));
                }

                W.Element("text", item.Text, ("class", "lgt"), ("x", Num.F(x + 18)), ("y", Num.F(y + 13)));
                W.Close();
            }
        }
    }
}