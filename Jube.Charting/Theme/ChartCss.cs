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
using Jube.Charting.Svg;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Theme
{
    internal static class ChartCss
    {
        private const string FontStack =
            "system-ui,-apple-system,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif";

        private static string Variables(ThemeVariant v)
        {
            var b = new StringBuilder();

            b.Append("--jc-surface:").Append(v.Surface).Append(";--jc-text:").Append(v.Text)
                .Append(";--jc-text2:").Append(v.TextSecondary).Append(";--jc-muted:").Append(v.TextMuted)
                .Append(";--jc-grid:").Append(v.Grid).Append(";--jc-axis:").Append(v.Axis)
                .Append(";--jc-tip:").Append(v.Tooltip).Append(';');

            for (var i = 0; i < v.Categorical.Count; i++)
            {
                b.Append("--jc-c").Append(i).Append(':').Append(v.Categorical[i]).Append(";--jc-ct").Append(i)
                    .Append(':')
                    .Append(v.OnColor(v.Categorical[i])).Append(';');
            }

            for (var i = 0; i < v.Sequential.Count; i++)
            {
                b.Append("--jc-q").Append(i).Append(':').Append(v.Sequential[i]).Append(";--jc-qt").Append(i)
                    .Append(':')
                    .Append(v.OnColor(v.Sequential[i])).Append(';');
            }

            for (var i = 0; i < v.Diverging.Count; i++)
            {
                b.Append("--jc-d").Append(i).Append(':').Append(v.Diverging[i]).Append(";--jc-dt").Append(i).Append(':')
                    .Append(v.OnColor(v.Diverging[i])).Append(';');
            }

            return b.ToString();
        }

        public static string Build(string cls, ChartOptions options, int seriesCount,
            IReadOnlyList<string?> customColours)
        {
            var k = "." + cls;
            var css = new StringBuilder();

            css.Append(k).Append('{');

            if (options.Sizing == ChartSizing.Responsive)
            {
                css.Append("display:block;width:100%;height:auto;max-width:100%;");
            }
            else
            {
                css.Append("display:block;");
            }

            css.Append("color-scheme:light;").Append(Variables(Palette.Light))
                .Append("font-family:").Append(FontStack).Append(";font-size:12px}");

            css.Append("@media (prefers-color-scheme:dark){")
                .Append(":root:not([data-theme=\"light\"]) ").Append(k).Append(":not([data-theme=\"light\"]),")
                .Append(k).Append(":root:not([data-theme=\"light\"]){color-scheme:dark;")
                .Append(Variables(Palette.Dark)).Append("}}");

            css.Append(":root[data-theme=\"dark\"] ").Append(k).Append(":not([data-theme=\"light\"]),")
                .Append(k).Append("[data-theme=\"dark\"]{color-scheme:dark;").Append(Variables(Palette.Dark))
                .Append('}');

            css.Append(Body(k));

            var count = Math.Min(seriesCount, ChartLimits.MaxSeries);
            var series = new CssRules(k);

            for (var i = 0; i < count; i++)
            {
                var custom = i < customColours.Count ? customColours[i] : null;
                var value = custom ?? "var(--jc-c" + i % Palette.CategoricalCount + ")";
                series.Rule(".s" + i, "--sc:" + value);

                var onColour = custom is null
                    ? "var(--jc-ct" + i % Palette.CategoricalCount + ")"
                    : OnCustom(custom);
                series.Rule(".s" + i + " .on", "fill:" + onColour);
                SeriesLinks(series, i);
            }

            css.Append(series);
            css.Append(ReducedMotion(k));

            return css.ToString();
        }

        public static string OnCustom(string custom)
        {
            return Contrast.BestText(Rgb.Parse(custom), Rgb.Parse(Palette.Light.Text), Rgb.Parse(Palette.Dark.Text))
                .Hex;
        }

        public static string ReducedMotion(string k)
        {
            return "@media (prefers-reduced-motion:reduce){" + k + "," + k +
                   " *{transition:none!important;animation:none!important}}";
        }

        public static void SeriesLinks(CssRules r, int i)
        {
            var s = ".s" + i;
            r.Has(".lg" + s + ":hover", ".sr:not(" + s + ")", "opacity:.16");
            r.Has(".sr" + s + ":hover", ".sr:not(" + s + ")", "opacity:.3");
            r.Has(".sr" + s + ":hover", ".lg:not(" + s + ")", "opacity:.4");
            r.Has(".sr" + s + ":hover", ".lg" + s + " .sw", "transform:scale(1.35)");
            r.Has(".lg" + s + ":hover", ".lg" + s + " .sw", "transform:scale(1.35)");
        }

        public static string Body(string k)
        {
            var r = new CssRules(k);
            r.Rule("text", "fill:var(--jc-text);font-size:12px");
            r.Rule(".bg", "fill:var(--jc-surface)");
            r.Rule(".ttl", "font-size:16px;font-weight:600");
            r.Rule(".sub", "fill:var(--jc-text2)");
            r.Rule(".tk", "fill:var(--jc-muted);font-size:11px");
            r.Rule(".at", "fill:var(--jc-text2);font-weight:500");
            r.Rule(".gl", "stroke:var(--jc-grid);stroke-width:1;fill:none");
            r.Rule(".ax", "stroke:var(--jc-axis);stroke-width:1;fill:none");
            r.Rule(".ref", "stroke:var(--jc-muted);stroke-width:1.5;stroke-dasharray:5 4;fill:none");
            r.Rule(".ref.s0,.ref.s1,.ref.s2,.ref.s3,.ref.s4,.ref.s5,.ref.s6,.ref.s7", "stroke:var(--sc)");
            r.Rule(".band", "fill:var(--jc-muted);fill-opacity:.12;stroke:none");
            r.Rule(".and", "fill:var(--jc-text);stroke:var(--jc-surface);stroke-width:1.5");
            r.Rule(".ant",
                "font-size:11px;paint-order:stroke;stroke:var(--jc-surface);stroke-width:3px;stroke-linejoin:round");
            r.Rule(".box", "fill:var(--sc);fill-opacity:.3;stroke:var(--sc);stroke-width:1.5");
            r.Rule(".wh,.wk", "stroke:var(--sc);stroke-width:1.5;fill:none");
            r.Rule(".med", "stroke:var(--jc-text);stroke-width:2.5");
            r.Rule(".out", "fill:none;stroke:var(--sc);stroke-width:1.5");
            r.Rule(".cd", "fill:var(--sc);stroke:var(--sc);stroke-width:1.5");
            r.Rule(".cd.hollow", "fill:var(--jc-surface)");
            r.Rule(".conn", "stroke:var(--jc-muted);stroke-width:1;stroke-dasharray:2 3;fill:none");
            r.Rule(".ar.off", "fill-opacity:0");
            r.Rule(".ring", "font-size:10px");
            r.Rule(".gtrack", "fill:var(--jc-grid);stroke:none");
            r.Rule(".needle", "fill:var(--jc-text);stroke:none");
            r.Rule(".hub", "fill:var(--jc-text);stroke:var(--jc-surface);stroke-width:2");
            r.Rule(".tgt", "stroke:var(--jc-text);stroke-width:3;stroke-linecap:round");
            r.Rule(".big", "font-size:28px;font-weight:600");
            r.Rule(".rg0,.rg1,.rg2", "fill:var(--jc-muted);stroke:none");
            r.Rule(".rg0", "fill-opacity:.1");
            r.Rule(".rg1", "fill-opacity:.2");
            r.Rule(".rg2", "fill-opacity:.32");
            r.Rule(".tgroup", "fill:var(--sc);fill-opacity:.12;stroke:var(--jc-surface);stroke-width:2");
            r.Rule(".bar.part", "fill-opacity:.35");
            r.Rule(".lgt", "fill:var(--jc-text2)");
            r.Rule(".sw", "fill:var(--sc);transform-box:fill-box;transform-origin:center");
            r.Rule(".hit", "fill:var(--jc-surface);fill-opacity:0;stroke:none");
            r.Rule(".ln", "fill:none;stroke:var(--sc);stroke-width:2;stroke-linejoin:round;stroke-linecap:round");
            r.Rule(".ar", "fill:var(--sc);fill-opacity:.28;stroke:none");
            r.Rule(".bar", "fill:var(--sc)");
            r.Rule(".ov", "fill:var(--sc);fill-opacity:.55;stroke:var(--sc);stroke-width:1");
            r.Rule(".pt",
                "fill:var(--sc);stroke:var(--jc-surface);stroke-width:1.5;transform-box:fill-box;transform-origin:center");
            r.Rule(".bub",
                "fill:var(--sc);fill-opacity:.6;stroke:var(--sc);stroke-width:1.5;transform-box:fill-box;transform-origin:center");
            r.Rule(".sl", "fill:var(--sc);stroke:var(--jc-surface);stroke-width:2");
            r.Rule(".eb", "stroke:var(--jc-text2);stroke-width:1.5;fill:none");
            r.Rule(".pt.h", "fill-opacity:0;stroke-opacity:0");
            r.Rule(".mk:hover .pt.h,.mk:focus-visible .pt.h", "fill-opacity:1;stroke-opacity:1");
            r.Rule(".pt.sm", "fill-opacity:.55;stroke:none");
            r.Rule(".na", "fill:var(--jc-grid)");
            r.Rule(".mk:hover .sl,.mk:focus-visible .sl", "transform:translate(var(--dx,0),var(--dy,0))");
            r.Rule(".cell", "stroke:var(--jc-surface);stroke-width:2");
            r.Rule(".vl",
                "stroke:var(--jc-text2);stroke-width:1;stroke-dasharray:3 3;opacity:0;transition:opacity .15s");
            r.Rule(".cx", "fill:var(--sc);stroke:var(--jc-surface);stroke-width:1.5;opacity:0;transition:opacity .15s");
            r.Rule(".mkr", "fill:var(--jc-surface);stroke:var(--sc);stroke-width:2");
            r.Rule(".lbl", "font-size:11px;font-weight:500;pointer-events:none");
            r.Rule(".ctr", "text-anchor:middle");
            r.Rule(".end", "text-anchor:end");
            r.Rule(".mid", "dominant-baseline:central");
            r.Rule(".mk", "outline:none");
            r.Rule(".m,.pt,.bub,.bar,.ov,.sl,.cell",
                "transition:opacity .15s ease,stroke-width .15s ease,transform .15s ease,fill-opacity .15s ease");
            r.Rule(".mk:hover .m,.mk:focus-visible .m", "stroke:var(--jc-text);stroke-width:2");
            r.Rule(".mk:hover .pt,.mk:focus-visible .pt", "transform:scale(1.6)");
            r.Rule(".mk:hover .bub,.mk:focus-visible .bub", "transform:scale(1.08);fill-opacity:.85");
            r.Rule(".mk:hover .bar,.mk:focus-visible .bar", "filter:none;stroke:var(--jc-text);stroke-width:2");
            r.Rule(".mk:hover .ov,.mk:focus-visible .ov", "fill-opacity:.85;stroke-width:2");
            r.Rule(".mk:hover .sl,.mk:focus-visible .sl", "stroke:var(--jc-text);stroke-width:2.5");
            r.Rule(".mk:hover .box,.mk:focus-visible .box", "fill-opacity:.55;stroke-width:2.5");
            r.Rule(".mk:hover .wk,.mk:focus-visible .wk,.mk:hover .cd,.mk:focus-visible .cd", "stroke-width:2.5");
            r.Rule(".mk:hover .cell,.mk:focus-visible .cell", "stroke:var(--jc-text);stroke-width:2.5");
            r.Rule(".mk:hover .vl,.mk:focus-visible .vl,.mk:hover .cx,.mk:focus-visible .cx", "opacity:1");
            r.Rule(".mk:hover .mkr,.mk:focus-visible .mkr", "stroke-width:3");
            r.Rule(".tt",
                "opacity:0;visibility:hidden;pointer-events:none;transition:opacity .15s ease,visibility .15s");
            r.Rule(".mk:hover .tt,.mk:focus-visible .tt", "opacity:1;visibility:visible");
            r.Rule(".tb", "fill:var(--jc-tip);stroke:var(--jc-axis);stroke-width:1");
            r.Rule(".tx", "fill:var(--jc-text);font-size:12px");
            r.Rule(".tx.b", "font-weight:600");
            r.Rule(".tsw", "fill:var(--sc)");
            r.Rule(".lg", "cursor:default;transition:opacity .15s ease");
            r.Rule(".lg:hover .lgt", "fill:var(--jc-text)");
            r.Rule(".sr", "transition:opacity .15s ease");

            r.Has(".mk:hover",
                ".mk:not(:hover) .m,.mk:not(:hover) .pt,.mk:not(:hover) .bub,.mk:not(:hover) .bar,.mk:not(:hover) .ov,.mk:not(:hover) .cell",
                "opacity:.4");
            r.Has(".mk:focus-visible", ".mk:not(:focus-visible) .m", "opacity:.4");

            return r.ToString();
        }

        public static string ExternalRoot()
        {
            var b = new StringBuilder();
            b.Append(":root{").Append(Variables(Palette.Light)).Append('}');

            b.Append("@media (prefers-color-scheme:dark){:root:not([data-theme=\"light\"]){")
                .Append(Variables(Palette.Dark)).Append("}}");

            b.Append(":root[data-theme=\"dark\"]{").Append(Variables(Palette.Dark)).Append('}');
            b.Append(".jc[data-theme=\"light\"]{color-scheme:light;").Append(Variables(Palette.Light)).Append('}');
            b.Append(".jc[data-theme=\"dark\"]{color-scheme:dark;").Append(Variables(Palette.Dark)).Append('}');

            b.Append(".jc{display:block;color-scheme:light;font-family:").Append(FontStack)
                .Append(";font-size:12px}");

            b.Append(".jc:not([width]){width:100%;height:auto;max-width:100%}");

            b.Append(".jc-message{color:var(--jc-muted);font-family:").Append(FontStack)
                .Append(";font-size:16px;padding:15px;text-align:center}");

            b.Append(
                "@media (prefers-color-scheme:dark){:root:not([data-theme=\"light\"]) .jc:not([data-theme=\"light\"]){color-scheme:dark}}");
            b.Append(":root[data-theme=\"dark\"] .jc:not([data-theme=\"light\"]){color-scheme:dark}");

            return b.ToString();
        }

        public static string ExternalSeries(string k)
        {
            var r = new CssRules(k);

            for (var i = 0; i < ChartLimits.MaxSeries; i++)
            {
                var c = i % Palette.CategoricalCount;
                r.Rule(".s" + i, "--sc:var(--jc-s" + i + ",var(--jc-c" + c + "))");
                r.Rule(".s" + i + " .on", "fill:var(--jc-o" + i + ",var(--jc-ct" + c + "))");
                SeriesLinks(r, i);
            }

            return r.ToString();
        }

        public static string ExternalCustomColours(IReadOnlyList<string?> colours, int seriesCount)
        {
            var b = new StringBuilder();
            var count = Math.Min(seriesCount, ChartLimits.MaxSeries);

            for (var i = 0; i < count && i < colours.Count; i++)
            {
                if (colours[i] is { } custom)
                {
                    b.Append("--jc-s").Append(i).Append(':').Append(custom).Append(";--jc-o").Append(i).Append(':')
                        .Append(OnCustom(custom)).Append(';');
                }
            }

            return b.Length == 0 ? string.Empty : b.ToString(0, b.Length - 1);
        }

        public static string ExternalVariants(string k)
        {
            var r = new CssRules(k);

            for (var i = 0; i < StyleCaps.PieDirections; i++)
            {
                var angle = (i + 0.5) / StyleCaps.PieDirections * 2 * Math.PI - Math.PI;

                r.Rule(".sl.px" + i,
                    "--dx:" + Num.F(Math.Cos(angle) * 6) + "px;--dy:" + Num.F(Math.Sin(angle) * 6) + "px");
            }

            for (var size = 10; size <= 22; size++)
            {
                r.Rule(".fs" + size, "font-size:" + size + "px");
            }

            return r.ToString();
        }

        public static string SequentialClasses(string k)
        {
            var b = new StringBuilder();

            for (var i = 0; i < Palette.Light.Sequential.Count; i++)
            {
                b.Append(k).Append(" .q").Append(i).Append("{fill:var(--jc-q").Append(i).Append(")}");
                b.Append(k).Append(" .qt").Append(i).Append("{fill:var(--jc-qt").Append(i).Append(")}");
            }

            for (var i = 0; i < Palette.Light.Diverging.Count; i++)
            {
                b.Append(k).Append(" .d").Append(i).Append("{fill:var(--jc-d").Append(i).Append(")}");
                b.Append(k).Append(" .dt").Append(i).Append("{fill:var(--jc-dt").Append(i).Append(")}");
            }

            return b.ToString();
        }
    }
}