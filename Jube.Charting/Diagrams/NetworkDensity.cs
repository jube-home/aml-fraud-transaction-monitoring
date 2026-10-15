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

using System.Globalization;
using Jube.Charting.Core;
using Jube.Charting.Svg;
using Jube.Charting.Text;
using Jube.Charting.Ticks;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Diagrams
{
    internal static class NetworkDensityRenderer
    {
        public static string Render(NetworkDiagramSpec spec)
        {
            var layers = spec.Layers;

            if (layers.Count < 2)
            {
                throw new ChartSpecificationException("A network diagram needs at least an input and an output layer.");
            }

            if (layers.Count > DiagramLimits.MaxNetworkLayers)
            {
                throw new DiagramTooLargeException("network layers", layers.Count, DiagramLimits.MaxNetworkLayers,
                    "Draw a summary of the network or split it into stages.");
            }

            var totalNodes = layers.Sum(l => (long)l.Nodes.Count);

            if (totalNodes > DiagramLimits.MaxNetworkInputNodes)
            {
                throw new DiagramTooLargeException("network nodes", totalNodes, DiagramLimits.MaxNetworkInputNodes,
                    "Draw a summary of the network.");
            }

            long totalEdges = 0;

            for (var l = 1; l < layers.Count; l++)
            {
                totalEdges += (long)layers[l].Nodes.Count * layers[l - 1].Nodes.Count;
            }

            if (totalEdges > DiagramLimits.MaxNetworkInputEdges)
            {
                throw new DiagramTooLargeException("network connections", totalEdges,
                    DiagramLimits.MaxNetworkInputEdges,
                    "Draw a summary of the network.");
            }

            var weighted = layers.Skip(1).Any(l => l.Weights is not null);
            double globalMax = 0;

            if (weighted)
            {
                foreach (var layer in layers.Skip(1).Where(l => l.Weights is not null))
                {
                    foreach (var row in layer.Weights!)
                    {
                        foreach (var v in row)
                        {
                            if (double.IsFinite(v) && Math.Abs(v) > globalMax)
                            {
                                globalMax = Math.Abs(v);
                            }
                        }
                    }
                }

                if (!(globalMax > 0) &&
                    !layers.Skip(1).Any(l => l.Weights is not null && l.Weights.Any(r => r.Any(double.IsFinite))))
                {
                    throw new DiagramNotSupportedException(
                        "the network has no finite weights, so there is nothing to draw.");
                }
            }

            var threshold = globalMax * Strong;
            var stats = Stats(spec, threshold);
            var pairs = layers.Count - 1;
            var maxBins = Math.Clamp((int)Math.Sqrt(1000.0 / pairs), 4, 16);
            var options = spec.Options;
            var contentWidth = Math.Max(720, options.Width - 2 * ChartCanvas.Pad - 8);

            var perRow = Math.Max(1,
                Math.Min(layers.Count, (int)((contentWidth + BlockGap) / (MinimumBlockWidth + BlockGap))));

            var blockWidth = Math.Min(MaximumBlockWidth, (contentWidth - BlockGap * (perRow - 1)) / perRow);
            var blockRows = (int)Math.Ceiling(layers.Count / (double)perRow);
            var y = 0.0;
            var blocksTop = y;
            y += blockRows * BlockHeight + (blockRows - 1) * BlockGap;
            y += 18;

            var panels = new List<Panel>();
            var sectionTop = y;

            if (weighted)
            {
                y += SectionHeader;
                var x = 0.0;
                var rowHeight = 0.0;

                for (var l = 1; l < layers.Count; l++)
                {
                    if (layers[l].Weights is null)
                    {
                        continue;
                    }

                    var panel = Bin(layers, l, maxBins);

                    if (x > 0 && x + panel.Width > contentWidth)
                    {
                        y += rowHeight + PanelGap;
                        x = 0;
                        rowHeight = 0;
                    }

                    panels.Add(panel with { X = x, Y = y });
                    x += panel.Width + PanelGap;
                    rowHeight = Math.Max(rowHeight, panel.Height);
                }

                y += rowHeight + 14;
            }

            var keyY = y;
            y += 44;
            var contentHeight = y;

            var totalPes = layers.Skip(1).Sum(l => (long)l.Nodes.Count);
            var totalWeights = totalEdges;
            var finiteWeights = stats.Sum(s => s.Finite);
            var above = stats.Sum(s => s.Above);

            var summary = "Neural network density view with " + layers.Count + " layers and " +
                          NumberFormat.Value(totalPes) + " processing elements (neurons)" +
                          (weighted
                              ? " and " + NumberFormat.Value(totalWeights) + " weights; " +
                                NumberFormat.Percent(finiteWeights > 0 ? above / (double)finiteWeights : 0) +
                                " of the weights are at least a tenth of the strongest."
                              : ".");

            var marks = layers.Count + panels.Sum(p => (long)p.RowBins * p.ColumnBins);
            var fit = DiagramFrame.Fit(options, summary, [], contentWidth, contentHeight, 0, "network density view");

            return ChartRun.Execute(fit.Options, id => Draw(spec, fit, id, summary, stats, panels, contentWidth, perRow,
                blockWidth, blocksTop, sectionTop, keyY, weighted, globalMax, threshold, marks, totalPes, totalWeights,
                finiteWeights, above));
        }

        private const double BlockHeight = 100;
        private const double BlockGap = 22;
        private const double MinimumBlockWidth = 150;
        private const double MaximumBlockWidth = 230;
        private const double PanelGap = 26;
        private const double PanelHeader = 40;
        private const double SectionHeader = 26;
        private const double MaximumGrid = 240;
        private const double MinimumCell = 6;
        private const double MaximumCell = 24;
        private const double Strong = 0.1;
        private const int Bins = 9;

        private static string Draw(NetworkDiagramSpec spec, FrameFit fit, string id, string summary,
            IReadOnlyList<LayerStats> stats, IReadOnlyList<Panel> panels, double contentWidth, int perRow,
            double blockWidth, double blocksTop, double sectionTop, double keyY, bool weighted, double globalMax,
            double threshold, long marks, long totalPes, long totalWeights, long finiteWeights, long above)
        {
            var layers = spec.Layers;
            var scene = DiagramFrame.Begin(fit, id, summary, [], marks);
            var w = scene.W;
            scene.OpenGroup(fit.X0, fit.Y0);

            for (var i = 0; i < stats.Count; i++)
            {
                var s = stats[i];
                var col = i % perRow;
                var row = i / perRow;
                var x = col * (blockWidth + BlockGap);
                var y = blocksTop + row * (BlockHeight + BlockGap);

                if (col > 0)
                {
                    w.Empty("path", ("class", "ep en"),
                        ("d", new PathBuilder().Move(x - BlockGap, y + BlockHeight / 2).Line(x, y + BlockHeight / 2)
                            .ToString()));
                }

                var rows = new List<TipRow>
                {
                    new(Clip(s.Name), true),
                    new(NetworkRoleName(spec, i) + " layer with " + NumberFormat.Value(s.Nodes) +
                        (i == 0 ? " inputs" : " processing elements"))
                };

                if (!string.IsNullOrWhiteSpace(s.Activation))
                {
                    rows.Add(new TipRow("Activation: " + Clip(s.Activation!)));
                }

                if (i > 0)
                {
                    rows.Add(new TipRow("Incoming weights: " + NumberFormat.Value(s.Weights)));

                    if (weighted && s.Finite > 0)
                    {
                        rows.Add(new TipRow("Mean magnitude: " + NumberFormat.Value(Math.Round(s.Sum / s.Finite, 4))));
                        rows.Add(new TipRow("Strongest weight: " + NumberFormat.Value(Math.Round(s.Maximum, 4))));

                        rows.Add(new TipRow(NumberFormat.Percent(s.Above / (double)s.Finite) +
                                            " of weights at least a tenth of the strongest"));
                    }

                    if (s is { BiasLow: { } low, BiasHigh: { } high })
                    {
                        rows.Add(new TipRow("Biases: " + NumberFormat.Value(Math.Round(low, 4)) + " to " +
                                            NumberFormat.Value(Math.Round(high, 4))));
                    }
                }

                var label = string.Join(", ", rows.Select(r => r.Text));
                var key = "n" + i;
                scene.OpenMark(key, label, "nd s" + SeriesOf(spec, i));

                w.Empty("rect", ("class", "nb"), ("x", Num.F(x)), ("y", Num.F(y)), ("width", Num.F(blockWidth)),
                    ("height", Num.F(BlockHeight)), ("rx", "8"));
                w.Empty("rect", ("class", "nt"), ("x", Num.F(x)), ("y", Num.F(y)), ("width", Num.F(blockWidth)),
                    ("height", Num.F(BlockHeight)), ("rx", "8"));

                var cx = x + blockWidth / 2;

                w.Element("text", TextMetrics.Truncate(s.Name, blockWidth - 16, 12, true), ("class", "ph ctr"),
                    ("x", Num.F(cx)), ("y", Num.F(y + 20)));

                w.Element("text",
                    NumberFormat.Value(s.Nodes) +
                    (i == 0 ? s.Nodes == 1 ? " input" : " inputs" : s.Nodes == 1 ? " PE" : " PEs"),
                    ("class", "lbl ctr"), ("x", Num.F(cx)), ("y", Num.F(y + 40)));

                if (!string.IsNullOrWhiteSpace(s.Activation))
                {
                    w.Element("text", TextMetrics.Truncate(s.Activation, blockWidth - 16, 10.5),
                        ("class", "kn ctr"), ("x", Num.F(cx)), ("y", Num.F(y + 56)));
                }

                if (i > 0)
                {
                    w.Element("text", NumberFormat.Value(s.Weights) + " weights in", ("class", "kn ctr"),
                        ("x", Num.F(cx)), ("y", Num.F(y + 72)));

                    if (weighted && s.Finite > 0)
                    {
                        w.Element("text",
                            TextMetrics.Truncate(
                                "mean " + NumberFormat.Value(Math.Round(s.Sum / s.Finite, 3)) + ", max " +
                                NumberFormat.Value(Math.Round(s.Maximum, 3)), blockWidth - 16, 10.5),
                            ("class", "kn ctr"), ("x", Num.F(cx)), ("y", Num.F(y + 88)));
                    }
                }

                scene.EndMark(key, fit.X0 + cx, fit.Y0 + y, rows);
            }

            if (weighted)
            {
                w.Element("text", "Weight magnitude between layers, rows are the earlier layer and columns the next",
                    ("class", "ph"), ("x", "0"), ("y", Num.F(sectionTop + 14)));

                var cell = 0;

                foreach (var p in panels)
                {
                    var from = stats[p.Pair - 1];
                    var to = stats[p.Pair];

                    w.Element("text", TextMetrics.Truncate(from.Name + " to " + to.Name, p.Width, 12, true),
                        ("class", "ph"), ("x", Num.F(p.X)), ("y", Num.F(p.Y + 12)));

                    w.Element("text",
                        TextMetrics.Truncate(
                            NumberFormat.Value(from.Nodes) + " x " + NumberFormat.Value(to.Nodes) + " = " +
                            NumberFormat.Value(to.Weights) + " weights", p.Width, 10.5),
                        ("class", "kn"), ("x", Num.F(p.X)), ("y", Num.F(p.Y + 27)));

                    var gx = p.X;
                    var gy = p.Y + PanelHeader;

                    for (var r = 0; r < p.RowBins; r++)
                    {
                        for (var c = 0; c < p.ColumnBins; c++)
                        {
                            var k = r * p.ColumnBins + c;
                            var x = gx + c * p.Cell;
                            var y = gy + r * p.Cell;
                            var rowsTip = new List<TipRow>();
                            var (r0, r1) = Span(from.Nodes, p.RowBins, r);
                            var (c0, c1) = Span(to.Nodes, p.ColumnBins, c);

                            rowsTip.Add(new TipRow(Clip(from.Name) + " " + Range(r0, r1) + " to " + Clip(to.Name) +
                                                   " " +
                                                   Range(c0, c1), true));

                            string? css;

                            if (p.Count[k] == 0)
                            {
                                rowsTip.Add(new TipRow("No finite weights"));
                                css = "cell m na";
                            }
                            else
                            {
                                rowsTip.Add(
                                    new TipRow("Mean magnitude: " + NumberFormat.Value(Math.Round(p.Mean[k], 4))));
                                rowsTip.Add(new TipRow("Strongest weight: " +
                                                       NumberFormat.Value(Math.Round(p.Max[k], 4))));

                                rowsTip.Add(new TipRow("Weights in the cell: " + NumberFormat.Value(p.Count[k])));

                                css = "cell m q" + Math.Clamp((int)(p.Mean[k] / globalMax * (Bins - 1) + 0.5), 0,
                                    Bins - 1);
                            }

                            scene.OpenMark("h" + cell, string.Join(", ", rowsTip.Select(t => t.Text)), string.Empty);

                            w.Empty("rect", ("class", css), ("x", Num.F(x)), ("y", Num.F(y)), ("width", Num.F(p.Cell)),
                                ("height", Num.F(p.Cell)));

                            scene.EndMark("h" + cell, fit.X0 + x + p.Cell / 2, fit.Y0 + y, rowsTip);
                            cell++;
                        }
                    }
                }

                var keyText = "Mean magnitude";
                w.Element("text", keyText, ("class", "key"), ("x", "0"), ("y", Num.F(keyY + 11)));
                var bar = TextMetrics.Width(keyText, 11) + 12;
                w.Element("text", "0", ("class", "kn"), ("x", Num.F(bar)), ("y", Num.F(keyY + 11)));
                var start = bar + TextMetrics.Width("0", 10.5) + 6;

                for (var i = 0; i < Bins; i++)
                {
                    w.Empty("rect", ("class", "cell q" + i), ("x", Num.F(start + i * 16)), ("y", Num.F(keyY)),
                        ("width", "16"), ("height", "14"));
                }

                w.Element("text", NumberFormat.Value(Math.Round(globalMax, 4)), ("class", "kn"),
                    ("x", Num.F(start + Bins * 16 + 6)), ("y", Num.F(keyY + 11)));
            }

            var totals = NumberFormat.Value(totalPes) + " processing elements (neurons) in " + (layers.Count - 1) +
                         " weighted layers, " +
                         NumberFormat.Value(layers[0].Nodes.Count) + " inputs" +
                         (weighted
                             ? ", " + NumberFormat.Value(totalWeights) + " weights; " +
                               NumberFormat.Percent(finiteWeights > 0 ? above / (double)finiteWeights : 0) +
                               " of weights are at least " + NumberFormat.Value(Math.Round(threshold, 4)) +
                               " (a tenth of the strongest)"
                             : string.Empty);

            w.Element("text", TextMetrics.Truncate(totals, contentWidth, 11), ("class", "kn"), ("x", "0"),
                ("y", Num.F(keyY + 34)));

            scene.CloseGroup();

            return scene.End();
        }

        private static string Clip(string text)
        {
            return TextMetrics.Truncate(text, 240, 12);
        }

        private static string Range(int from, int to)
        {
            return to - from <= 1
                ? (from + 1).ToString(CultureInfo.InvariantCulture)
                : (from + 1).ToString(CultureInfo.InvariantCulture) + " to " +
                  to.ToString(CultureInfo.InvariantCulture);
        }

        private static (int From, int To) Span(int nodes, int bins, int bin)
        {
            var from = (int)((long)bin * nodes / bins);
            var to = (int)((long)(bin + 1) * nodes / bins);

            return (from, Math.Max(to, from + 1));
        }

        private static int SeriesOf(NetworkDiagramSpec spec, int layer)
        {
            return NetworkLayout.RoleOf(spec, layer) switch { NetworkRole.Input => 0, NetworkRole.Output => 1, _ => 2 };
        }

        private static string NetworkRoleName(NetworkDiagramSpec spec, int layer)
        {
            return NetworkLayout.RoleOf(spec, layer) switch
            {
                NetworkRole.Input => "Input",
                NetworkRole.Output => "Output",
                _ => "Hidden"
            };
        }

        private static List<LayerStats> Stats(NetworkDiagramSpec spec, double threshold)
        {
            var layers = spec.Layers;
            var result = new List<LayerStats>();
            var hidden = 0;

            for (var l = 0; l < layers.Count; l++)
            {
                var layer = layers[l];
                var role = NetworkLayout.RoleOf(spec, l);

                if (role == NetworkRole.Hidden)
                {
                    hidden++;
                }

                var name = layer.Name ?? (role == NetworkRole.Hidden ? "Hidden " + hidden
                    : role == NetworkRole.Input ? "Input" : "Output");

                long finite = 0;
                long above = 0;
                var sum = 0.0;
                var max = 0.0;

                if (l > 0 && layer.Weights is not null)
                {
                    foreach (var row in layer.Weights)
                    {
                        foreach (var v in row)
                        {
                            if (!double.IsFinite(v))
                            {
                                continue;
                            }

                            var a = Math.Abs(v);
                            finite++;
                            sum += a;

                            if (a > max)
                            {
                                max = a;
                            }

                            if (a >= threshold)
                            {
                                above++;
                            }
                        }
                    }
                }

                var biases = layer.Nodes.Where(n => n.Bias is { } b && double.IsFinite(b)).Select(n => n.Bias!.Value)
                    .ToArray();

                result.Add(new LayerStats(name, layer.Nodes.Count, layer.Activation,
                    l > 0 ? (long)layer.Nodes.Count * layers[l - 1].Nodes.Count : 0, finite, sum, max, above,
                    biases.Length > 0 ? biases.Min() : null, biases.Length > 0 ? biases.Max() : null));
            }

            return result;
        }

        private static Panel Bin(IReadOnlyList<NetworkLayer> layers, int l, int maxBins)
        {
            var rows = layers[l - 1].Nodes.Count;
            var cols = layers[l].Nodes.Count;
            var rb = Math.Min(rows, maxBins);
            var cb = Math.Min(cols, maxBins);
            var sum = new double[rb * cb];
            var max = new double[rb * cb];
            var count = new long[rb * cb];
            var weights = layers[l].Weights!;
            var rowBin = new int[rows];

            for (var r = 0; r < rows; r++)
            {
                rowBin[r] = (int)((long)r * rb / rows);
            }

            for (var c = 0; c < cols; c++)
            {
                var cBin = (int)((long)c * cb / cols);
                var row = weights[c];

                for (var r = 0; r < rows; r++)
                {
                    var v = row[r];

                    if (!double.IsFinite(v))
                    {
                        continue;
                    }

                    var k = rowBin[r] * cb + cBin;
                    var a = Math.Abs(v);
                    sum[k] += a;

                    if (a > max[k])
                    {
                        max[k] = a;
                    }

                    count[k]++;
                }
            }

            var mean = new double[sum.Length];

            for (var k = 0; k < mean.Length; k++)
            {
                mean[k] = count[k] > 0 ? sum[k] / count[k] : 0;
            }

            var cell = Math.Clamp(Math.Floor(MaximumGrid / Math.Max(rb, cb)), MinimumCell, MaximumCell);
            var width = Math.Max(cb * cell, 190);

            return new Panel(l, rb, cb, mean, max, count, cell, width, PanelHeader + rb * cell, 0, 0);
        }

        private sealed record LayerStats(
            string Name,
            int Nodes,
            string? Activation,
            long Weights,
            long Finite,
            double Sum,
            double Maximum,
            long Above,
            double? BiasLow,
            double? BiasHigh);

        private sealed record Panel(
            int Pair,
            int RowBins,
            int ColumnBins,
            double[] Mean,
            double[] Max,
            long[] Count,
            double Cell,
            double Width,
            double Height,
            double X,
            double Y);
    }
}