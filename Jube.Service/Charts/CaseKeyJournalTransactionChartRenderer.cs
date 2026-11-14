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
using Jube.Resources;
using Jube.Service.Query.CaseKeyJournal;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public static class CaseKeyJournalTransactionChartRenderer
    {
        public const int MaximumItems = 15;

        public static string Render(CaseKeyJournalTransactionChart kind, JournalAdaptationDto adaptation, bool dark,
            IStringLocalizer? strings = null)
        {
            ArgumentNullException.ThrowIfNull(adaptation);

            var theme = dark ? ThemeMode.Dark : ThemeMode.Light;

            return kind switch
            {
                CaseKeyJournalTransactionChart.Contribution => Contribution(adaptation, theme, strings),
                CaseKeyJournalTransactionChart.Calibration => Calibration(adaptation, theme, strings),
                CaseKeyJournalTransactionChart.Gauge => Gauge(adaptation, theme, strings),
                CaseKeyJournalTransactionChart.Drivers => Drivers(adaptation, theme, strings),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "That chart is not available.")
            };
        }

        public static string Label(JournalContributionItemDto item) => item.HumanLabel ?? item.Name ?? string.Empty;

        private static string T(IStringLocalizer? strings, string key, string english, params object[] arguments) =>
            ChartText.Of(strings, key, english, arguments);

        private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string BaseSubtitle(IStringLocalizer? strings, double baseValue, string? space) =>
            T(strings, CaseKeyJournalChartResources.BaseSubtitle, "From the base value of {0}", Number(baseValue)) +
            (space is { } named ? T(strings, CaseKeyJournalChartResources.InSpace, " in {0}", named) : string.Empty);

        private static string Contribution(JournalAdaptationDto adaptation, ThemeMode theme, IStringLocalizer? strings)
        {
            var contribution = adaptation.Contribution;
            var items = (contribution?.Items ?? [])
                .Where(item => !item.Hidden && item.Signed.HasValue && !string.IsNullOrEmpty(Label(item)))
                .OrderByDescending(item => Math.Abs(item.Signed!.Value)).Take(MaximumItems).ToList();

            if (contribution is null || items.Count == 0)
            {
                throw new ArgumentException("There is no contribution to chart.");
            }

            var hidden = contribution.HiddenItems > 0
                ? T(strings, CaseKeyJournalChartResources.HiddenInputs, "; {0} inputs hidden", contribution.HiddenItems)
                : string.Empty;

            if (contribution is { Exact: true, BaseValue: { } baseValue })
            {
                var builder = Chart.Waterfall()
                    .Title(T(strings, CaseKeyJournalChartResources.ContributionMoved, "{0}: what moved the score", adaptation.Name))
                    .Subtitle(BaseSubtitle(strings, baseValue, contribution.Space) + hidden)
                    .Size(560, 40 + 34 * (items.Count + 2))
                    .Theme(theme)
                    .ValueFormat(value => value.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture))
                    .Step(T(strings, CaseKeyJournalChartResources.BaseValue, "Base value"), baseValue);

                foreach (var item in items)
                {
                    builder = builder.Step(Label(item), item.Signed!.Value);
                }

                return builder.Total(contribution.HiddenItems > 0
                    ? T(strings, CaseKeyJournalChartResources.VisibleInputs, "Visible inputs")
                    : T(strings, CaseKeyJournalChartResources.Score, "Score")).Render();
            }

            return Chart.Bar()
                .Title(T(strings, CaseKeyJournalChartResources.ContributionPushed, "{0}: what pushed the score", adaptation.Name))
                .Subtitle(T(strings, CaseKeyJournalChartResources.RelativeContributions, "Relative contributions") +
                          (contribution.Method is { } method ? T(strings, CaseKeyJournalChartResources.ByMethod, " by {0}", method) : string.Empty) +
                          T(strings, CaseKeyJournalChartResources.NotAdditive, ", not additive") + hidden)
                .Size(720, 120 + items.Count * 36)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .Horizontal()
                .ValueFormat(value => value.ToString("0.###", CultureInfo.InvariantCulture))
                .Categories(items.Select(Label))
                .Series(T(strings, CaseKeyJournalChartResources.Contribution, "Contribution"), items.Select(item => item.Signed!.Value))
                .Render();
        }

        private static string Gauge(JournalAdaptationDto adaptation, ThemeMode theme, IStringLocalizer? strings)
        {
            if (adaptation.Value is not { } value)
            {
                throw new ArgumentException("There is no score to chart.");
            }

            var band = JournalScoreBands.Of(adaptation);
            var threshold = adaptation.Result?.Threshold;
            var thresholdText = threshold.HasValue
                ? T(strings, CaseKeyJournalChartResources.GaugeThreshold, "threshold {0}", Number(threshold.Value))
                : T(strings, CaseKeyJournalChartResources.GaugeNoThreshold, "no threshold declared");

            return Chart.Bullet()
                .Title(adaptation.HumanLabel ?? adaptation.Name)
                .Subtitle(band is { } named
                    ? T(strings, CaseKeyJournalChartResources.GaugeCalibrated, "Calibrated probability {0} · {1} · {2}", Number(value),
                        JournalScoreBands.Word(named), thresholdText)
                    : T(strings, CaseKeyJournalChartResources.GaugeScore, "Score {0} (not a probability) · {1}", Number(value), thresholdText))
                .Size(520, 120)
                .Theme(theme)
                .Thick()
                .ValueFormat(item => item.ToString("0.##", CultureInfo.InvariantCulture))
                .Item(adaptation.Name, value, threshold,
                    band is null ? null : [0.25, 0.5, 0.75, 1.0])
                .Render();
        }

        private static string Drivers(JournalAdaptationDto adaptation, ThemeMode theme, IStringLocalizer? strings)
        {
            var contribution = adaptation.Contribution;
            var (drivers, other) = JournalActivationCards.DriversOf(adaptation);

            if (contribution is null || drivers.Count == 0)
            {
                throw new ArgumentException("There is no contribution to chart.");
            }

            var hidden = contribution.HiddenItems > 0
                ? T(strings, CaseKeyJournalChartResources.HiddenInputs, "; {0} inputs hidden", contribution.HiddenItems)
                : string.Empty;

            if (contribution is { Exact: true, BaseValue: { } baseValue })
            {
                var builder = Chart.Waterfall()
                    .Title(T(strings, CaseKeyJournalChartResources.DriversMoved, "What moved {0}", adaptation.HumanLabel ?? adaptation.Name))
                    .Subtitle(BaseSubtitle(strings, baseValue, contribution.Space) + hidden)
                    .Size(520, 40 + 30 * (drivers.Count + 3))
                    .Theme(theme)
                    .ValueFormat(item => item.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture))
                    .Step(T(strings, CaseKeyJournalChartResources.BaseValue, "Base value"), baseValue);

                foreach (var driver in drivers)
                {
                    builder = builder.Step(driver.Label, driver.Signed);
                }

                if (other > 0)
                {
                    var rest = contribution.Items.Where(item => !item.Hidden && item.Signed.HasValue).Sum(item =>
                        item.Signed!.Value) - drivers.Sum(driver => driver.Signed);

                    builder = builder.Step(T(strings, CaseKeyJournalChartResources.OtherInputs, "{0} other inputs", other), rest);
                }

                return builder.Total(contribution.HiddenItems > 0
                    ? T(strings, CaseKeyJournalChartResources.VisibleInputs, "Visible inputs")
                    : T(strings, CaseKeyJournalChartResources.Score, "Score")).Render();
            }

            return Chart.Bar()
                .Title(T(strings, CaseKeyJournalChartResources.DriversPushed, "What pushed {0}", adaptation.HumanLabel ?? adaptation.Name))
                .Subtitle(T(strings, CaseKeyJournalChartResources.RelativeNotAdditive, "Relative contributions, not additive") + hidden)
                .Size(520, 100 + drivers.Count * 34)
                .Theme(theme)
                .Legend(LegendPosition.None)
                .Horizontal()
                .ValueFormat(item => item.ToString("0.###", CultureInfo.InvariantCulture))
                .Categories(drivers.Select(driver => driver.Label))
                .Series(T(strings, CaseKeyJournalChartResources.Contribution, "Contribution"), drivers.Select(driver => driver.Signed))
                .Render();
        }

        private static string Calibration(JournalAdaptationDto adaptation, ThemeMode theme, IStringLocalizer? strings)
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
                .Title(T(strings, CaseKeyJournalChartResources.CalibrationTitle, "{0}: did the score mean what it said", adaptation.Name))
                .Subtitle(adaptation.Calibration!.Sample is { } sample
                    ? T(strings, CaseKeyJournalChartResources.CalibrationValidated, "Validated on {0} cases", sample)
                    : T(strings, CaseKeyJournalChartResources.CalibrationExpected, "Expected against observed"))
                .Size(560, 460)
                .Theme(theme)
                .Curve(adaptation.Name, bands.Select(band => band.Expected!.Value),
                    bands.Select(band => band.Observed!.Value), bands.Select(band => (double)(band.Count ?? 0)),
                    null, error)
                .Render();
        }
    }
}
