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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.CaseRisk.Analytics;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseSearch;
using Jube.Service.Charts;
using Jube.Service.Query.CaseAnalytics;
using Xunit;
using static Jube.Test.CaseRisk.FactBuilder;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class CaseAnalyticsChartRendererTests
    {
        private static readonly Guid RuleOne = Guid.Parse("00000000-0000-0000-0000-00000000aa01");
        private static readonly Guid RuleTwo = Guid.Parse("00000000-0000-0000-0000-00000000aa02");

        private static CaseAnalyticsDto Report()
        {
            var now = Start.AddDays(10);

            var facts = Enumerable.Range(1, 60).Select(i => Fact(i, i * 90.0, i * 90.0 + 120 + i, i % 3 == 0 ? 1 : 2,
                i % 2 == 0 ? RuleOne : RuleTwo, i % 2 == 0 ? "Structuring" : "Velocity", viewedMinutes: i * 90.0 + 10,
                ownedMinutes: i * 90.0 + 20, handoffs: i % 5 == 0 ? 3 : i % 3, priority: i % 100,
                likelihood: (i % 100) / 100.0, firstViewTarget: 15, resolutionTarget: 130))
                .Concat([Fact(100, 9000, lane: 1, priority: 80), Fact(101, 9500, lane: 4)]).ToList();

            var stays = facts.Select(f => new StayFact(f.CaseId, Status, f.CreatedDate, f.ClosedDate)).ToList();

            return new CaseAnalyticsDto
            {
                From = Start, To = now, GeneratedAt = now, CasesAnalysed = facts.Count,
                StatusNames = new Dictionary<Guid, string> { [Status] = "Awaiting review" },
                CycleTimes = CycleTimeCalculator.Compute(facts, now),
                Flow = FlowCalculator.Daily(facts, stays, Start.Date, now.Date),
                StatusDwell = DwellCalculator.ByStatus(stays, now, new Dictionary<Guid, int?>()),
                Aging = AgingCalculator.Open(facts, now),
                Rules = RuleEffectivenessCalculator.Compute(facts),
                Ranking = RankingQualityCalculator.Compute(facts),
                ServiceLevels = SlaCalculator.Compute(facts, now),
                Rhythm = RhythmCalculator.Compute(facts),
                Workload = WorkloadCalculator.Compute(facts, "me"),
                Handoffs = HandoffCalculator.Compute(facts)
            };
        }

        [Theory]
        [InlineData(CaseAnalyticsChart.Flow, "Opened, closed and backlog")]
        [InlineData(CaseAnalyticsChart.FirstView, "Time to first look")]
        [InlineData(CaseAnalyticsChart.Resolution, "Time to resolution")]
        [InlineData(CaseAnalyticsChart.Aging, "Open cases by age")]
        [InlineData(CaseAnalyticsChart.StatusDwell, "Time in each status")]
        [InlineData(CaseAnalyticsChart.RuleEffort, "Analyst time lost to false positives")]
        [InlineData(CaseAnalyticsChart.RulePrecision, "Rule precision")]
        [InlineData(CaseAnalyticsChart.Gains, "Confirmed cases found as cases are worked")]
        [InlineData(CaseAnalyticsChart.Calibration, "Predicted likelihood against what happened")]
        [InlineData(CaseAnalyticsChart.Arrivals, "When cases arrive")]
        [InlineData(CaseAnalyticsChart.FirstViewByHour, "Median time to first look by hour of arrival")]
        [InlineData(CaseAnalyticsChart.ServiceLevelWeekly, "Service level attainment by week")]
        [InlineData(CaseAnalyticsChart.Handoffs, "How often cases change hands")]
        public void EveryChartRendersAnSvgWithItsTitleInBothThemes(CaseAnalyticsChart kind, string title)
        {
            var light = CaseAnalyticsChartRenderer.Render(kind, Report(), false);
            var dark = CaseAnalyticsChartRenderer.Render(kind, Report(), true);

            light.Should().StartWith("<svg").And.Contain(title).And.EndWith("</svg>");
            dark.Should().StartWith("<svg").And.Contain(title);
            dark.Should().NotBe(light);
        }

        [Fact]
        public void TheChartNamesTheRulesAndStatusesNotTheirGuids()
        {
            var effort = CaseAnalyticsChartRenderer.Render(CaseAnalyticsChart.RuleEffort, Report(), false);
            var dwell = CaseAnalyticsChartRenderer.Render(CaseAnalyticsChart.StatusDwell, Report(), false);

            effort.Should().Contain("Structuring").And.Contain("Velocity").And.NotContain(RuleOne.ToString());
            dwell.Should().Contain("Awaiting review").And.NotContain(Status.ToString());
        }

        [Theory]
        [InlineData(CaseAnalyticsChart.Flow)]
        [InlineData(CaseAnalyticsChart.FirstView)]
        [InlineData(CaseAnalyticsChart.Aging)]
        [InlineData(CaseAnalyticsChart.RuleEffort)]
        [InlineData(CaseAnalyticsChart.Gains)]
        [InlineData(CaseAnalyticsChart.Calibration)]
        [InlineData(CaseAnalyticsChart.Arrivals)]
        [InlineData(CaseAnalyticsChart.ServiceLevelWeekly)]
        public void AChartWithNothingBehindItSaysSoInsteadOfDrawingAnEmptyFrame(CaseAnalyticsChart kind)
        {
            var now = Start.AddDays(1);

            var empty = new CaseAnalyticsDto
            {
                CycleTimes = CycleTimeCalculator.Compute([], now),
                Aging = AgingCalculator.Open([], now),
                Ranking = RankingQualityCalculator.Compute([]),
                ServiceLevels = SlaCalculator.Compute([], now),
                Rhythm = RhythmCalculator.Compute([]),
                Handoffs = HandoffCalculator.Compute([])
            };

            var act = () => CaseAnalyticsChartRenderer.Render(kind, empty, false);

            act.Should().Throw<ArgumentException>().WithMessage("There is no*");
        }

        [Fact]
        public void TheUnsureGainsAndCalibrationChartsSayTheyRestOnTooFewCases()
        {
            var report = Report();
            var few = new CaseAnalyticsDto
            {
                Ranking = RankingQualityCalculator.Compute(
                [
                    Fact(1, 0, 10, 1, priority: 90, likelihood: 0.9), Fact(2, 0, 10, 2, priority: 10, likelihood: 0.1)
                ])
            };

            CaseAnalyticsChartRenderer.Render(CaseAnalyticsChart.Gains, few, false)
                .Should().Contain("too few to rely on");
            CaseAnalyticsChartRenderer.Render(CaseAnalyticsChart.Calibration, few, false)
                .Should().Contain("too few to rely on");
            CaseAnalyticsChartRenderer.Render(CaseAnalyticsChart.Gains, report, false)
                .Should().NotContain("too few to rely on");
        }

        [Fact]
        public void TheRiskMatrixPutsTheMostLikelyAtTheTopAndSaysHowManyCasesAreNotAssessed()
        {
            var matrix = new CaseRiskMatrixDto
            {
                Total = 9,
                Cells =
                [
                    new CaseRiskMatrixCellDto { LikelihoodBand = 4, Impact = 5, Cases = 2 },
                    new CaseRiskMatrixCellDto { LikelihoodBand = 1, Impact = 1, Cases = 4 },
                    new CaseRiskMatrixCellDto { LikelihoodBand = 0, Impact = 0, Cases = 3 }
                ]
            };

            var svg = CaseAnalyticsChartRenderer.RiskMatrix(matrix, false);

            svg.Should().StartWith("<svg").And.Contain("Risk matrix").And.Contain("3 not yet assessed");
            svg.IndexOf("Very likely", StringComparison.Ordinal).Should()
                .BeLessThan(svg.IndexOf("Unlikely", StringComparison.Ordinal));

            var act = () => CaseAnalyticsChartRenderer.RiskMatrix(new CaseRiskMatrixDto(), false);
            act.Should().Throw<ArgumentException>();
        }
    }
}
