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
using Jube.Charting.Theme.Models;

namespace Jube.Charting.Theme
{
    internal static class StyleFamilies
    {
        public static readonly StyleFamily[]
            All = [.. Enum.GetValues<StyleFamily>().Where(f => f != StyleFamily.None)];

        public static string Token(StyleFamily family)
        {
            return family switch
            {
                StyleFamily.None => string.Empty,
                StyleFamily.ScatterMatrix => "jc-scatter-matrix",
                _ => "jc-" + family.ToString().ToLowerInvariant()
            };
        }

        public static string Scope(StyleFamily family)
        {
            return ".jc:where(." + Token(family) + ")";
        }

        public static string Rules(StyleFamily family, string k)
        {
            return family switch
            {
                StyleFamily.Bands => Bands(k),
                StyleFamily.Slope => Slope(k),
                StyleFamily.Stream => Stream(k),
                StyleFamily.Survival => Survival(k),
                StyleFamily.Parallel => Parallel(k),
                StyleFamily.Ridgeline => Ridgeline(k),
                StyleFamily.Calendar => Calendar(k),
                StyleFamily.ScatterMatrix => ScatterMatrix(k),
                StyleFamily.Facet => Facet(k),
                StyleFamily.Dot => Dot(k),
                StyleFamily.Violin => Violin(k),
                StyleFamily.Sankey => Sankey(k),
                StyleFamily.Diagram => Diagram(k),
                _ => string.Empty
            };
        }

        public static string Relations(StyleFamily family, string k)
        {
            var b = new StringBuilder();

            switch (family)
            {
                case StyleFamily.Sankey:
                    var sankey = new CssRules(k);

                    for (var i = 0; i < StyleCaps.SankeyNodes; i++)
                    {
                        DiagramRules.SankeyNode(sankey, i);
                    }

                    for (var j = 0; j < StyleCaps.SankeyLinks; j++)
                    {
                        DiagramRules.SankeyLink(sankey, j);
                    }

                    b.Append(sankey);
                    break;
                case StyleFamily.ScatterMatrix:
                    for (var r = 0; r < StyleCaps.MatrixRows; r++)
                    {
                        b.Append(DiagramRules.MatrixRow(k, r));
                    }

                    break;
                case StyleFamily.Diagram:
                    b.Append(DiagramRules.NodeEmphasis(k));

                    foreach (var prefix in new[] { "n", "x" })
                    {
                        for (var i = 0; i < StyleCaps.Nodes; i++)
                        {
                            b.Append(DiagramRules.Tip(k, prefix + i));
                        }
                    }

                    for (var i = 0; i < StyleCaps.Nodes; i++)
                    {
                        b.Append(DiagramRules.NodeHover(k, i));
                    }

                    for (var i = 0; i < StyleCaps.Nodes; i++)
                    {
                        b.Append(DiagramRules.TreeHoverLocal(k, i));
                    }

                    b.Append(DiagramRules.RowLink(k)).Append(DiagramRules.RowTransition(k));

                    for (var j = 0; j < StyleCaps.ComponentColumns; j++)
                    {
                        b.Append(DiagramRules.ComponentBar(k, j));
                    }

                    for (var r = 0; r < StyleCaps.ComponentRows; r++)
                    {
                        for (var j = 0; j < StyleCaps.ComponentColumns; j++)
                        {
                            b.Append(DiagramRules.ComponentCell(k, r, j));
                        }

                        b.Append(DiagramRules.ComponentRow(k, r));
                    }

                    b.Append(DiagramRules.HcTransition(k)).Append(DiagramRules.PcbTransition(k));

                    for (var v = 0; v < StyleCaps.FuzzyVariables; v++)
                    {
                        b.Append(DiagramRules.FuzzyVariable(k, v));
                    }

                    for (var r = 0; r < StyleCaps.FuzzyRules; r++)
                    {
                        b.Append(DiagramRules.FuzzyRule(k, r));
                    }

                    break;
            }

            return b.ToString();
        }

        public static CssRules Common(this CssRules r)
        {
            r.Rule(".cib", "fill:var(--sc);fill-opacity:.16;stroke:none");
            r.Rule(".cib.o", "fill-opacity:.09");
            r.Rule(".ck", "stroke:var(--sc);stroke-width:1.5;fill:none");
            r.Rule(".hl",
                "fill:none;stroke:var(--jc-surface);stroke-opacity:0;stroke-width:12;stroke-linejoin:round;stroke-linecap:round;pointer-events:stroke");
            r.Rule(".dash", "stroke-dasharray:6 4");
            r.Rule(".sr:hover .ln", "stroke-width:3");
            r.Rule(".sr .ln,.sr .cib", "transition:stroke-width .15s ease,fill-opacity .15s ease");
            r.Rule(".sr:hover .cib", "fill-opacity:.28");
            r.Rule(".gt", "fill:none;stroke:var(--jc-text2);stroke-width:1.2");
            r.Rule(".dm", "opacity:.28");

            return r;
        }

        private static string Bands(string k)
        {
            var r = new CssRules(k).Common();
            r.Rule(".ebs", "stroke:var(--sc);stroke-width:1.5;fill:none;stroke-linecap:round");
            r.Rule(".evl", "stroke:var(--jc-text2);stroke-width:1.2;stroke-dasharray:4 3;fill:none");
            r.Rule(".evl.cp", "stroke:var(--jc-text);stroke-width:1.8;stroke-dasharray:1 3;stroke-linecap:round");
            r.Rule(".evd", "fill:var(--jc-text);stroke:var(--jc-surface);stroke-width:1.5");
            r.Rule(".evt",
                "fill:var(--jc-text2);font-size:11px;paint-order:stroke;stroke:var(--jc-surface);stroke-width:3px;stroke-linejoin:round");
            r.Rule(".mk:hover .evl,.mk:focus-visible .evl", "stroke:var(--jc-text);stroke-width:2");
            r.Rule(".rsb", "fill:var(--jc-grid);fill-opacity:.55;stroke:none");
            r.Rule(".rsl", "fill:none;stroke:var(--sc);stroke-width:1.2;stroke-linejoin:round");
            r.Rule(".rsd", "fill:var(--jc-surface);fill-opacity:.62;stroke:none");
            r.Rule(".rsw", "fill:var(--jc-c0);fill-opacity:.16;stroke:var(--jc-text2);stroke-width:1.2");
            r.Rule(".rsh", "fill:var(--jc-surface);stroke:var(--jc-axis);stroke-width:1.5");
            r.Rule(".mk:hover .rsw,.mk:focus-visible .rsw", "fill-opacity:.3");

            return r.ToString();
        }

        private static string Slope(string k)
        {
            var r = new CssRules(k).Common();
            r.Rule(".pt.big", "font-size:inherit");
            r.Rule(".dl",
                "font-size:11px;paint-order:stroke;stroke:var(--jc-surface);stroke-width:3px;stroke-linejoin:round");
            r.Rule(".rk", "font-size:10px;font-weight:700;pointer-events:none");
            r.Rule(".ph", "fill:var(--jc-text2);font-size:12px;font-weight:600");

            return r.ToString();
        }

        private static string Stream(string k)
        {
            var r = new CssRules(k).Common();

            r.Rule(".st",
                "fill:var(--sc);fill-opacity:.85;stroke:var(--jc-surface);stroke-width:.8;stroke-linejoin:round");
            r.Rule(".sr:hover .st", "fill-opacity:1");
            r.Rule(".shl", "fill:var(--jc-text);fill-opacity:0;stroke:none");
            r.Rule(".mk:hover .shl,.mk:focus-visible .shl", "fill-opacity:.22");
            r.Rule(".sbar", "stroke:var(--jc-text2);stroke-width:2;fill:none");

            return r.ToString();
        }

        private static string Survival(string k)
        {
            var r = new CssRules(k).Common();
            r.Rule(".med", "fill:none;stroke:var(--sc);stroke-width:1.2;stroke-dasharray:3 3");
            r.Rule(".rk", "fill:var(--jc-text2);font-size:11px");

            return r.ToString();
        }

        private static string Parallel(string k)
        {
            var r = new CssRules(k);

            r.Rule(".pl",
                "fill:none;stroke:var(--sc);stroke-width:1.3;stroke-opacity:.6;stroke-linejoin:round;stroke-linecap:round;transition:stroke-opacity .15s ease,stroke-width .15s ease");
            r.Rule(".pl.fine", "stroke-width:1;stroke-opacity:.28");
            r.Rule(".hl",
                "fill:none;stroke:var(--jc-surface);stroke-opacity:0;stroke-width:9;stroke-linejoin:round;pointer-events:stroke");
            r.Rule(".mk:hover .pl,.mk:focus-visible .pl", "stroke-width:3;stroke-opacity:1");
            r.Rule(".mk .cx", "opacity:0");
            r.Rule(".mk:hover .cx,.mk:focus-visible .cx", "opacity:1");
            r.Rule(".pcl",
                "fill:var(--jc-muted);font-size:10.5px;paint-order:stroke;stroke:var(--jc-surface);stroke-width:3px;stroke-linejoin:round");
            r.Rule(".pax", "stroke:var(--jc-axis);stroke-width:1.5;fill:none");
            r.Rule(".pah", "fill:var(--jc-text2);font-size:12px;font-weight:600");
            r.Has(".mk:hover", ".mk:not(:hover) .pl", "stroke-opacity:.06");
            r.Has(".mk:focus-visible", ".mk:not(:focus-visible) .pl", "stroke-opacity:.06");
            r.Has(".mk:hover", ".mk:not(:hover) .cx", "opacity:0");

            return r.ToString();
        }

        private static string Ridgeline(string k)
        {
            var r = new CssRules(k);
            r.Rule(".ri", "fill:var(--sc);fill-opacity:.6;stroke:var(--sc);stroke-width:1.6;stroke-linejoin:round");
            r.Rule(".mk:hover .ri,.mk:focus-visible .ri", "fill-opacity:.85;stroke-width:2.5");
            r.Has(".mk:hover", ".mk:not(:hover) .ri", "fill-opacity:.2;stroke-opacity:.5");
            r.Rule(".rb", "stroke:var(--jc-grid);stroke-width:1;fill:none");

            return r.ToString();
        }

        private static string Calendar(string k)
        {
            var r = new CssRules(k);
            r.Rule(".cyr", "fill:var(--jc-text2);font-size:13px;font-weight:600");
            r.Rule(".cmo", "fill:var(--jc-muted);font-size:10.5px");

            return r.ToString();
        }

        private static string ScatterMatrix(string k)
        {
            var r = new CssRules(k);
            r.Rule(".cf", "fill:none;stroke:var(--jc-axis);stroke-width:1");
            r.Rule(".mvn", "fill:var(--jc-text2);font-size:12px;font-weight:600");
            r.Rule(".mtk", "fill:var(--jc-muted);font-size:10px");
            r.Rule(".pt.dot", "stroke:none;fill-opacity:.75");

            return r.ToString();
        }

        private static string Facet(string k)
        {
            var r = new CssRules(k);
            r.Rule(".fst", "fill:var(--jc-grid);fill-opacity:.7;stroke:none");
            r.Rule(".fsb", "fill:none;stroke:var(--jc-grid);stroke-width:1");
            r.Rule(".fsl", "fill:var(--jc-text2);font-size:12px;font-weight:600");

            return r.ToString();
        }

        private static string Dot(string k)
        {
            var r = new CssRules(k);
            r.Rule(".stem", "stroke:var(--sc);stroke-width:2;fill:none;stroke-linecap:round");
            r.Rule(".dbc", "stroke:var(--jc-axis);stroke-width:4;fill:none;stroke-linecap:round");
            r.Rule(".rg", "stroke:var(--jc-grid);stroke-width:1;stroke-dasharray:1 4;fill:none;stroke-linecap:round");
            r.Rule(".rowhl", "fill:var(--jc-grid);fill-opacity:0;stroke:none;transition:fill-opacity .15s ease");
            r.Rule(".mk:hover .rowhl,.mk:focus-visible .rowhl", "fill-opacity:.6");
            r.Rule(".mk:hover .stem,.mk:focus-visible .stem", "stroke-width:3");
            r.Rule(".mk:hover .dbc,.mk:focus-visible .dbc", "stroke:var(--jc-text2)");
            r.Rule(".dl",
                "font-size:11px;paint-order:stroke;stroke:var(--jc-surface);stroke-width:3px;stroke-linejoin:round");

            return r.ToString();
        }

        private static string Violin(string k)
        {
            var r = new CssRules(k);
            r.Rule(".vi", "fill:var(--sc);fill-opacity:.4;stroke:var(--sc);stroke-width:1.5;stroke-linejoin:round");
            r.Rule(".mk:hover .vi,.mk:focus-visible .vi", "fill-opacity:.65;stroke-width:2.5");
            r.Has(".mk:hover", ".mk:not(:hover) .vi", "fill-opacity:.15;stroke-opacity:.4");
            r.Rule(".vbx", "fill:var(--jc-text2);stroke:none");
            r.Rule(".vwk", "stroke:var(--jc-text2);stroke-width:1.5;fill:none");
            r.Rule(".vmd", "fill:var(--jc-surface);stroke:var(--jc-text);stroke-width:1.5");
            r.Rule(".vpt", "fill:var(--sc);fill-opacity:.7;stroke:none");

            return r.ToString();
        }

        private static string Sankey(string k)
        {
            var r = new CssRules(k);
            r.Rule(".lkp", "fill:var(--sc);fill-opacity:.3;stroke:none;transition:fill-opacity .15s ease");
            r.Rule(".mk:hover .lkp,.mk:focus-visible .lkp", "fill-opacity:.7");
            r.Rule(".sk",
                "fill:var(--jc-text);font-size:11px;paint-order:stroke;stroke:var(--jc-surface);stroke-width:3px;stroke-linejoin:round;pointer-events:none");

            r.Has(".lk:hover", ".lk:not(:hover) .lkp", "fill-opacity:.07");
            r.Has(".lk:focus-visible", ".lk:not(:focus-visible) .lkp", "fill-opacity:.07");

            return r.ToString();
        }

        private static string Diagram(string k)
        {
            var r = new CssRules(k, '|');
            r.Rule(".nb", "fill:var(--jc-surface);stroke:var(--sc,var(--jc-axis));stroke-width:2");
            r.Rule(".nt", "fill:var(--sc,var(--jc-muted));fill-opacity:.2;stroke:none");
            r.Rule(".nb.dash", "stroke-dasharray:4 3");
            r.Rule(".nd|.eg|.pn", "transition:opacity .15s ease");
            r.Rule(".mk:hover .nb|.mk:focus-visible .nb", "stroke:var(--jc-text);stroke-width:3.5");
            r.Rule(".ep",
                "fill:none;stroke-linecap:round;stroke-linejoin:round;stroke-opacity:.68;transition:stroke-opacity .15s ease");
            r.Rule(".ep.en", "stroke:var(--jc-axis)");
            r.Rule(".ep.et", "stroke:var(--jc-muted);stroke-opacity:.5");
            r.Rule(".vt", "fill:var(--jc-grid)");
            r.Rule(".nt.lf", "fill-opacity:.38");
            r.Rule(".ep.ed", "stroke-dasharray:5 4");
            r.Rule(".eh", "fill:none;stroke:transparent;stroke-width:12;pointer-events:stroke");
            r.Rule(".eg:hover .ep|.eg:focus-visible .ep", "stroke-opacity:1");
            r.Rule(".ah", "fill:var(--jc-text2);stroke:none");
            r.Rule(".mk:hover .ah|.mk:focus-visible .ah", "fill:var(--jc-text)");
            r.Rule(".hl", "fill:var(--jc-text);fill-opacity:0;stroke:none;transition:fill-opacity .15s ease");
            r.Rule(".mk:hover .hl|.mk:focus-visible .hl", "fill-opacity:.07");
            r.Rule(".hb", "fill:var(--jc-surface);fill-opacity:0;stroke:none");
            r.Rule(".mk:hover .bs|.mk:focus-visible .bs", "stroke:var(--jc-text);stroke-width:2");
            r.Rule(".sh", "fill:var(--jc-text);fill-opacity:.05;stroke:none");
            r.Rule(".key", "font-size:11px;fill:var(--jc-text2)");
            r.Rule(".kn", "font-size:10.5px;fill:var(--jc-muted)");
            r.Rule(".ph", "font-size:12px;font-weight:600");
            r.Rule(".pf", "fill:none;stroke:var(--jc-grid);stroke-width:1");
            r.Rule(".tps", "pointer-events:none");
            r.Rule(".dsh", "stroke-dasharray:3 3");
            r.Rule(".ln.thin", "stroke-width:1.5");
            r.Rule(".ln.hi", "stroke-width:2.5");
            r.Rule(".rb", "fill:var(--sc);fill-opacity:.85;stroke:none");
            r.Rule(".mk:hover .rb|.mk:focus-visible .rb", "fill-opacity:1;stroke:var(--jc-text);stroke-width:1.5");
            r.Rule(".ci", "stroke:var(--sc);stroke-width:2;fill:none;stroke-linecap:round");
            r.Rule(".dmd", "fill:var(--sc);stroke:var(--jc-surface);stroke-width:1.5");
            r.Rule(".dmd.hollow", "fill:var(--jc-surface);stroke:var(--sc);stroke-width:2");
            r.Rule(".mk:hover .dmd|.mk:focus-visible .dmd", "stroke:var(--jc-text);stroke-width:2.5");
            r.Rule(".mk:hover .ci|.mk:focus-visible .ci", "stroke-width:3");
            r.Rule(".tri", "fill:var(--jc-surface);stroke:var(--jc-muted);stroke-width:1.5;stroke-dasharray:3 2");
            r.Rule(".mk:hover .tri|.mk:focus-visible .tri", "stroke:var(--jc-text);stroke-width:2.5");
            r.Rule(".cl", "fill:var(--sc)");
            r.Rule(".db", "fill:var(--sc);fill-opacity:.42;stroke:var(--sc);stroke-width:1");
            r.Rule(".ax.dsh", "stroke-dasharray:3 3");

            for (var i = 0; i < Palette.Light.Diverging.Count; i++)
            {
                r.Rule(".sd" + i, "stroke:var(--jc-d" + i + ")");
            }

            r.Has(".eg:hover", ".eg:not(:hover)", "opacity:.12");
            r.Has(".eg:hover", ".nd", "opacity:.45");

            for (var i = 0; i < 8; i++)
            {
                r.Has(".lg.s" + i + ":hover", ".nd:not(.s" + i + ")", "opacity:.2");
                r.Has(".lg.s" + i + ":hover", ".eg", "opacity:.3");
            }

            return r.ToString();
        }
    }
}