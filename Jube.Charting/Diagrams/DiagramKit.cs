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

using System.Text;
using Jube.Charting.Core;
using Jube.Charting.Core.Models;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Svg;
using Jube.Charting.Theme;
using Jube.Charting.Theme.Models;

namespace Jube.Charting.Diagrams
{
    internal sealed class Scene(ChartCanvas canvas)
    {
        private readonly StringBuilder css = new();
        private readonly List<PendingTip> tips = [];
        private double ox;
        private double oy;
        private double scale = 1;

        public ChartCanvas Canvas { get; } = canvas;
        public SvgWriter W => Canvas.W;
        public bool Interactive => Canvas.Interactive;
        public bool External => Canvas.External;
        public string K => Canvas.External ? StyleFamilies.Scope(StyleFamily.Diagram) : "." + Canvas.Id;

        public void Rules(string cssText)
        {
            if (!External)
            {
                css.Append(cssText);
            }
        }

        public void OpenGroup(double originX, double originY, double scaleFactor = 1, string? cls = null)
        {
            ox = originX;
            oy = originY;
            scale = scaleFactor;

            W.Open("g", ("class", cls),
                ("transform",
                    "translate(" + Num.F(originX) + " " + Num.F(originY) + ")" +
                    (Math.Abs(scaleFactor - 1) < 0.0001 ? string.Empty : " scale(" + Num.F(scaleFactor) + ")")));
        }

        public void CloseGroup()
        {
            W.Close();
            ox = 0;
            oy = 0;
            scale = 1;
        }

        public void OpenMark(string key, string label, string classes)
        {
            if (!Interactive)
            {
                W.Open("g");

                return;
            }

            Canvas.OpenMark(-1, label, (External && !StyleCaps.KeyClass(key) ? string.Empty : key + " ") + classes);
        }

        public (string Name, string? Value) StrokeWidth(double width)
        {
            return External ? ("stroke-width", Num.F(width)) : ("style", "stroke-width:" + Num.F(width));
        }

        public void CloseMark()
        {
            W.Close();
        }

        public void EndMark(string key, double x, double y, IReadOnlyList<TipRow> rows, bool right = false)
        {
            if (External && Interactive && rows.Count > 0 && !StyleCaps.TopLayerKey(key))
            {
                var plot = Canvas.Plot;

                var full = new Rect((ChartCanvas.Pad - ox) / scale, (ChartCanvas.Pad - oy) / scale,
                    (Canvas.Width - ChartCanvas.Pad - ox) / scale, (Canvas.Height - ChartCanvas.Pad - oy) / scale);
                Canvas.SetPlot(full);
                Canvas.Tooltip((x - ox) / scale, (y - oy) / scale, rows, right);
                Canvas.SetPlot(plot);
                CloseMark();

                return;
            }

            CloseMark();
            Tip(key, x, y, rows, right);
        }

        public void Tip(string key, double x, double y, IReadOnlyList<TipRow> rows, bool right = false)
        {
            if (!Interactive || rows.Count == 0)
            {
                return;
            }

            tips.Add(new PendingTip(key, x, y, rows, right));
            Rules(DiagramRules.Tip(K, key));
        }

        public string End()
        {
            if (tips.Count > 0)
            {
                Canvas.SetPlot(new Rect(ChartCanvas.Pad, ChartCanvas.Pad, Canvas.Width - ChartCanvas.Pad,
                    Canvas.Height - ChartCanvas.Pad));

                W.Open("g", ("class", "tps"));

                foreach (var tip in tips)
                {
                    W.Open("g", ("class", "tp t" + tip.Key));
                    Canvas.Tooltip(tip.X, tip.Y, tip.Rows, tip.Right);
                    W.Close();
                }

                W.Close();
            }

            var svg = Canvas.End();

            if (External)
            {
                return svg;
            }

            var at = svg.IndexOf("</style>", StringComparison.Ordinal);
            var result = svg[..at] + css + svg[at..];
            ChartLimits.Output(result.Length);

            return result;
        }
    }
}