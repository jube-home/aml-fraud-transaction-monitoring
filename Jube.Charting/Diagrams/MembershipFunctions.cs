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
    public sealed class MembershipFunctions(MembershipFunctionsSpec spec) : IStyleModeChart
    {
        public MembershipFunctionsSpec Spec { get; } = spec;

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new MembershipFunctions(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            Validate();
            var s = Spec;
            var plan = PanelKit.Plan(s.Variables.Count, s.Options.Width, CellHeight, 240, s.MaxColumns);
            var rulesHeight = s.Rules.Count > 0 ? 34 + s.Rules.Count * RuleHeight : 0;

            var summary = "Fuzzy membership functions: " + s.Variables.Count + " variables (" +
                          string.Join(", ", s.Variables.Take(4).Select(v => TextMetrics.Truncate(v.Name, 60, 12))) +
                          ") and " + s.Rules.Count + " rules.";

            var fit = DiagramFrame.Fit(s.Options with { Legend = LegendPosition.None }, summary, [], plan.Width,
                plan.Height + (rulesHeight > 0 ? 14 + rulesHeight : 0), 0, "membership functions");

            var marks = (long)s.Variables.Count * (Columns + DiagramLimits.MaxFuzzySets) + s.Rules.Count;

            return ChartRun.Execute(fit.Options, id =>
            {
                var scene = DiagramFrame.Begin(fit, id, summary, [], marks);
                var w = scene.W;
                scene.OpenGroup(fit.X0, fit.Y0);
                var uses = new Dictionary<(int Variable, int Set), List<int>>();

                for (var r = 0; r < s.Rules.Count; r++)
                {
                    foreach (var t in s.Rules[r].If.Append(s.Rules[r].Then))
                    {
                        var vi = s.Variables.ToList().FindIndex(v => v.Name == t.Variable);
                        var si = s.Variables[vi].Sets.ToList().FindIndex(x => x.Label == t.Set);

                        if (!uses.TryGetValue((vi, si), out var list))
                        {
                            list = [];
                            uses[(vi, si)] = list;
                        }

                        if (!list.Contains(r))
                        {
                            list.Add(r);
                        }
                    }
                }

                for (var v = 0; v < s.Variables.Count; v++)
                {
                    var variable = s.Variables[v];
                    var (ox, oy) = PanelKit.Origin(plan, v);
                    PanelKit.Frame(w, ox, oy, plan, variable.Name);
                    var left = ox + 30;
                    var right = ox + plan.CellWidth - 14;
                    var top = oy + 44;
                    var baseline = oy + plan.CellHeight - 26;
                    var xs = new LinearScale(variable.Min, variable.Max, left, right);
                    var ys = new LinearScale(0, 1, baseline, top);

                    foreach (var (value, label) in new[] { (0.0, "0"), (0.5, "0.5"), (1.0, "1") })
                    {
                        w.Empty("line", ("class", "gl"), ("x1", Num.F(left)), ("y1", Num.F(ys.Map(value))),
                            ("x2", Num.F(right)), ("y2", Num.F(ys.Map(value))));

                        w.Element("text", label, ("class", "kn end mid"), ("x", Num.F(left - 5)),
                            ("y", Num.F(ys.Map(value))));
                    }

                    PanelKit.XAxis(w, xs, left, right, baseline, 5);
                    var width = (right - left) / Columns;

                    for (var k = 0; k < Columns; k++)
                    {
                        var x = variable.Min + (k + 0.5) / Columns * (variable.Max - variable.Min);
                        var px = left + (k + 0.5) * width;

                        var rows = new List<TipRow>
                        {
                            new(TextMetrics.Truncate(variable.Name, 160, 12, true) + " = " + NumberFormat.Value(x),
                                true)
                        };

                        var memberships = variable.Sets.Select(set => set.Membership(x)).ToArray();

                        for (var j = 0; j < memberships.Length; j++)
                        {
                            rows.Add(new TipRow(
                                TextMetrics.Truncate(variable.Sets[j].Label, 160, 12) + ": " +
                                NumberFormat.Value(Math.Round(memberships[j], 3)),
                                Math.Abs(memberships[j] - memberships.Max()) < 0.0001 && memberships[j] > 0, j));
                        }

                        var key = "m" + v + "c" + k;

                        scene.OpenMark(key, string.Join(", ", rows.Select(t => t.Text)),
                            scene.External ? string.Empty : "cl0");

                        w.Empty("rect", ("class", "hb"), ("x", Num.F(px - width / 2)), ("y", Num.F(top)),
                            ("width", Num.F(width)), ("height", Num.F(baseline - top)));

                        w.Empty("line", ("class", "vl"), ("x1", Num.F(px)), ("y1", Num.F(top)), ("x2", Num.F(px)),
                            ("y2", Num.F(baseline)));

                        for (var j = 0; j < memberships.Length; j++)
                        {
                            w.Empty("circle", ("class", "cx s" + j), ("cx", Num.F(px)),
                                ("cy", Num.F(ys.Map(memberships[j]))), ("r", "3.5"));
                        }

                        scene.EndMark(key, fit.X0 + px, fit.Y0 + (top + baseline) / 2, rows, true);
                    }

                    var placed = new List<(double X0, double X1, double Y)>();

                    for (var j = 0; j < variable.Sets.Count; j++)
                    {
                        var set = variable.Sets[j];
                        var line = new PathBuilder();
                        var area = new PathBuilder().Move(left, baseline);
                        var peakX = 0.0;
                        var peakCount = 0;

                        var xsSample = Enumerable.Range(0, Samples + 1).Select(k =>
                                variable.Min + (double)k / Samples * (variable.Max - variable.Min))
                            .Concat(set.KeyPoints().Where(x => x > variable.Min && x < variable.Max)).Distinct()
                            .OrderBy(x => x).ToArray();

                        var values = xsSample.Select(x => Math.Clamp(set.Membership(x), 0, 1)).ToArray();
                        var peak = values.Max();

                        for (var k = 0; k < xsSample.Length; k++)
                        {
                            var px = xs.Map(xsSample[k]);
                            var py = ys.Map(values[k]);
                            line.Line(px, py);
                            area.Line(px, py);

                            if (values[k] >= peak - 1e-9)
                            {
                                peakX += px;
                                peakCount++;
                            }
                        }

                        area.Line(right, baseline).Close();
                        peakX /= Math.Max(peakCount, 1);

                        var used = uses.TryGetValue((v, j), out var ruleList)
                            ? string.Concat(ruleList.Where(r => StyleCaps.Keep(scene.External, r, StyleCaps.FuzzyRules))
                                .Select(r => " u" + r))
                            : string.Empty;

                        var detail = new List<TipRow>
                        {
                            new(TextMetrics.Truncate(set.Label, 200, 12, true), true, j),
                            new(set.Describe()),
                            new("Peak membership " + NumberFormat.Value(Math.Round(peak, 3)) + " at " +
                                NumberFormat.Value(variable.Min +
                                                   (peakX - left) / (right - left) * (variable.Max - variable.Min)))
                        };

                        if (uses.TryGetValue((v, j), out var used2))
                        {
                            detail.Add(new TipRow("Used by " + used2.Count + (used2.Count == 1 ? " rule" : " rules")));
                        }

                        var key = "s" + v + "s" + j;

                        scene.OpenMark(key, string.Join(", ", detail.Select(t => t.Text)),
                            "fz " + (StyleCaps.Keep(scene.External, v, StyleCaps.FuzzyVariables)
                                ? "pv" + v + " "
                                : string.Empty) + "s" + j + used);

                        w.Empty("path", ("class", "ar"), ("d", area.ToString()));
                        w.Empty("path", ("class", "ln thin"), ("d", line.ToString()));
                        var text = TextMetrics.Truncate(set.Label, 90, 11, true);
                        var half = TextMetrics.Width(text, 11, true) / 2 + 2;
                        var ly = ys.Map(peak) - 6;

                        foreach (var lift in new[] { 0.0, -14, -28 })
                        {
                            if (!placed.Any(p =>
                                    Math.Abs(p.Y - (ly + lift)) < 13 && peakX - half < p.X1 && peakX + half > p.X0))
                            {
                                ly += lift;
                                break;
                            }
                        }

                        ly = Math.Max(ly, top + 8);
                        placed.Add((peakX - half, peakX + half, ly));

                        w.Element("text", text, ("class", "ant ctr"),
                            ("x", Num.F(Math.Clamp(peakX, left + half, right - half))), ("y", Num.F(ly)));

                        scene.EndMark(key, fit.X0 + peakX, fit.Y0 + ys.Map(peak), detail, true);

                        if (scene.Interactive && j == 0)
                        {
                            scene.Rules(DiagramRules.FuzzyVariable(scene.K, v));
                        }
                    }
                }

                if (s.Rules.Count > 0)
                {
                    var y0 = plan.Height + 14;
                    w.Element("text", "Rules", ("class", "ph"), ("x", "2"), ("y", Num.F(y0 + 14)));

                    for (var r = 0; r < s.Rules.Count; r++)
                    {
                        var rule = s.Rules[r];
                        var y = y0 + 26 + r * RuleHeight;

                        var text = RuleText(rule) + (Math.Abs(rule.Weight - 1) < 0.0001
                            ? string.Empty
                            : "  (weight " + NumberFormat.Value(rule.Weight) + ")");

                        var rows = new List<TipRow>
                        {
                            new(
                                "Rule " + (r + 1) + (Math.Abs(rule.Weight - 1) < 0.0001
                                    ? string.Empty
                                    : ", weight " + NumberFormat.Value(rule.Weight)), true),
                            new("IF " + string.Join(rule.Conjunction ? " AND " : " OR ",
                                rule.If.Select(t => t.Variable + " is " + t.Set))),
                            new("THEN " + rule.Then.Variable + " is " + rule.Then.Set)
                        };

                        var key = "q" + r;
                        scene.OpenMark(key, string.Join(", ", rows.Select(t => t.Text)), "rl");

                        if (r % 2 == 0)
                        {
                            w.Empty("rect", ("class", "sh"), ("x", "0"), ("y", Num.F(y)), ("width", Num.F(plan.Width)),
                                ("height", Num.F(RuleHeight)));
                        }

                        w.Empty("rect", ("class", "hl"), ("x", "0"), ("y", Num.F(y)), ("width", Num.F(plan.Width)),
                            ("height", Num.F(RuleHeight)));

                        w.Element("text", TextMetrics.Truncate(text, plan.Width - 20, 12), ("class", "mid"),
                            ("x", "10"),
                            ("y", Num.F(y + RuleHeight / 2)));

                        scene.EndMark(key, fit.X0 + 40, fit.Y0 + y + RuleHeight / 2, rows);

                        if (scene.Interactive)
                        {
                            scene.Rules(DiagramRules.FuzzyRule(scene.K, r));
                        }
                    }
                }

                scene.CloseGroup();

                return scene.End();
            });
        }

        private const double CellHeight = 190;
        private const double RuleHeight = 22;
        private const int Samples = 96;
        private const int Columns = 24;

        private void Validate()
        {
            var s = Spec;

            if (s.Variables.Count == 0)
            {
                throw new ChartSpecificationException("Membership functions need at least one variable.");
            }

            if (s.Variables.Count > DiagramLimits.MaxFuzzyVariables)
            {
                throw new DiagramTooLargeException("fuzzy variables", s.Variables.Count,
                    DiagramLimits.MaxFuzzyVariables,
                    "Draw the variables that matter.");
            }

            if (s.Rules.Count > DiagramLimits.MaxFuzzyRules)
            {
                throw new DiagramTooLargeException("fuzzy rules", s.Rules.Count, DiagramLimits.MaxFuzzyRules,
                    "Show the highest weighted rules only.");
            }

            if (s.MaxColumns < 1 || s.MaxColumns > 6)
            {
                throw new ChartSpecificationException("MaxColumns must be between 1 and 6.");
            }

            foreach (var v in s.Variables)
            {
                if (!double.IsFinite(v.Min) || !double.IsFinite(v.Max) || !(v.Min < v.Max))
                {
                    throw new ChartSpecificationException(v.Name +
                                                          ": the universe needs finite bounds with min below max.");
                }

                if (v.Sets.Count == 0)
                {
                    throw new ChartSpecificationException(v.Name + ": a variable needs at least one set.");
                }

                if (v.Sets.Count > DiagramLimits.MaxFuzzySets)
                {
                    throw new DiagramTooLargeException("fuzzy sets per variable", v.Sets.Count,
                        DiagramLimits.MaxFuzzySets,
                        "Merge the minor sets.");
                }

                foreach (var set in v.Sets)
                {
                    set.Validate();
                }
            }

            foreach (var r in s.Rules)
            {
                foreach (var t in r.If.Append(r.Then))
                {
                    if (!s.Variables.Any(v => v.Name == t.Variable && v.Sets.Any(x => x.Label == t.Set)))
                    {
                        throw new ChartSpecificationException("A rule refers to " + t.Variable + " is " + t.Set +
                                                              ", which is not declared.");
                    }
                }

                if (r.If.Count == 0 || !(r.Weight >= 0) || !double.IsFinite(r.Weight))
                {
                    throw new ChartSpecificationException(
                        "A rule needs at least one condition and a finite non-negative weight.");
                }
            }
        }

        private static string RuleText(FuzzyRule r)
        {
            var join = r.Conjunction ? " AND " : " OR ";

            return "IF " + string.Join(join, r.If.Select(t => t.Variable + " is " + t.Set)) + " THEN " +
                   r.Then.Variable +
                   " is " + r.Then.Set;
        }
    }
}