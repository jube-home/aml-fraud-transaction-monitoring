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

using System;
using System.Linq;
using FluentAssertions;
using Jube.CaseRisk.Journal;
using Jube.Dto.Options;
using Jube.Service.Charts;
using Jube.Service.Query.CaseKeyJournal;
using Xunit;
using static Jube.Test.CaseRisk.Journal.JournalEvents;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class CaseKeyJournalChartRendererTests
    {
        private static CaseKeyJournalAnalyticsDto Report()
        {
            var events = Enumerable.Range(0, 60).Select(i => Event(i + 1, Start.AddHours(i * 7), 100 + i % 9,
                counterparty: i % 4 == 0 ? "A" : "B", other: i * 2.0)).ToList();
            var measures = new[] { "Amount", "Other" };

            return new CaseKeyJournalAnalyticsDto
            {
                Progression = ProgressionCalculator.Compute(events, measures),
                Distributions = DistributionCalculator.Compute(events, measures),
                Velocity = VelocityCalculator.Compute(events),
                Gaps = GapCalculator.Compute(events),
                Counterparties = CounterpartyCalculator.Compute(events, ["Counterparty"]),
                Correlation = CorrelationCalculator.Compute(events, measures),
                Drift = DriftCalculator.Compute(events, measures)
            };
        }

        [Theory]
        [InlineData(CaseKeyJournalChart.Progression, "Amount")]
        [InlineData(CaseKeyJournalChart.Distribution, "Amount")]
        [InlineData(CaseKeyJournalChart.Velocity, null)]
        [InlineData(CaseKeyJournalChart.Gaps, null)]
        [InlineData(CaseKeyJournalChart.Counterparties, "Counterparty")]
        [InlineData(CaseKeyJournalChart.Correlation, null)]
        [InlineData(CaseKeyJournalChart.Drift, null)]
        public void EveryChartDrawsAnSvgInBothThemes(CaseKeyJournalChart kind, string? field)
        {
            var report = Report();

            CaseKeyJournalChartRenderer.Render(kind, report, field, false).Should().StartWith("<svg");
            CaseKeyJournalChartRenderer.Render(kind, report, field, true).Should().StartWith("<svg");
        }

        [Theory]
        [InlineData(CaseKeyJournalChart.Progression, "Missing")]
        [InlineData(CaseKeyJournalChart.Distribution, "Missing")]
        [InlineData(CaseKeyJournalChart.Velocity, null)]
        [InlineData(CaseKeyJournalChart.Gaps, null)]
        [InlineData(CaseKeyJournalChart.Counterparties, null)]
        [InlineData(CaseKeyJournalChart.Correlation, null)]
        [InlineData(CaseKeyJournalChart.Drift, null)]
        public void WithNothingToDrawTheChartSaysSoRatherThanDrawingAnEmptyOne(CaseKeyJournalChart kind,
            string? field)
        {
            var empty = new CaseKeyJournalAnalyticsDto();

            var act = () => CaseKeyJournalChartRenderer.Render(kind, empty, field, false);

            act.Should().Throw<ArgumentException>().WithMessage("There is no * to chart.");
        }

        [Fact]
        public void EachChartKindMapsToTheSectionItNeeds()
        {
            CaseKeyJournalChartSections.For(CaseKeyJournalChart.Progression).Should().Be("progression");
            CaseKeyJournalChartSections.For(CaseKeyJournalChart.Distribution).Should().Be("distributions");
            CaseKeyJournalChartSections.For(CaseKeyJournalChart.Velocity).Should().Be("velocity");
            CaseKeyJournalChartSections.For(CaseKeyJournalChart.Gaps).Should().Be("gaps");
            CaseKeyJournalChartSections.For(CaseKeyJournalChart.Counterparties).Should().Be("counterparties");
            CaseKeyJournalChartSections.For(CaseKeyJournalChart.Correlation).Should().Be("correlation");
            CaseKeyJournalChartSections.For(CaseKeyJournalChart.Drift).Should().Be("drift");
        }
    }
}
