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
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseKeyJournal;

namespace Jube.Service.Charts
{
    public static class CaseKeyJournalTransactionChartRenderer
    {
        public const int MaximumItems = 15;

        public static string Render(CaseKeyJournalTransactionChart kind, JournalAdaptationDto adaptation, bool dark)
        {
            ArgumentNullException.ThrowIfNull(adaptation);

            var theme = dark ? ThemeMode.Dark : ThemeMode.Light;

            return kind switch
            {
                CaseKeyJournalTransactionChart.Contribution => Contribution(adaptation, theme),
                CaseKeyJournalTransactionChart.Calibration => Calibration(adaptation, theme),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "That chart is not available.")
            };
        }

        public static string Label(JournalContributionItemDto item) => item.HumanLabel ?? item.Name ?? string.Empty;

        private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Contribution(JournalAdaptationDto adaptation, ThemeMode theme)
        {
            var contribution = adaptation.Contribution;
            var items = (contribution?.Items ?? [])
                .Where(item => !item.Hidden && item.Signed.HasValue && !string.IsNullOrEmpty(Label(item)))
                .OrderByDescending(item => Math.Abs(item.Signed!.Value)).Take(MaximumItems).ToList();

            if (contribution is null || items.Count == 0)
            {
                throw new ArgumentException("There is no contribution to chart.");
            }

            var hidden = contribution.HiddenItems > 0 ? $"; {contribution.HiddenItems} inputs hidden" : string.Empty;

            if (contribution is { Exact: true, BaseValue: { } baseValue })
            {
                var builder = Chart.Waterfall()
                    .Title(adaptation.Name + ": what moved the score")
                    .Subtitle("From the base value of " + Number(baseValue) + (contribution.Space is { } space
                        ? " in " + space
                        : string.Empty) + hidden)
                    .Size(560, 40 + 34 * (items.Count + 2))
                    .Theme(theme)
                    .ValueFormat(value => value.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture))
                    .Step("Base value", baseValue);

                foreach (var item in items)
                {
                    builder = builder.Step(Label(item), item.Signed!.Value);
                }

                return builder.Total(contribution.HiddenItems > 0 ? "Visible inputs" : "Score").Render();
            }

            return Chart.Bar()
                .Title(adaptation.Name + ": what pushed the score")
                .Subtitle("Relative contributions" + (contribution.Method is { } method ? " by " + method : string.Empty) +
                          ", not additive" + hidden)
                .Size(720, 120 + items.Count * 36)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .Horizontal()
                .ValueFormat(value => value.ToString("0.###", CultureInfo.InvariantCulture))
                .Categories(items.Select(Label))
                .Series("Contribution", items.Select(item => item.Signed!.Value))
                .Render();
        }

        private static string Calibration(JournalAdaptationDto adaptation, ThemeMode theme)
        {
            var bands = (adaptation.Calibration?.Bands ?? [])
                .Where(band => band.Expected.HasValue && band.Observed.HasValue).ToList();

            if (bands.Count == 0)
            {
                throw new ArgumentException("There is no calibration to chart.");
            }

            var total = bands.Sum(band => band.Count ?? 0);
            double? error = total > 0
                ? bands.Sum(band => (band.Count ?? 0) * Math.Abs(band.Expected!.Value - band.Observed!.Value)) / total
                : null;

            return Chart.Calibration()
                .Title(adaptation.Name + ": did the score mean what it said")
                .Subtitle(adaptation.Calibration!.Sample is { } sample
                    ? "Validated on " + sample + " cases"
                    : "Expected against observed")
                .Size(560, 460)
                .Theme(theme)
                .Curve(adaptation.Name, bands.Select(band => band.Expected!.Value),
                    bands.Select(band => band.Observed!.Value), bands.Select(band => (double)(band.Count ?? 0)),
                    null, error)
                .Render();
        }
    }
}
