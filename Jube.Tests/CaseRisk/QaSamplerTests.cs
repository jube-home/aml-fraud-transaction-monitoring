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
using Jube.CaseRisk;
using Jube.CaseRisk.Analytics;
using Jube.Dto.Options;
using Xunit;
using static Jube.Test.CaseRisk.FactBuilder;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class QaSamplerTests
    {
        private static readonly Guid workflow = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly CaseRiskSettings settings = new();

        [Fact]
        public void TheDrawIsStableForACaseAndAlwaysInsideZeroAndOne()
        {
            var draws = Enumerable.Range(1, 2000).Select(id => QaSampler.Draw(workflow, id)).ToList();

            draws.Should().OnlyContain(d => d >= 0 && d < 1);
            QaSampler.Draw(workflow, 77).Should().Be(QaSampler.Draw(workflow, 77));
            QaSampler.Draw(workflow, 77).Should().NotBe(QaSampler.Draw(Guid.NewGuid(), 77));
            draws.Distinct().Count().Should().BeGreaterThan(1990);
        }

        [Theory]
        [InlineData(0.05)]
        [InlineData(0.2)]
        [InlineData(0.5)]
        public void ThePickedShareTracksTheConfiguredRate(double rate)
        {
            const int cases = 40_000;

            var picked = Enumerable.Range(1, cases).Count(id => QaSampler.Picked(workflow, id, rate));

            ((double)picked / cases).Should().BeApproximately(rate, 0.01);
        }

        [Fact]
        public void ARateOfNothingNeverPicksAndAWholeRateAlwaysDoes()
        {
            Enumerable.Range(1, 5000).Should().OnlyContain(id => !QaSampler.Picked(workflow, id, 0));
            Enumerable.Range(1, 5000).Should().OnlyContain(id => QaSampler.Picked(workflow, id, 1.0));
        }

        [Theory]
        [InlineData(CaseRiskLane.Standard, 10, true)]
        [InlineData(CaseRiskLane.Standard, 24, true)]
        [InlineData(CaseRiskLane.Standard, 25, false)]
        [InlineData(CaseRiskLane.Standard, 90, false)]
        [InlineData(CaseRiskLane.DeadlineNear, 5, false)]
        [InlineData(CaseRiskLane.Mandatory, 5, false)]
        [InlineData(CaseRiskLane.LiveMoney, 5, false)]
        public void OnlyLowPriorityStandardCasesAreEligible(CaseRiskLane lane, int priority, bool expected)
        {
            QaSampler.Eligible(lane, priority, settings).Should().Be(expected);
        }

        [Fact]
        public void TheReportCountsPickedOpenClosedAndDecidedCasesAndTheirConfirmedShare()
        {
            var facts = new[]
            {
                Fact(1, closedMinutes: 10, outcome: 1) with { QaSample = true },
                Fact(2, closedMinutes: 10, outcome: 1) with { QaSample = true },
                Fact(3, closedMinutes: 10, outcome: 2) with { QaSample = true },
                Fact(4, closedMinutes: 10, outcome: 2) with { QaSample = true },
                Fact(5, closedMinutes: 10, outcome: 0) with { QaSample = true },
                Fact(6) with { QaSample = true },
                Fact(7, closedMinutes: 10, outcome: 1),
                Fact(8, closedMinutes: 10, outcome: 1),
                Fact(9, closedMinutes: 10, outcome: 2),
                Fact(10)
            };

            var report = QaSampleCalculator.Compute(facts);

            report.Picked.Should().Be(6);
            report.Open.Should().Be(1);
            report.Closed.Should().Be(5);
            report.Decided.Should().Be(4);
            report.Confirmed.Should().Be(2);
            report.ConfirmedRate.Should().Be(0.5);
            report.OverallConfirmedRate.Should().BeApproximately(4 / 7.0, 1e-9);
            report.Enough.Should().BeFalse();
        }

        [Fact]
        public void TheReportIsEnoughOnceTenSampledCasesHaveAnOutcomeAndSaysNothingBeforeThat()
        {
            var empty = QaSampleCalculator.Compute([Fact(1)]);
            var ten = QaSampleCalculator.Compute(Enumerable.Range(1, 10)
                .Select(i => Fact(i, closedMinutes: 5, outcome: i % 2 == 0 ? 1 : 2) with { QaSample = true }).ToList());

            empty.Picked.Should().Be(0);
            empty.ConfirmedRate.Should().BeNull();
            empty.OverallConfirmedRate.Should().BeNull();
            empty.Enough.Should().BeFalse();
            ten.Decided.Should().Be(10);
            ten.Enough.Should().BeTrue();
            ten.ConfirmedRate.Should().Be(0.5);
        }

        [Theory]
        [InlineData(0.0, true)]
        [InlineData(0.5, true)]
        [InlineData(0.51, false)]
        [InlineData(-0.01, false)]
        [InlineData(double.NaN, false)]
        public void TheSampleRateOverrideMustBeBetweenZeroAndAHalf(double rate, bool valid)
        {
            CaseRiskSettingsResolver.Check(new CaseRiskOverrides { QaSampleRate = rate }).Should()
                .BeEquivalentTo(valid ? [] : new[] { CaseRiskSettingsProblem.QaSampleRateOutOfRange });
            CaseRiskSettingsResolver.Resolve(settings, new CaseRiskOverrides { QaSampleRate = 0.2 })
                .QaSampleRate.Should().Be(0.2);
            CaseRiskSettingsResolver.Resolve(settings, new CaseRiskOverrides()).QaSampleRate.Should().Be(0.05);
        }
    }
}
