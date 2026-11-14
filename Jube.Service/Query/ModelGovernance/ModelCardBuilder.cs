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
using System.Net;
using System.Text;
using Jube.Dto.Options;
using Jube.Dto.Query.ModelGovernance;

namespace Jube.Service.Query.ModelGovernance
{
    public sealed record ModelCardDrift(string ScoreName, DateTime ObservedDate, double Psi, int StatusId);

    public sealed record ModelCardExhaustive(double? Score, int? Models, DateTime? CompletedDate);

    public sealed record ModelCardInput(string Name, int KindId, Guid ModelGuid, DateTime Generated,
        string GeneratedBy, ModelGovernanceRecordDto? Governance, int Versions, IReadOnlyList<ModelCardDrift> Drift,
        ModelCardExhaustive? Exhaustive);

    public static class ModelCardBuilder
    {
        private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        private static string Date(DateTime? value) =>
            value is null ? "not set" : value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " UTC";

        private static string Tier(int id) => Enum.IsDefined((ModelRiskTier)id) ? ((ModelRiskTier)id).ToString() : "Unknown";

        private static string Status(int id) => id switch { 1 => "Stable", 2 => "Moderate drift", 3 => "Significant drift", _ => "Unknown" };

        private static string Kind(int id) => id == 2 ? "Exhaustive adaptation" : "Entity analysis model";

        public static string Build(ModelCardInput input)
        {
            var html = new StringBuilder();

            html.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">");
            html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            html.Append("<title>Model card: ").Append(E(input.Name)).Append("</title><style>");
            html.Append(":root{--bg:#fff;--fg:#1f2933;--muted:#616e7c;--line:#d9dee3;--ok:#1b7f3b;--warn:#a15c00;--bad:#b42318}");
            html.Append("@media (prefers-color-scheme: dark){:root{--bg:#121417;--fg:#e4e7eb;--muted:#9aa5b1;--line:#2d3339;--ok:#5fd38a;--warn:#f0b35c;--bad:#ff8a80}}");
            html.Append("body{margin:0;padding:24px 16px;background:var(--bg);color:var(--fg);font:15px/1.5 system-ui,sans-serif}");
            html.Append("main{max-width:860px;margin:0 auto}h1{font-size:1.6rem;margin:0 0 4px}h2{font-size:1.1rem;margin:28px 0 8px;border-bottom:1px solid var(--line);padding-bottom:4px}");
            html.Append("dl{display:grid;grid-template-columns:max-content 1fr;gap:4px 16px;margin:0}dt{color:var(--muted)}dd{margin:0;white-space:pre-wrap}");
            html.Append("table{border-collapse:collapse;width:100%}th,td{border-bottom:1px solid var(--line);padding:4px 8px;text-align:left}small{color:var(--muted)}");
            html.Append("svg{width:100%;height:auto;max-width:640px}</style></head><body><main>");

            html.Append("<h1>").Append(E(input.Name)).Append("</h1><small>")
                .Append(E(Kind(input.KindId))).Append(" &middot; ").Append(E(input.ModelGuid.ToString())).Append("</small>");

            html.Append("<h2>Governance</h2>");

            if (input.Governance is null)
            {
                html.Append("<p>No governance statement has been recorded for this model.</p>");
            }
            else
            {
                var g = input.Governance;
                html.Append("<dl><dt>Owner</dt><dd>").Append(E(g.OwnerUser)).Append("</dd>");
                html.Append("<dt>Risk tier</dt><dd>").Append(E(Tier(g.RiskTierId))).Append("</dd>");
                html.Append("<dt>Intended purpose</dt><dd>").Append(E(g.IntendedPurpose)).Append("</dd>");
                html.Append("<dt>Human oversight</dt><dd>").Append(E(g.HumanOversight)).Append("</dd>");
                html.Append("<dt>Next review</dt><dd>").Append(E(Date(g.NextReviewDate))).Append("</dd>");
                html.Append("<dt>Statement</dt><dd>version ").Append(g.Version.ToString(CultureInfo.InvariantCulture))
                    .Append(" of ").Append(input.Versions.ToString(CultureInfo.InvariantCulture)).Append(", saved by ")
                    .Append(E(g.CreatedUser)).Append(" on ").Append(E(Date(g.CreatedDate))).Append("</dd></dl>");
            }

            if (input.Exhaustive is { } exhaustive)
            {
                html.Append("<h2>Training outcome</h2><dl>");
                html.Append("<dt>Best score</dt><dd>").Append(exhaustive.Score is null ? "not recorded" : exhaustive.Score.Value.ToString("0.####", CultureInfo.InvariantCulture)).Append("</dd>");
                html.Append("<dt>Models tried</dt><dd>").Append(exhaustive.Models?.ToString(CultureInfo.InvariantCulture) ?? "not recorded").Append("</dd>");
                html.Append("<dt>Completed</dt><dd>").Append(E(Date(exhaustive.CompletedDate))).Append("</dd></dl>");
            }

            if (input.KindId == 1)
            {
                html.Append("<h2>Drift history</h2>");

                if (input.Drift.Count == 0)
                {
                    html.Append("<p>No drift checks have been recorded. Drift monitoring is off unless EnableModelDriftMonitoring is True, and needs enough archived scores.</p>");
                }
                else
                {
                    foreach (var group in input.Drift.GroupBy(d => d.ScoreName).OrderBy(g => g.Key, StringComparer.Ordinal))
                    {
                        var series = group.OrderBy(d => d.ObservedDate).ToList();
                        html.Append("<h3>").Append(E(group.Key)).Append("</h3>");
                        AppendChart(html, series);
                        html.Append("<table><thead><tr><th>Date</th><th>PSI</th><th>Status</th></tr></thead><tbody>");

                        foreach (var point in series.AsEnumerable().Reverse().Take(10))
                        {
                            html.Append("<tr><td>").Append(E(Date(point.ObservedDate))).Append("</td><td>")
                                .Append(point.Psi.ToString("0.000", CultureInfo.InvariantCulture)).Append("</td><td>")
                                .Append(E(Status(point.StatusId))).Append("</td></tr>");
                        }

                        html.Append("</tbody></table>");
                    }
                }
            }

            html.Append("<h2>About this card</h2><p><small>Generated ").Append(E(Date(input.Generated)))
                .Append(" by ").Append(E(input.GeneratedBy))
                .Append(". Drift is the population stability index of the score distribution and says the scores moved, not that the model is wrong. Input drift, AUC and calibration against outcomes are not on this card.</small></p>");
            html.Append("</main></body></html>");

            return html.ToString();
        }

        private static void AppendChart(StringBuilder html, IReadOnlyList<ModelCardDrift> series)
        {
            const double width = 640, height = 160, left = 36, top = 8, bottom = 20;
            var max = Math.Max(0.35, series.Max(s => s.Psi) * 1.1);
            double Y(double psi) => top + (height - top - bottom) * (1 - psi / max);
            double X(int i) => series.Count == 1 ? left + (width - left - 8) / 2 : left + (width - left - 8) * i / (series.Count - 1);
            string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

            html.Append("<svg viewBox=\"0 0 640 160\" role=\"img\" aria-label=\"PSI over time\">");

            foreach (var (level, color, label) in new[] { (0.1, "var(--warn)", "0.1"), (0.25, "var(--bad)", "0.25") })
            {
                html.Append("<line x1=\"").Append(F(left)).Append("\" x2=\"632\" y1=\"").Append(F(Y(level))).Append("\" y2=\"")
                    .Append(F(Y(level))).Append("\" stroke=\"").Append(color).Append("\" stroke-dasharray=\"4 3\"/>");
                html.Append("<text x=\"2\" y=\"").Append(F(Y(level) + 4)).Append("\" fill=\"var(--muted)\" font-size=\"10\">").Append(label).Append("</text>");
            }

            html.Append("<polyline fill=\"none\" stroke=\"var(--fg)\" stroke-width=\"2\" points=\"");

            for (var i = 0; i < series.Count; i++)
            {
                html.Append(F(X(i))).Append(',').Append(F(Y(series[i].Psi))).Append(' ');
            }

            html.Append("\"/>");

            for (var i = 0; i < series.Count; i++)
            {
                html.Append("<circle cx=\"").Append(F(X(i))).Append("\" cy=\"").Append(F(Y(series[i].Psi))).Append("\" r=\"3\" fill=\"var(--fg)\"/>");
            }

            html.Append("</svg>");
        }
    }
}
