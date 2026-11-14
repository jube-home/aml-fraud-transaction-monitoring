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

using System.Linq;
using FluentAssertions;
using Jube.CaseRisk;
using Xunit;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class CaseRiskSettingsResolverTests
    {
        private static readonly CaseRiskSettings defaults = new();

        [Fact]
        public void NoOverridesLeavesTheEnvironmentDefaultsUntouched()
        {
            CaseRiskSettingsResolver.Resolve(defaults, null).Should().BeSameAs(defaults);
            CaseRiskSettingsResolver.Resolve(defaults, new CaseRiskOverrides()).Should().BeEquivalentTo(defaults);
        }

        [Fact]
        public void AnOverrideReplacesOnlyTheSettingItNamesAndTheRestKeepTheDefaults()
        {
            var resolved = CaseRiskSettingsResolver.Resolve(defaults, new CaseRiskOverrides
            {
                BaseRatePrior = 0.4, ImpactWeights = " 2, 4,8 ,16,32 ", DeadlineLaneDays = 5
            });

            resolved.BaseRatePrior.Should().Be(0.4);
            resolved.ImpactWeights.Should().Equal(2, 4, 8, 16, 32);
            resolved.DeadlineLaneDays.Should().Be(5);
            resolved.Kappa.Should().Be(defaults.Kappa);
            resolved.Rho.Should().Be(defaults.Rho);
            resolved.PriorStrength.Should().Be(defaults.PriorStrength);
            resolved.MoneyBandThresholds.Should().Equal(defaults.MoneyBandThresholds);
            resolved.KeyHistoryHalfLifeDays.Should().Be(defaults.KeyHistoryHalfLifeDays);
        }

        [Theory]
        [InlineData("1,2,3,4")]
        [InlineData("1,2,3,4,5,6")]
        [InlineData("1,two,3,4,5")]
        [InlineData("1,2,NaN,4,5")]
        [InlineData("")]
        public void AListThatCannotBeReadFallsBackToTheDefaultAndNeverThrows(string text)
        {
            CaseRiskSettingsResolver.Resolve(defaults, new CaseRiskOverrides { ImpactWeights = text })
                .ImpactWeights.Should().Equal(defaults.ImpactWeights);
        }

        [Fact]
        public void ValidOverridesHaveNoProblems()
        {
            CaseRiskSettingsResolver.Check(new CaseRiskOverrides
            {
                Kappa = 1, Rho = 0, PriorStrength = 10_000, BaseRatePrior = 0.001, DeadlineLaneDays = 0,
                KeyHistoryHalfLifeDays = 3650, ImpactWeights = "0.5,1,2,3,4", MoneyBands = "1,2,3,4"
            }).Should().BeEmpty();

            CaseRiskSettingsResolver.Check(null).Should().BeEmpty();
            CaseRiskSettingsResolver.Check(new CaseRiskOverrides()).Should().BeEmpty();
        }

        [Theory]
        [InlineData(0, CaseRiskSettingsProblem.KappaOutOfRange)]
        [InlineData(1.0001, CaseRiskSettingsProblem.KappaOutOfRange)]
        [InlineData(double.NaN, CaseRiskSettingsProblem.KappaOutOfRange)]
        [InlineData(double.PositiveInfinity, CaseRiskSettingsProblem.KappaOutOfRange)]
        public void KappaMustBeAboveZeroAndNoMoreThanOne(double kappa, CaseRiskSettingsProblem expected)
        {
            CaseRiskSettingsResolver.Check(new CaseRiskOverrides { Kappa = kappa }).Should().Equal(expected);
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        [InlineData(double.NaN)]
        public void RhoMustBeBetweenZeroAndOne(double rho)
        {
            CaseRiskSettingsResolver.Check(new CaseRiskOverrides { Rho = rho })
                .Should().Equal(CaseRiskSettingsProblem.RhoOutOfRange);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10_000.5)]
        public void PriorStrengthMustBeAboveZeroAndNoMoreThanTenThousand(double strength)
        {
            CaseRiskSettingsResolver.Check(new CaseRiskOverrides { PriorStrength = strength })
                .Should().Equal(CaseRiskSettingsProblem.PriorStrengthOutOfRange);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0.0009)]
        [InlineData(0.9991)]
        [InlineData(1)]
        public void BaseRatePriorMustStayAwayFromCertainty(double prior)
        {
            CaseRiskSettingsResolver.Check(new CaseRiskOverrides { BaseRatePrior = prior })
                .Should().Equal(CaseRiskSettingsProblem.BaseRatePriorOutOfRange);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(366)]
        public void TheDeadlineWindowMustBeBetweenZeroAndAYear(int days)
        {
            CaseRiskSettingsResolver.Check(new CaseRiskOverrides { DeadlineLaneDays = days })
                .Should().Equal(CaseRiskSettingsProblem.DeadlineLaneDaysOutOfRange);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3651)]
        public void TheKeyHistoryHalfLifeMustBeAboveZeroAndNoMoreThanTenYears(double days)
        {
            CaseRiskSettingsResolver.Check(new CaseRiskOverrides { KeyHistoryHalfLifeDays = days })
                .Should().Equal(CaseRiskSettingsProblem.KeyHistoryHalfLifeOutOfRange);
        }

        [Theory]
        [InlineData("1,2,3,4")]
        [InlineData("1,2,3,4,5,6")]
        [InlineData("1,2,2,4,5")]
        [InlineData("5,4,3,2,1")]
        [InlineData("0,1,2,3,4")]
        [InlineData("-1,1,2,3,4")]
        [InlineData("a,b,c,d,e")]
        public void ImpactWeightsMustBeFivePositiveStrictlyIncreasingNumbers(string weights)
        {
            CaseRiskSettingsResolver.Check(new CaseRiskOverrides { ImpactWeights = weights })
                .Should().Equal(CaseRiskSettingsProblem.ImpactWeightsInvalid);
        }

        [Theory]
        [InlineData("1,2,3")]
        [InlineData("1,2,3,4,5")]
        [InlineData("4,3,2,1")]
        [InlineData("0,1,2,3")]
        [InlineData("1,1,2,3")]
        public void MoneyBandsMustBeFourPositiveStrictlyIncreasingNumbers(string bands)
        {
            CaseRiskSettingsResolver.Check(new CaseRiskOverrides { MoneyBands = bands })
                .Should().Equal(CaseRiskSettingsProblem.MoneyBandsInvalid);
        }

        [Fact]
        public void EveryProblemIsReportedAtOnce()
        {
            CaseRiskSettingsResolver.Check(new CaseRiskOverrides
            {
                Kappa = 0, Rho = 2, PriorStrength = 0, BaseRatePrior = 0, DeadlineLaneDays = -1,
                KeyHistoryHalfLifeDays = 0, ImpactWeights = "1", MoneyBands = "1"
            }).Select(p => (int)p).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8);
        }

        [Fact]
        public void TheOverriddenSettingsChangeTheLikelihoodTheWholeCalculationProduces()
        {
            var inputs = new CaseRiskInputs(0, 0, [], new ImpactInputs(3, null),
                new LaneInputs(new System.DateTime(2026, 10, 23, 0, 0, 0, System.DateTimeKind.Utc)));

            var plain = CaseRiskCalculator.Assess(inputs, defaults);
            var raised = CaseRiskCalculator.Assess(inputs,
                CaseRiskSettingsResolver.Resolve(defaults, new CaseRiskOverrides
                {
                    BaseRatePrior = 0.4, ImpactWeights = "1,3,10,30,200"
                }));

            plain.Likelihood.Probability.Should().BeApproximately(0.08, 1e-9);
            raised.Likelihood.Probability.Should().BeApproximately(0.4, 1e-9);
            raised.Priority.Should().BeGreaterThan(plain.Priority);
        }
    }
}
