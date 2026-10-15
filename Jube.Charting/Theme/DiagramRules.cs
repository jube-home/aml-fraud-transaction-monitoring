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

namespace Jube.Charting.Theme
{
    internal static class DiagramRules
    {
        public static string Tip(string k, string key)
        {
            return new CssRules(k).Has(".mk." + key + ":hover,.mk." + key + ":focus-visible",
                ".tp.t" + key + " .tt", "opacity:1;visibility:visible").ToString();
        }

        public static string NodeEmphasis(string k)
        {
            var r = new CssRules(k);

            for (var i = 0; i < 9; i++)
            {
                r.Rule(".nt.nq" + i, "fill:var(--jc-q" + i + ");fill-opacity:.92");
            }

            return r.ToString();
        }

        public static string NodeHover(string k, int i)
        {
            var r = new CssRules(k);
            var c = ".nd.n" + i + ":hover,.nd.n" + i + ":focus-visible";
            r.Has(c, ".nd:not(.n" + i + "):not(.j" + i + ")", "opacity:.22");
            r.Has(c, ".eg:not(.e" + i + ")", "opacity:.07");
            r.Has(c, ".eg.e" + i + " .ep", "stroke-opacity:1");

            return r.ToString();
        }

        public static string TreeHover(string k, int g, string tr)
        {
            var r = new CssRules(k);
            var c = ".nd.n" + g + ":hover,.nd.n" + g + ":focus-visible";
            var t = k + ":has(" + c + ") ";
            r.Has(c, ".nd." + tr + ":not(.n" + g + "):not(.j" + g + "):not(.a" + g + ")", "opacity:.22");
            r.Has(c, ".eg." + tr + ":not(.e" + g + "):not(.a" + g + ")", "opacity:.07");
            r.Raw(t + ".eg.e" + g + " .ep," + t + ".eg.a" + g + " .ep{stroke-opacity:1}");

            return r.ToString();
        }

        public static string TreeHoverLocal(string k, int i)
        {
            var scope = k + " .tree";
            var r = new CssRules(scope);
            var c = ".nd.n" + i + ":hover,.nd.n" + i + ":focus-visible";
            r.Has(c, ".nd:not(.n" + i + "):not(.j" + i + "):not(.a" + i + ")", "opacity:.22");
            r.Has(c, ".eg:not(.e" + i + "):not(.a" + i + ")", "opacity:.07");

            r.Raw(scope + ":has(" + c + ") .eg.e" + i + " .ep," + scope + ":has(" + c + ") .eg.a" + i +
                  " .ep{stroke-opacity:1}");

            return r.ToString();
        }

        public static string RowLink(string k)
        {
            return new CssRules(k).Has(".rw:hover", ".rw:not(:hover)", "opacity:.55").ToString();
        }

        public static string RowTransition(string k)
        {
            return new CssRules(k).Rule(".rw", "transition:opacity .15s ease").ToString();
        }

        public static string ComponentBar(string k, int j)
        {
            return new CssRules(k).Has(".pcb.c" + j + ":hover,.pcb.c" + j + ":focus-visible", ".hc:not(.cc" + j + ")",
                    "opacity:.3")
                .ToString();
        }

        public static string ComponentCell(string k, int r, int j)
        {
            var hover = ".hc.rr" + r + ".cc" + j;
            var c = hover + ":hover," + hover + ":focus-visible";

            return new CssRules(k).Has(c, ".hc:not(.rr" + r + "):not(.cc" + j + ")", "opacity:.35")
                .Has(c, ".pcb:not(.c" + j + ")", "opacity:.4").ToString();
        }

        public static string ComponentRow(string k, int r)
        {
            return new CssRules(k).Has(".hc.rr" + r + ":hover,.hc.rr" + r + ":focus-visible", ".hc:not(.rr" + r + ")",
                    "opacity:.4")
                .ToString();
        }

        public static string HcTransition(string k)
        {
            return new CssRules(k).Rule(".hc", "transition:opacity .15s ease").ToString();
        }

        public static string PcbTransition(string k)
        {
            return new CssRules(k).Rule(".pcb", "transition:opacity .15s ease").ToString();
        }

        public static string FuzzyVariable(string k, int v)
        {
            return new CssRules(k).Has(".pv" + v + " .fz:hover", ".pv" + v + " .fz:not(:hover)", "opacity:.28")
                .ToString();
        }

        public static string FuzzyRule(string k, int r)
        {
            return new CssRules(k).Has(".rl.q" + r + ":hover,.rl.q" + r + ":focus-visible", ".fz:not(.u" + r + ")",
                    "opacity:.22")
                .ToString();
        }

        public static void SankeyNode(CssRules css, int i)
        {
            var c = ".nd.n" + i + ":hover,.nd.n" + i + ":focus-visible";
            css.Has(c, ".lk:not(.m" + i + ") .lkp", "fill-opacity:.05");
            css.Has(c, ".lk.m" + i + " .lkp", "fill-opacity:.62");
            css.Has(c, ".nd:not(.a" + i + ") .bar", "opacity:.3");
            css.Has(c, ".nd.a" + i + " .bar", "opacity:1");
        }

        public static void SankeyLink(CssRules css, int j)
        {
            var c = ".lk.l" + j + ":hover,.lk.l" + j + ":focus-visible";
            css.Has(c, ".lk:not(.k" + j + ") .lkp", "fill-opacity:.05");
            css.Has(c, ".lk.k" + j + " .lkp", "fill-opacity:.62");
        }

        public static string MatrixRow(string k, int r)
        {
            return new CssRules(k).Has(".mk.r" + r + ":hover", ".mk.r" + r + " .pt",
                "transform:scale(2);stroke:var(--jc-text);stroke-width:1.5;opacity:1").ToString();
        }
    }
}