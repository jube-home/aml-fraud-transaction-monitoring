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

using Jube.Charting.Core;
using Jube.Charting.Interfaces;
using Jube.Charting.More.Models;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme.Models;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    public sealed class SurvivalChart(SurvivalChartSpec spec) : IStyleModeChart
    {
        private const double RowHeight = 16;

        public SurvivalChartSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new SurvivalChart(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            return ChartRun.Execute(Spec.Options, Draw);
        }

        private string Draw(string id)
        {
            var s = Spec;
            ChartLimits.Series(s.Groups.Count);
            ChartLimits.Observations(s.Groups.Sum(g => (long)g.Times.Count));
            var colours = Guard.Colours(s.Groups.Select(g => g.Color));

            var curves = s.Groups.Select(g => g.Curve ?? SurvivalMath.KaplanMeier(g.Times, g.Events, s.Confidence,
                s.Bands ? s.Method : ConfidenceMethod.None)).ToArray();

            LogRankResult? logRank = null;

            if (s is { LogRankTest: true, Groups.Count: >= 2 } && s.Groups.All(g => g.Curve is null) &&
                s.Groups.All(g => g.Times.Count > 0))
            {
                logRank = SurvivalMath.LogRank([.. s.Groups.Select(g => g.Times)],
                    [.. s.Groups.Select(g => g.Events)]);
            }

            var legend = s.Groups
                .Select((g, i) => new LegendEntry(g.Name + " (n=" + curves[i].Count + ")", i, LegendShape.Line))
                .ToList();

            var names = s.Groups.Select(g => g.Name).ToArray();

            var summary = Guard.Summary(s.Cumulative ? "Cumulative incidence chart" : "Survival chart", names,
                string.Join(" ", curves.Select((c, i) => TextMetrics.Truncate(names[i], 80, 12) + ": " + c.Count +
                                                         " subjects, " + c.EventCount + " events" +
                                                         (c.Median is { } m
                                                             ? ", median " + NumberFormat.Value(m)
                                                             : string.Empty) + ".")
                    .Take(6)) + (logRank is null ? string.Empty : " Log-rank p " + PText(logRank.PValue) + "."));

            var canvas = new ChartCanvas(MoreKit.AutoLegend(s.Options), id, summary, legend, s.Groups.Count, colours);
            var showTable = s is { AtRisk: true, Groups.Count: > 0 and <= 12 };
            var strip = showTable ? 20 + RowHeight * s.Groups.Count + 8 : 0;
            var maxTime = curves.Length == 0 ? 1 : curves.Max(c => c.MaxTime);

            if (!(maxTime > 0))
            {
                maxTime = 1;
            }

            var xd = s.XDomain;
            canvas.Family = StyleFamily.Survival;
            var plot = canvas.Frame(30, 20 + strip);
            IScale ys;
            IReadOnlyList<Tick> yTicks;

            for (var pass = 0; pass < 2; pass++)
            {
                (ys, var values) = AxisFactory.Numeric(0, 1, plot.Bottom, plot.Top, AxisKind.Linear,
                    (int)Math.Clamp(plot.Height / 42, 3, 10), (0, 1));

                yTicks = AxisFactory.ToTicks(ys, values, v => NumberFormat.Percent(v, 0));
                plot = canvas.Frame(TextMetrics.MaxWidth(yTicks.Select(t => t.Label), 11), 20 + strip);
            }

            (ys, var yValues) = AxisFactory.Numeric(0, 1, plot.Bottom, plot.Top, AxisKind.Linear,
                (int)Math.Clamp(plot.Height / 42, 3, 10), (0, 1));

            yTicks = AxisFactory.ToTicks(ys, yValues, v => NumberFormat.Percent(v, 0));

            var (xs, xValues) = AxisFactory.Numeric(xd?.Min ?? 0, xd?.Max ?? maxTime, plot.Left, plot.Right,
                AxisKind.Linear,
                (int)Math.Clamp(plot.Width / 80, 3, 12), xd);

            var xTicks = AxisFactory.ToTicks(xs, xValues, s.XFormat);
            var marks = curves.Sum(c => (long)Math.Min(c.Steps.Count, ChartLimits.InteractivePointsPerSeries));
            canvas.LimitInteractive(marks);
            canvas.Begin();
            canvas.YAxis(yTicks);
            canvas.XAxis(xTicks, true);
            canvas.Baseline(plot.Bottom);

            var noData = curves.All(c => c.Count == 0);

            for (var gi = 0; gi < curves.Length; gi++)
            {
                DrawGroup(canvas, s, curves[gi], gi, names[gi], xs, ys, plot);
            }

            if (showTable)
            {
                var top = plot.Bottom + 34;
                canvas.W.Element("text", "Number at risk", ("class", "rk"), ("x", Num.F(plot.Left)), ("y", Num.F(top)));

                for (var gi = 0; gi < curves.Length; gi++)
                {
                    var y = top + 6 + gi * RowHeight;

                    canvas.W.Open("g", ("class", "lg s" + gi % ChartLimits.MaxSeries), ("role", "group"),
                        ("aria-label", XmlText.Clean("Number at risk, " + names[gi], 200)));

                    canvas.W.Empty("rect", ("class", "hit"), ("x", Num.F(ChartCanvas.Pad)), ("y", Num.F(y)),
                        ("width", Num.F(plot.Right - ChartCanvas.Pad + 4)), ("height", Num.F(RowHeight - 1)));

                    canvas.W.Empty("rect", ("class", "sw"), ("x", Num.F(ChartCanvas.Pad + 2)), ("y", Num.F(y + 3)),
                        ("width", "10"), ("height", "10"), ("rx", "2"));

                    foreach (var tick in xTicks)
                    {
                        if (tick.Position < plot.Left - 1 || tick.Position > plot.Right + 1)
                        {
                            continue;
                        }

                        canvas.W.Element("text", NumberFormat.Value(curves[gi].AtRiskAt(tick.Value)),
                            ("class", "tk ctr"), ("x", Num.F(tick.Position)), ("y", Num.F(y + 12)));
                    }

                    canvas.W.Close();
                }
            }

            if (logRank is not null)
            {
                canvas.W.Element("text", "Log-rank p " + PText(logRank.PValue), ("class", "tk"),
                    ("x", Num.F(plot.Left + 8)),
                    ("y", Num.F(plot.Bottom - 8)));
            }

            if (noData)
            {
                canvas.Text(plot.CenterX, plot.CenterY, "No data", "tk ctr");
            }

            canvas.AxisTitles(20 + strip);

            return canvas.End();
        }

        private static string PText(double p)
        {
            return p < 0.001 ? "< 0.001" : "= " + NumberFormat.Value(Math.Round(p, 3));
        }

        private void DrawGroup(ChartCanvas canvas, SurvivalChartSpec s, SurvivalCurve curve, int gi, string name,
            IScale xs, IScale ys, Rect plot)
        {
            double V(double v)
            {
                return s.Cumulative ? 1 - v : v;
            }

            var steps = curve.Steps;
            var endTime = Math.Max(curve.MaxTime, steps.Count > 0 ? steps[^1].Time : 0);
            var px = new List<double>();
            var idx = new List<int>();

            for (var i = 0; i < steps.Count; i++)
            {
                var x = xs.Map(steps[i].Time);

                if (px.Count > 0 && x - px[^1] < 0.4 && i < steps.Count - 1)
                {
                    px[^1] = x;
                    idx[^1] = i;
                    continue;
                }

                px.Add(x);
                idx.Add(i);
            }

            var xEnd = Math.Min(xs.Map(endTime), plot.Right);

            double Next(int k)
            {
                return k + 1 < px.Count ? px[k + 1] : Math.Max(xEnd, px[k]);
            }

            canvas.OpenSeries(gi);

            if (s.Bands && steps.Count > 0 && steps.Any(t => t.Upper > t.Lower))
            {
                var band = new PathBuilder();

                for (var k = 0; k < px.Count; k++)
                {
                    var y = ys.Map(V(s.Cumulative ? steps[idx[k]].Lower : steps[idx[k]].Upper));
                    band.Line(px[k], y).Line(Next(k), y);
                }

                for (var k = px.Count - 1; k >= 0; k--)
                {
                    var y = ys.Map(V(s.Cumulative ? steps[idx[k]].Upper : steps[idx[k]].Lower));
                    band.Line(Next(k), y).Line(px[k], y);
                }

                canvas.W.Empty("path", ("class", "cib"), ("d", band + "Z"));
            }

            var line = new PathBuilder();

            for (var k = 0; k < px.Count; k++)
            {
                var y = ys.Map(V(steps[idx[k]].Survival));
                line.Line(px[k], y);
                line.Line(Next(k), y);
            }

            if (!line.IsEmpty)
            {
                canvas.W.Open("path", ("class", "ln"), ("d", line.ToString()));
                canvas.W.Element("title", name);
                canvas.W.Close();
                canvas.W.Empty("path", ("class", "hl"), ("d", line.ToString()));
            }

            if (s.CensorMarks && curve.CensorTimes.Count > 0)
            {
                var ticks = new PathBuilder();

                foreach (var t in MoreKit.Thin(curve.CensorTimes, 1500))
                {
                    var x = xs.Map(t);

                    if (x < plot.Left - 0.5 || x > plot.Right + 0.5)
                    {
                        continue;
                    }

                    var y = ys.Map(V(curve.StepAt(t).Survival));
                    ticks.Move(x, y - 4).Line(x, y + 4);
                }

                if (!ticks.IsEmpty)
                {
                    canvas.W.Empty("path", ("class", "ck"), ("d", ticks.ToString()));
                }
            }

            if (s.MedianMarkers && curve.Median is { } median)
            {
                var mx = xs.Map(median);
                var my = ys.Map(0.5);

                if (mx >= plot.Left && mx <= plot.Right)
                {
                    canvas.W.Open("path", ("class", "med"),
                        ("d", new PathBuilder().Move(plot.Left, my).Line(mx, my).Line(mx, plot.Bottom).ToString()));

                    canvas.W.Element("title", name + " median " + NumberFormat.Value(median));
                    canvas.W.Close();
                }
            }

            if (canvas.Interactive)
            {
                var candidates = steps.Where(t => t.Time > 0).ToList();
                var perPercent = s.Cumulative ? "Cumulative incidence" : "Survival";

                foreach (var step in MoreKit.Thin(candidates, ChartLimits.InteractivePointsPerSeries))
                {
                    var x = xs.Map(step.Time);

                    if (x < plot.Left - 0.5 || x > plot.Right + 0.5)
                    {
                        continue;
                    }

                    var y = ys.Map(V(step.Survival));

                    var rows = new List<TipRow>
                    {
                        new(name, true, gi),
                        new("Time: " + (s.XFormat is null ? NumberFormat.Value(step.Time) : s.XFormat(step.Time))),
                        new(perPercent + ": " + NumberFormat.Percent(V(step.Survival)))
                    };

                    if (step.Upper > step.Lower)
                    {
                        var lo = s.Cumulative ? 1 - step.Upper : step.Lower;
                        var hi = s.Cumulative ? 1 - step.Lower : step.Upper;

                        rows.Add(new TipRow(NumberFormat.Value(Math.Round(curve.Confidence * 100, 1)) + "% CI: " +
                                            NumberFormat.Percent(lo) + " to " + NumberFormat.Percent(hi)));
                    }

                    rows.Add(new TipRow("At risk: " + step.AtRisk + ", events: " + step.Events + ", censored: " +
                                        step.Censored));

                    canvas.OpenMark(gi, string.Join(", ", rows.Select(r => r.Text)));
                    canvas.W.Empty("circle", ("class", "hit"), ("cx", Num.F(x)), ("cy", Num.F(y)), ("r", "9"));
                    canvas.W.Empty("circle", ("class", "pt h"), ("cx", Num.F(x)), ("cy", Num.F(y)), ("r", "4.5"));
                    canvas.Tooltip(x, y - 5, rows);
                    canvas.CloseMark();
                }
            }

            canvas.W.Close();
        }
    }
}