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
using Jube.Charting.Core.Models;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Interfaces;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme;
using Jube.Charting.Ticks;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Diagrams
{
    public sealed class CoefficientDiagram(CoefficientDiagramSpec spec) : IStyleModeChart
    {
        public CoefficientDiagramSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new CoefficientDiagram(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var s = Spec;
            var rows = Rows(out var total);
            var name = TextMetrics.Truncate(s.EstimateName, 200, 12);
            var hasCi = rows.Any(r => r.Low.HasValue);
            var hasP = rows.Any(r => r.PValue.HasValue);
            var showOr = s is { ShowOddsRatios: true, LogScale: false };
            var confidence = Num.F(s.ConfidenceLevel * 100) + "%";
            var estimateHeader = name + (hasCi ? " (" + confidence + " CI)" : string.Empty);

            string Fmt(CoefficientRow r)
            {
                return NumberFormat.Value(r.Estimate) + (r.Low.HasValue
                    ? " [" + NumberFormat.Value(r.Low.Value) + ", " + NumberFormat.Value(r.High!.Value) + "]"
                    : string.Empty);
            }

            string OrText(CoefficientRow r)
            {
                return NumberFormat.Value(Math.Exp(r.Estimate)) + (r.Low.HasValue
                    ? " [" + NumberFormat.Value(Math.Exp(r.Low.Value)) + ", " +
                      NumberFormat.Value(Math.Exp(r.High!.Value)) + "]"
                    : string.Empty);
            }

            var labelW = Math.Min(TextMetrics.MaxWidth(rows.Select(r => r.Name), 12), 220);

            var estimateW = Math.Max(TextMetrics.MaxWidth(rows.Select(Fmt), 11),
                TextMetrics.Width(estimateHeader, 11, true)) + 8;

            var pW = hasP ? 62.0 : 0;

            var orW = showOr
                ? Math.Max(TextMetrics.MaxWidth(rows.Select(OrText), 11), TextMetrics.Width("Odds ratio", 11, true)) + 8
                : 0;

            var columns = estimateW + pW + orW + 12 * (1 + (hasP ? 1 : 0) + (showOr ? 1 : 0));
            var plotW = Math.Max(260, s.Options.Width - 2 * ChartCanvas.Pad - 8 - labelW - 12 - columns);
            var contentW = labelW + 12 + plotW + columns;
            var header = 24.0;
            var axisH = 44.0;
            var contentH = header + rows.Count * RowHeight + axisH;
            var notes = new List<string>();

            if (rows.Any(r => !r.Significant))
            {
                notes.Add("hollow markers are not significant" + (hasP
                    ? " at p < " + NumberFormat.Value(s.SignificanceLevel)
                    : hasCi
                        ? " (interval includes " + (s.LogScale ? "1" : "0") + ")"
                        : string.Empty));
            }

            if (total > rows.Count)
            {
                notes.Add("showing " + rows.Count + " of " + total + " coefficients");
            }

            var note = string.Join("; ", notes);
            var legend = new[] { new LegendEntry("Negative", 0), new LegendEntry("Positive", 7) };

            var summary = "Coefficient forest plot of " + rows.Count + " coefficients" +
                          (s.LogScale ? " on a log scale" : string.Empty) +
                          "; largest: " + TextMetrics.Truncate(rows[0].Name, 100, 12) + " at " +
                          NumberFormat.Value(rows[0].Estimate) + ".";

            var fit = DiagramFrame.Fit(s.Options, summary, legend, contentW, contentH, note.Length > 0 ? 24 : 0,
                "coefficient diagram");

            return ChartRun.Execute(fit.Options, id =>
            {
                var scene = DiagramFrame.Begin(fit, id, summary, legend, rows.Count);
                var w = scene.W;
                var reference = s.LogScale ? 1.0 : 0.0;
                var extent = new Extent();
                extent.Add(reference);

                foreach (var r in rows)
                {
                    extent.Add(r.Low ?? r.Estimate);
                    extent.Add(r.High ?? r.Estimate);
                    extent.Add(r.Estimate);
                }

                var x0 = labelW + 12;

                var (scale, tickValues) = AxisFactory.Numeric(extent.Min, extent.Max, x0, x0 + plotW,
                    s.LogScale ? AxisKind.Log : AxisKind.Linear, 6);

                var ticks = AxisFactory.ToTicks(scale, tickValues, null);
                scene.OpenGroup(fit.X0, fit.Y0);
                var top = header;

                w.Element("text", TextMetrics.Truncate(estimateHeader, plotW + estimateW, 11, true), ("class", "key"),
                    ("x", Num.F(x0 + plotW + 12)), ("y", "14"));

                var cursor = x0 + plotW + 12 + estimateW + 12;

                if (hasP)
                {
                    w.Element("text", "p-value", ("class", "key"), ("x", Num.F(cursor)), ("y", "14"));
                    cursor += pW + 12;
                }

                if (showOr)
                {
                    w.Element("text", "Odds ratio", ("class", "key"), ("x", Num.F(cursor)), ("y", "14"));
                }

                w.Element("text", "Predictor", ("class", "key end"), ("x", Num.F(labelW)), ("y", "14"));

                foreach (var t in ticks.Where(t => double.IsFinite(t.Position)))
                {
                    w.Empty("line", ("class", "gl"), ("x1", Num.F(t.Position)), ("y1", Num.F(top)),
                        ("x2", Num.F(t.Position)),
                        ("y2", Num.F(top + rows.Count * RowHeight)));
                }

                for (var i = 0; i < rows.Count; i += 2)
                {
                    w.Empty("rect", ("class", "sh"), ("x", "0"), ("y", Num.F(top + i * RowHeight)),
                        ("width", Num.F(contentW)), ("height", Num.F(RowHeight)));
                }

                var refX = scale.Map(reference);

                w.Empty("line", ("class", "ax dsh"), ("x1", Num.F(refX)), ("y1", Num.F(top)), ("x2", Num.F(refX)),
                    ("y2", Num.F(top + rows.Count * RowHeight)));

                for (var i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    var y = top + i * RowHeight + RowHeight / 2;
                    var series = r.Positive ? 7 : 0;

                    var trows = new List<TipRow>
                    {
                        new(TextMetrics.Truncate(r.Name, 240, 12, true), true),
                        new(name + ": " + NumberFormat.Value(r.Estimate), false, series)
                    };

                    if (r.Low.HasValue)
                    {
                        trows.Add(new TipRow(confidence + " CI: " + NumberFormat.Value(r.Low.Value) + " to " +
                                             NumberFormat.Value(r.High!.Value)));
                    }

                    if (showOr)
                    {
                        trows.Add(new TipRow("Odds ratio: " + OrText(r)));
                    }

                    if (r.PValue.HasValue)
                    {
                        trows.Add(new TipRow("p-value: " + PText(r.PValue.Value)));
                    }

                    trows.Add(new TipRow(r.Significant ? "Significant" : "Not significant"));
                    var key = "r" + i;
                    scene.OpenMark(key, string.Join(", ", trows.Select(t => t.Text)), "rw s" + series);

                    w.Empty("rect", ("class", "hl"), ("x", "0"), ("y", Num.F(top + i * RowHeight)),
                        ("width", Num.F(contentW)),
                        ("height", Num.F(RowHeight)));

                    w.Element("text", TextMetrics.Truncate(r.Name, labelW, 12), ("class", "end mid"),
                        ("x", Num.F(labelW)),
                        ("y", Num.F(y)));

                    var px = scale.Map(r.Estimate);

                    if (r.Low.HasValue)
                    {
                        var lx = scale.Map(r.Low.Value);
                        var hx = scale.Map(r.High!.Value);

                        w.Empty("path", ("class", "ci"),
                            ("d",
                                new PathBuilder().Move(lx, y).Line(hx, y).Move(lx, y - 4).Line(lx, y + 4)
                                    .Move(hx, y - 4)
                                    .Line(hx, y + 4).ToString()));
                    }
                    else
                    {
                        w.Empty("path", ("class", "ci"), ("d", new PathBuilder().Move(refX, y).Line(px, y).ToString()));
                    }

                    var d = new PathBuilder().Move(px, y - 5.5).Line(px + 5.5, y).Line(px, y + 5.5).Line(px - 5.5, y)
                        .Close();

                    w.Empty("path", ("class", r.Significant ? "dmd" : "dmd hollow"), ("d", d.ToString()));
                    w.Element("text", Fmt(r), ("class", "tk mid"), ("x", Num.F(x0 + plotW + 12)), ("y", Num.F(y)));
                    var col = x0 + plotW + 12 + estimateW + 12;

                    if (hasP)
                    {
                        w.Element("text", r.PValue.HasValue ? PText(r.PValue.Value) : string.Empty, ("class", "tk mid"),
                            ("x", Num.F(col)), ("y", Num.F(y)));

                        col += pW + 12;
                    }

                    if (showOr)
                    {
                        w.Element("text", OrText(r), ("class", "tk mid"), ("x", Num.F(col)), ("y", Num.F(y)));
                    }

                    scene.EndMark(key, fit.X0 + px, fit.Y0 + y, trows);
                }

                var bottom = top + rows.Count * RowHeight;

                w.Empty("line", ("class", "ax"), ("x1", Num.F(x0)), ("y1", Num.F(bottom)), ("x2", Num.F(x0 + plotW)),
                    ("y2", Num.F(bottom)));

                var lastEnd = double.NegativeInfinity;

                foreach (var t in ticks.Where(t => double.IsFinite(t.Position)))
                {
                    var half = TextMetrics.Width(t.Label, 11) / 2;

                    w.Empty("line", ("class", "ax"), ("x1", Num.F(t.Position)), ("y1", Num.F(bottom)),
                        ("x2", Num.F(t.Position)),
                        ("y2", Num.F(bottom + 4)));

                    if (t.Position - half < lastEnd + 6)
                    {
                        continue;
                    }

                    lastEnd = t.Position + half;

                    w.Element("text", t.Label, ("class", "tk ctr"), ("x", Num.F(t.Position)),
                        ("y", Num.F(bottom + 18)));
                }

                w.Element("text", name + (s.LogScale ? " (log scale)" : string.Empty), ("class", "key ctr"),
                    ("x", Num.F(x0 + plotW / 2)), ("y", Num.F(bottom + 36)));

                scene.CloseGroup();

                if (note.Length > 0)
                {
                    w.Element("text", note, ("class", "kn"), ("x", Num.F(fit.X0)),
                        ("y", Num.F(fit.Y0 + contentH + 16)));
                }

                if (scene.Interactive)
                {
                    scene.Rules(DiagramRules.RowLink(scene.K));
                    scene.Rules(DiagramRules.RowTransition(scene.K));
                }

                return scene.End();
            });
        }

        public IReadOnlyList<CoefficientRow> Rows(out int total)
        {
            var s = Spec;

            if (s.Items.Count == 0)
            {
                throw new ChartSpecificationException("A coefficient diagram needs at least one coefficient.");
            }

            if (s.Items.Count > DiagramLimits.MaxCoefficientInput)
            {
                throw new DiagramTooLargeException("coefficients", s.Items.Count, DiagramLimits.MaxCoefficientInput,
                    "Select the most influential coefficients before drawing.");
            }

            if (s.MaxRows < 1 || s.MaxRows > DiagramLimits.MaxCoefficientRows)
            {
                throw new ChartSpecificationException("MaxRows must be between 1 and " +
                                                      DiagramLimits.MaxCoefficientRows +
                                                      ".");
            }

            var z = s.ConfidenceLevel switch
            {
                0.90 => 1.644854,
                0.95 => 1.959964,
                0.99 => 2.575829,
                _ => throw new ChartSpecificationException("ConfidenceLevel must be 0.90, 0.95 or 0.99.")
            };

            var reference = s.LogScale ? 1.0 : 0.0;
            var rows = new List<CoefficientRow>();

            foreach (var item in s.Items)
            {
                if (!double.IsFinite(item.Estimate) || (s.LogScale && !(item.Estimate > 0)))
                {
                    throw new ChartSpecificationException("Estimates must be finite" +
                                                          (s.LogScale ? " and positive on a log scale." : "."));
                }

                var low = item.Low;
                var high = item.High;

                if (low is null && high is null && item.StandardError is { } se)
                {
                    if (!(se >= 0) || !double.IsFinite(se))
                    {
                        throw new ChartSpecificationException("Standard errors must be finite and not negative.");
                    }

                    if (s.LogScale)
                    {
                        low = item.Estimate * Math.Exp(-z * se);
                        high = item.Estimate * Math.Exp(z * se);
                    }
                    else
                    {
                        low = item.Estimate - z * se;
                        high = item.Estimate + z * se;
                    }
                }

                if (low.HasValue != high.HasValue)
                {
                    throw new ChartSpecificationException(
                        "A confidence interval needs both its lower and upper bound.");
                }

                if (low is { } l && high is { } h &&
                    (!double.IsFinite(l) || !double.IsFinite(h) || l > item.Estimate || h < item.Estimate ||
                     (s.LogScale && !(l > 0))))
                {
                    throw new ChartSpecificationException(
                        "A confidence interval must be finite, ordered and contain its estimate.");
                }

                if (item.PValue is { } p && !(p is >= 0 and <= 1))
                {
                    throw new ChartSpecificationException("P-values must be between 0 and 1.");
                }

                var significant = item.PValue is { } pv
                    ? pv < s.SignificanceLevel
                    : low is not { } lo || high is not { } hi || lo > reference || hi < reference;

                rows.Add(new CoefficientRow(item.Name, item.Estimate, low, high, item.PValue, significant,
                    item.Estimate > reference));
            }

            total = rows.Count;

            double Magnitude(CoefficientRow r)
            {
                return s.LogScale ? Math.Abs(Math.Log(r.Estimate)) : Math.Abs(r.Estimate);
            }

            var ordered = s.Sort switch
            {
                CoefficientSort.Magnitude => rows.Select((r, i) => (r, i)).OrderByDescending(t => Magnitude(t.r))
                    .ThenBy(t => t.i).Select(t => t.r),
                CoefficientSort.Estimate => rows.Select((r, i) => (r, i)).OrderByDescending(t => t.r.Estimate)
                    .ThenBy(t => t.i).Select(t => t.r),
                CoefficientSort.PValue => rows.Select((r, i) => (r, i)).OrderBy(t => t.r.PValue ?? 1)
                    .ThenByDescending(t => Magnitude(t.r)).ThenBy(t => t.i).Select(t => t.r),
                _ => rows
            };

            return [.. ordered.Take(s.MaxRows)];
        }

        private const double RowHeight = 26;

        private static string PText(double p)
        {
            return p < 0.001 ? "<0.001" : NumberFormat.Value(Math.Round(p, 3));
        }
    }
}