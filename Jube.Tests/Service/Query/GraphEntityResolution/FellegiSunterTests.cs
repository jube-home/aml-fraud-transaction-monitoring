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
using FluentAssertions;
using Jube.Service.Query.Graph.Resolution;
using Xunit;

namespace Jube.Test.Service.Query.GraphEntityResolution
{
    [Trait("Category", "Unit")]
    public sealed class FellegiSunterTests
    {
        [Fact]
        public void WeightsAreTheBaseTwoLogRatiosOfTheProbabilities()
        {
            FellegiSunter.AgreementWeight(0.95, 0.01).Should().BeApproximately(Math.Log2(95), 1e-12);
            FellegiSunter.AgreementWeight(0.95, 0.01).Should().BeApproximately(6.5698, 1e-3);
            FellegiSunter.DisagreementWeight(0.95, 0.01).Should().BeApproximately(Math.Log2(0.05 / 0.99), 1e-12);
            FellegiSunter.DisagreementWeight(0.95, 0.01).Should().BeApproximately(-4.3074, 1e-3);
        }

        [Fact]
        public void ARareValueAgreesWithMoreWeightThanACommonOne()
        {
            var rare = FellegiSunter.AgreementWeight(0.95, FellegiSunter.TermFrequencyU(0.0001));
            var common = FellegiSunter.AgreementWeight(0.95, FellegiSunter.TermFrequencyU(0.05));

            rare.Should().BeGreaterThan(common);
            (rare - common).Should().BeApproximately(Math.Log2(0.05 / 0.0001), 1e-9);
        }

        [Fact]
        public void ThePosteriorIsThePriorOddsTimesTwoToTheTotalWeight()
        {
            FellegiSunter.Posterior(0.5, [0]).Should().BeApproximately(0.5, 1e-12);
            FellegiSunter.Posterior(0.5, [1]).Should().BeApproximately(2d / 3, 1e-12);
            FellegiSunter.Posterior(0.5, [1, 1, -2]).Should().BeApproximately(0.5, 1e-12);
            FellegiSunter.Posterior(0.01, [Math.Log2(99)]).Should().BeApproximately(0.5, 1e-9);
        }

        [Fact]
        public void ExtremesAreClampedAndNeverNaN()
        {
            FellegiSunter.Posterior(0.5, [500]).Should().Be(1);
            FellegiSunter.Posterior(0.5, [-500]).Should().Be(0);
            double.IsFinite(FellegiSunter.AgreementWeight(1, 0)).Should().BeTrue();
            double.IsFinite(FellegiSunter.DisagreementWeight(1, 1)).Should().BeTrue();
            FellegiSunter.Posterior(0, [0]).Should().BeInRange(0, 1);
            FellegiSunter.Posterior(1, [0]).Should().BeInRange(0, 1);
        }

        [Fact]
        public void MoreAgreementNeverLowersTheProbability()
        {
            var one = FellegiSunter.Posterior(1e-4, [8]);
            var two = FellegiSunter.Posterior(1e-4, [8, 5]);

            two.Should().BeGreaterThan(one);
        }
    }
}
