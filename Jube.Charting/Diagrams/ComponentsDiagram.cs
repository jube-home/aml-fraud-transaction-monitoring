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
using Jube.Charting.Scales;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Theme;
using Jube.Charting.Ticks;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Diagrams
{
    public sealed class ComponentsDiagram(ComponentsDiagramSpec spec) : IStyleModeChart
    {
        public ComponentsDiagramSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new ComponentsDiagram(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            var shown = ShownFeatures();

            return Spec.Components.Count > 0 ? RenderPca(shown) : RenderClusters(shown);
        }

        public IReadOnlyList<int> ShownFeatures()
        {
            Validate();
            var s = Spec;
            var limit = s.Components.Count > 0 ? s.MaxFeatureRows : Math.Min(s.MaxFeatureRows, 24);

            var columns = s.Components.Count > 0
                ? s.Components.Take(s.MaxComponentColumns).Select(c => c.Loadings).ToArray()
                : s.Clusters.Select(c => c.Centroid).ToArray();

            var score = Enumerable.Range(0, s.Features.Count).Select(f => columns.Max(c => Math.Abs(c[f]))).ToArray();

            if (s.Features.Count <= limit)
            {
                return [.. Enumerable.Range(0, s.Features.Count)];
            }

            var top = Enumerable.Range(0, s.Features.Count).OrderByDescending(f => score[f]).ThenBy(f => f).Take(limit)
                .ToArray();

            return [.. top.OrderBy(f => f)];
        }

        private void Validate()
        {
            var s = Spec;

            if (s.Components.Count > 0 && s.Clusters.Count > 0)
            {
                throw new DiagramNotSupportedException(
                    "a components diagram shows either principal components or clusters, not both.");
            }

            if (s.Components.Count == 0 && s.Clusters.Count == 0)
            {
                throw new ChartSpecificationException("A components diagram needs components or clusters.");
            }

            if (s.Features.Count == 0)
            {
                throw new ChartSpecificationException("A components diagram needs feature names.");
            }

            if (s.Features.Count > DiagramLimits.MaxFeatures)
            {
                throw new DiagramTooLargeException("features", s.Features.Count, DiagramLimits.MaxFeatures,
                    "Select the most relevant features first.");
            }

            if (s.Components.Count > DiagramLimits.MaxComponents)
            {
                throw new DiagramTooLargeException("components", s.Components.Count, DiagramLimits.MaxComponents,
                    "Keep the leading components only.");
            }

            if (s.Clusters.Count > DiagramLimits.MaxClusters)
            {
                throw new DiagramTooLargeException("clusters", s.Clusters.Count, DiagramLimits.MaxClusters,
                    "Merge or drop the smallest clusters.");
            }

            if (s.MaxFeatureRows < 1 || s.MaxFeatureRows > 60 || s.MaxComponentColumns < 1 ||
                s.MaxComponentColumns > 20)
            {
                throw new ChartSpecificationException(
                    "MaxFeatureRows must be 1 to 60 and MaxComponentColumns 1 to 20.");
            }

            foreach (var c in s.Components)
            {
                if (c.Loadings.Count != s.Features.Count || c.Loadings.Any(v => !double.IsFinite(v)))
                {
                    throw new ChartSpecificationException(c.Name + ": needs one finite loading per feature.");
                }

                if (!(c.ExplainedVariance is >= 0 and <= 1))
                {
                    throw new ChartSpecificationException(c.Name +
                                                          ": explained variance must be a ratio within 0 and 1.");
                }
            }

            if (s.Components.Sum(c => c.ExplainedVariance) > 1.0001)
            {
                throw new ChartSpecificationException("Explained variance ratios must not sum to more than 1.");
            }

            foreach (var c in s.Clusters)
            {
                if (c.Centroid.Count != s.Features.Count || c.Centroid.Any(v => !double.IsFinite(v)))
                {
                    throw new ChartSpecificationException(c.Name + ": needs one finite centroid value per feature.");
                }

                if (!(c.Size >= 0) || !double.IsFinite(c.Size))
                {
                    throw new ChartSpecificationException(c.Name + ": size must be finite and not negative.");
                }
            }
        }

        private string RenderPca(IReadOnlyList<int> features)
        {
            var s = Spec;
            var count = Math.Min(s.Components.Count, s.MaxComponentColumns);
            var comps = s.Components.Take(count).ToArray();
            var cumulative = new double[s.Components.Count];
            var run = 0.0;

            for (var i = 0; i < cumulative.Length; i++)
            {
                run += s.Components[i].ExplainedVariance;
                cumulative[i] = run;
            }

            var scW = Math.Clamp(count * 34 + 70, 260, 460);
            var scH = 260.0;
            var labelW = Math.Min(TextMetrics.MaxWidth(features.Select(f => s.Features[f]), 11), 160);
            var cw = count > 8 ? 34.0 : 46.0;
            var ch = 22.0;
            var header = 46.0;
            var heatW = labelW + 10 + count * cw;
            var heatH = header + features.Count * ch;
            var gap = 28.0;
            var contentW = scW + gap + heatW;
            var contentH = Math.Max(scH, heatH) + 30;
            var maxAbs = Math.Max(comps.Max(c => features.Max(f => Math.Abs(c.Loadings[f]))), 1e-12);
            var notes = new List<string>();

            if (s.Components.Count > count)
            {
                notes.Add("first " + count + " of " + s.Components.Count + " components");
            }

            if (s.Features.Count > features.Count)
            {
                notes.Add("top " + features.Count + " of " + s.Features.Count + " features by loading");
            }

            var note = string.Join("; ", notes);

            var summary = "Principal components: scree plot of " + count + " components explaining " +
                          NumberFormat.Percent(cumulative[count - 1]) +
                          " of the variance with a loadings heatmap of " + features.Count + " features.";

            var fit = DiagramFrame.Fit(s.Options with { Legend = LegendPosition.None }, summary, [], contentW,
                contentH + (note.Length > 0 ? 14 : 0), 0, "components diagram");

            return ChartRun.Execute(fit.Options, id =>
            {
                var scene = DiagramFrame.Begin(fit, id, summary, [], count + count * features.Count);
                var w = scene.W;
                scene.OpenGroup(fit.X0, fit.Y0);
                w.Element("text", "Explained variance", ("class", "ph"), ("x", "0"), ("y", "12"));
                var left = 40.0;
                var right = scW - 6.0;
                var top = 30.0;
                var baseline = scH - 34;
                var ys = new LinearScale(0, 100, baseline, top);

                foreach (var t in new[] { 0, 25, 50, 75, 100 })
                {
                    w.Empty("line", ("class", "gl"), ("x1", Num.F(left)), ("y1", Num.F(ys.Map(t))),
                        ("x2", Num.F(right)),
                        ("y2", Num.F(ys.Map(t))));

                    w.Element("text", t + "%", ("class", "kn end mid"), ("x", Num.F(left - 6)),
                        ("y", Num.F(ys.Map(t))));
                }

                var band = (right - left) / count;
                var barW = Math.Min(band * 0.62, 34);
                var line = new PathBuilder();
                var points = new List<(double X, double Y)>();

                for (var i = 0; i < count; i++)
                {
                    var c = comps[i];
                    var cx = left + (i + 0.5) * band;
                    var pct = c.ExplainedVariance * 100;

                    var top3 = Enumerable.Range(0, s.Features.Count).OrderByDescending(f => Math.Abs(c.Loadings[f]))
                        .ThenBy(f => f).Take(3)
                        .Select(f =>
                            TextMetrics.Truncate(s.Features[f], 90, 12) + " " + NumberFormat.Value(c.Loadings[f]))
                        .ToArray();

                    var rows = new List<TipRow>
                    {
                        new(TextMetrics.Truncate(c.Name, 200, 12, true), true, 0),
                        new("Explained variance: " + NumberFormat.Percent(c.ExplainedVariance)),
                        new("Cumulative: " + NumberFormat.Percent(cumulative[i]))
                    };

                    rows.AddRange(top3.Select((t, k) => new TipRow((k == 0 ? "Top loadings: " : "  ") + t)));
                    var key = "p" + i;

                    scene.OpenMark(key, string.Join(", ", rows.Select(r => r.Text)),
                        "pcb " + (StyleCaps.Keep(scene.External, i, StyleCaps.ComponentColumns)
                            ? "c" + i + " "
                            : string.Empty) + "s0");

                    w.Empty("rect", ("class", "hb"), ("x", Num.F(cx - band / 2)), ("y", Num.F(top)),
                        ("width", Num.F(band)),
                        ("height", Num.F(baseline - top)));

                    w.Empty("rect", ("class", "rb"), ("x", Num.F(cx - barW / 2)), ("y", Num.F(ys.Map(pct))),
                        ("width", Num.F(barW)),
                        ("height", Num.F(Math.Max(0.5, baseline - ys.Map(pct)))), ("rx", "2"));

                    w.Element("text", TextMetrics.Truncate(c.Name, band - 2, 10.5), ("class", "kn ctr"),
                        ("x", Num.F(cx)),
                        ("y", Num.F(baseline + 14)));

                    if (band >= 30)
                    {
                        var inside = baseline - ys.Map(pct) >= 20;

                        w.Element("text", NumberFormat.Percent(c.ExplainedVariance, 0),
                            ("class", inside ? "lbl ctr on" : "kn ctr"), ("x", Num.F(cx)),
                            ("y", Num.F(inside ? ys.Map(pct) + 14 : ys.Map(pct) - 4)));
                    }

                    scene.EndMark(key, fit.X0 + cx, fit.Y0 + ys.Map(pct), rows, true);
                    points.Add((cx, ys.Map(cumulative[i] * 100)));
                }

                foreach (var (x, y) in points)
                {
                    line.Line(x, y);
                }

                w.Empty("path", ("class", "ln s1 thin"), ("d", line.ToString()));

                foreach (var (x, y) in points)
                {
                    w.Empty("circle", ("class", "mkr s1"), ("cx", Num.F(x)), ("cy", Num.F(y)), ("r", "3.5"));
                }

                w.Empty("line", ("class", "ax"), ("x1", Num.F(left)), ("y1", Num.F(baseline)), ("x2", Num.F(right)),
                    ("y2", Num.F(baseline)));

                w.Element("text", "Cumulative", ("class", "key"),
                    ("x", Num.F(right - TextMetrics.Width("Cumulative", 11) - 4)), ("y", Num.F(top - 6)));

                w.Empty("rect", ("class", "cl s1"), ("x", Num.F(right - TextMetrics.Width("Cumulative", 11) - 20)),
                    ("y", Num.F(top - 15)), ("width", "10"), ("height", "3"));

                var hx = scW + gap;
                w.Element("text", "Loadings", ("class", "ph"), ("x", Num.F(hx)), ("y", "12"));
                var gx = hx + labelW + 10;

                for (var j = 0; j < count; j++)
                {
                    w.Element("text", TextMetrics.Truncate(comps[j].Name, cw - 2, 10.5), ("class", "kn ctr"),
                        ("x", Num.F(gx + (j + 0.5) * cw)), ("y", Num.F(header - 6)));
                }

                for (var r = 0; r < features.Count; r++)
                {
                    w.Element("text", TextMetrics.Truncate(s.Features[features[r]], labelW, 11),
                        ("class", "tk end mid"),
                        ("x", Num.F(hx + labelW)),
                        ("y", Num.F(header + (r + 0.5) * ch)));

                    for (var j = 0; j < count; j++)
                    {
                        var v = comps[j].Loadings[features[r]];
                        var norm = comps[j].Loadings.Sum(x => x * x);
                        var bin = Diverge.CellBin(v, maxAbs);

                        var rows = new List<TipRow>
                        {
                            new(
                                TextMetrics.Truncate(s.Features[features[r]], 160, 12, true) + " on " +
                                TextMetrics.Truncate(comps[j].Name, 60, 12), true),
                            new("Loading: " + NumberFormat.Value(v)),
                            new("Share of component: " + NumberFormat.Percent(norm > 0 ? v * v / norm : 0))
                        };

                        var key = "h" + r + "x" + j;

                        scene.OpenMark(key, string.Join(", ", rows.Select(t => t.Text)),
                            "hc " + (StyleCaps.Keep(scene.External, r, StyleCaps.ComponentRows)
                                ? "rr" + r + " "
                                : string.Empty) + (StyleCaps.Keep(scene.External, j, StyleCaps.ComponentColumns)
                                ? "cc" + j
                                : string.Empty));

                        w.Empty("rect", ("class", "cell d" + bin), ("x", Num.F(gx + j * cw)),
                            ("y", Num.F(header + r * ch)),
                            ("width", Num.F(cw)), ("height", Num.F(ch)));

                        if (cw >= 34)
                        {
                            w.Element("text", NumberFormat.Value(Math.Round(v, 2)), ("class", "lbl ctr mid dt" + bin),
                                ("x", Num.F(gx + (j + 0.5) * cw)),
                                ("y", Num.F(header + (r + 0.5) * ch)));
                        }

                        scene.EndMark(key, fit.X0 + gx + (j + 0.5) * cw, fit.Y0 + header + (r + 0.5) * ch, rows, true);
                    }
                }

                var keyY = Math.Max(scH, heatH) + 6;
                Diverge.Key(scene, hx, keyY, maxAbs, "Loading");
                scene.CloseGroup();

                if (note.Length > 0)
                {
                    w.Element("text", note, ("class", "kn"), ("x", Num.F(fit.X0)),
                        ("y", Num.F(fit.Y0 + contentH + 10)));
                }

                if (scene.Interactive)
                {
                    for (var j = 0; j < count; j++)
                    {
                        scene.Rules(DiagramRules.ComponentBar(scene.K, j));
                    }

                    for (var r = 0; r < features.Count; r++)
                    {
                        for (var j = 0; j < count; j++)
                        {
                            scene.Rules(DiagramRules.ComponentCell(scene.K, r, j));
                        }
                    }

                    scene.Rules(DiagramRules.HcTransition(scene.K));
                    scene.Rules(DiagramRules.PcbTransition(scene.K));
                }

                return scene.End();
            });
        }

        private string RenderClusters(IReadOnlyList<int> features)
        {
            var s = Spec;

            var clusters = s.Clusters.Select((c, i) => (c, i)).OrderByDescending(t => t.c.Size).ThenBy(t => t.i)
                .Select(t => t.c).ToArray();

            var n = clusters.Length;
            var cw = features.Count <= 8 ? 50.0 : features.Count <= 14 ? 38.0 : 30.0;
            var ch = 26.0;
            var labelW = Math.Min(TextMetrics.MaxWidth(clusters.Select(c => c.Name), 12), 150);
            var colLabelW = Math.Min(TextMetrics.MaxWidth(features.Select(f => s.Features[f]), 10.5), 120);
            var header = colLabelW * 0.72 + 14;
            var barW = 130.0;
            var total = clusters.Sum(c => c.Size);
            var maxSize = Math.Max(clusters.Max(c => c.Size), 1e-12);
            var gridW = features.Count * cw;
            var contentW = labelW + 10 + gridW + 16 + barW + 70;
            var contentH = header + n * ch + 34;
            var maxAbs = Math.Max(clusters.Max(c => features.Max(f => Math.Abs(c.Centroid[f]))), 1e-12);

            var note = s.Features.Count > features.Count
                ? "showing " + features.Count + " of " + s.Features.Count + " features with the widest spread"
                : string.Empty;

            var summary = "Cluster centroid profile heatmap of " + n + " clusters across " + features.Count +
                          " features with a cluster size bar for each; the largest is " +
                          TextMetrics.Truncate(clusters[0].Name, 80, 12) + " with " +
                          NumberFormat.Value(clusters[0].Size) +
                          " members.";

            var fit = DiagramFrame.Fit(s.Options with { Legend = LegendPosition.None }, summary, [], contentW,
                contentH + (note.Length > 0 ? 14 : 0), 0, "cluster diagram");

            return ChartRun.Execute(fit.Options, id =>
            {
                var scene = DiagramFrame.Begin(fit, id, summary, [], n * (features.Count + 1));
                var w = scene.W;
                scene.OpenGroup(fit.X0, fit.Y0);
                var gx = labelW + 10;

                for (var j = 0; j < features.Count; j++)
                {
                    var x = gx + (j + 0.5) * cw;
                    var label = TextMetrics.Truncate(s.Features[features[j]], colLabelW, 10.5);

                    w.Element("text", label, ("class", "kn"), ("x", Num.F(x)), ("y", Num.F(header - 4)),
                        ("transform", "rotate(-45 " + Num.F(x) + " " + Num.F(header - 4) + ")"));
                }

                var bx = gx + gridW + 16;
                w.Element("text", "Size", ("class", "key"), ("x", Num.F(bx)), ("y", Num.F(header - 6)));

                for (var r = 0; r < n; r++)
                {
                    var c = clusters[r];
                    var y = header + r * ch;

                    w.Element("text", TextMetrics.Truncate(c.Name, labelW, 12), ("class", "end mid"),
                        ("x", Num.F(labelW)),
                        ("y", Num.F(y + ch / 2)));

                    var ranked = features.OrderByDescending(f => Math.Abs(c.Centroid[f])).ThenBy(f => f).Take(3)
                        .ToArray();

                    for (var j = 0; j < features.Count; j++)
                    {
                        var v = c.Centroid[features[j]];
                        var bin = Diverge.CellBin(v, maxAbs);

                        var rows = new List<TipRow>
                        {
                            new(
                                TextMetrics.Truncate(c.Name, 120, 12, true) + " on " +
                                TextMetrics.Truncate(s.Features[features[j]], 120, 12), true),
                            new(TextMetrics.Truncate(s.ValueName, 120, 12) + ": " + NumberFormat.Value(v)),
                            new("Cluster size: " + NumberFormat.Value(c.Size) +
                                (total > 0 ? " (" + NumberFormat.Percent(c.Size / total) + ")" : string.Empty)),
                            new("Defining features: " + string.Join(", ",
                                ranked.Select(f => TextMetrics.Truncate(s.Features[f], 60, 12))))
                        };

                        var key = "h" + r + "x" + j;

                        scene.OpenMark(key, string.Join(", ", rows.Select(t => t.Text)),
                            "hc " + (StyleCaps.Keep(scene.External, r, StyleCaps.ComponentRows)
                                ? "rr" + r
                                : string.Empty));

                        w.Empty("rect", ("class", "cell d" + bin), ("x", Num.F(gx + j * cw)), ("y", Num.F(y)),
                            ("width", Num.F(cw)), ("height", Num.F(ch)));

                        if (cw >= 34)
                        {
                            w.Element("text", NumberFormat.Value(Math.Round(v, 1)), ("class", "lbl ctr mid dt" + bin),
                                ("x", Num.F(gx + (j + 0.5) * cw)), ("y", Num.F(y + ch / 2)));
                        }

                        scene.EndMark(key, fit.X0 + gx + (j + 0.5) * cw, fit.Y0 + y + ch / 2, rows, true);
                    }

                    var length = Math.Max(1, barW * c.Size / maxSize);

                    var sizeRows = new List<TipRow>
                    {
                        new(TextMetrics.Truncate(c.Name, 200, 12, true), true),
                        new("Size: " + NumberFormat.Value(c.Size) +
                            (total > 0 ? " (" + NumberFormat.Percent(c.Size / total) + ")" : string.Empty)),
                        new("Defining features: " + string.Join(", ",
                            ranked.Select(f => TextMetrics.Truncate(s.Features[f], 60, 12))))
                    };

                    var sizeKey = "z" + r;

                    scene.OpenMark(sizeKey, string.Join(", ", sizeRows.Select(t => t.Text)),
                        "hc " +
                        (StyleCaps.Keep(scene.External, r, StyleCaps.ComponentRows) ? "rr" + r + " " : string.Empty) +
                        "s0");

                    w.Empty("rect", ("class", "hb"), ("x", Num.F(bx)), ("y", Num.F(y)), ("width", Num.F(barW + 70)),
                        ("height", Num.F(ch)));

                    w.Empty("rect", ("class", "rb"), ("x", Num.F(bx)), ("y", Num.F(y + 5)), ("width", Num.F(length)),
                        ("height", Num.F(ch - 10)), ("rx", "2"));

                    w.Element("text",
                        NumberFormat.Value(c.Size) +
                        (total > 0 ? " · " + NumberFormat.Percent(c.Size / total, 0) : string.Empty),
                        ("class", "tk mid"),
                        ("x", Num.F(bx + length + 6)),
                        ("y", Num.F(y + ch / 2)));

                    scene.EndMark(sizeKey, fit.X0 + bx + length, fit.Y0 + y + ch / 2, sizeRows, true);

                    if (scene.Interactive)
                    {
                        scene.Rules(DiagramRules.ComponentRow(scene.K, r));
                    }
                }

                Diverge.Key(scene, 0, header + n * ch + 8, maxAbs, TextMetrics.Truncate(s.ValueName, 160, 11));
                scene.CloseGroup();

                if (note.Length > 0)
                {
                    w.Element("text", note, ("class", "kn"), ("x", Num.F(fit.X0)),
                        ("y", Num.F(fit.Y0 + contentH + 10)));
                }

                if (scene.Interactive)
                {
                    scene.Rules(DiagramRules.HcTransition(scene.K));
                }

                return scene.End();
            });
        }
    }
}