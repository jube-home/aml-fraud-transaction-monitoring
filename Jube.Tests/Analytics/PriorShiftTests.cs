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
using Jube.Analytics;
using Xunit;

namespace Jube.Test.Analytics
{
    public class PriorShiftTests
    {
        [Fact]
        public void Apply_KnownAnswers()
        {
            PriorShift.Apply(0.8, 0.5, 0.01).Should().BeApproximately(0.03883495145631069, 1e-12);
            PriorShift.Apply(0.3, 0.4, 0.05).Should().BeApproximately(0.03272727272727273, 1e-12);
        }

        [Fact]
        public void Apply_EqualPriorsIsIdentity_AndEndpointsAreFixed()
        {
            PriorShift.Apply(0.37, 0.2, 0.2).Should().BeApproximately(0.37, 1e-12);
            PriorShift.Apply(0.0, 0.5, 0.01).Should().Be(0);
            PriorShift.Apply(1.0, 0.5, 0.01).Should().Be(1);
            double.IsNaN(PriorShift.Apply(double.NaN, 0.5, 0.01)).Should().BeTrue();
        }

        [Fact]
        public void Apply_IsStrictlyMonotone()
        {
            var mapped = PriorShift.Apply([0.1, 0.2, 0.5, 0.9], 0.5, 0.02);
            mapped.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        }

        [Theory]
        [InlineData(0.0, 0.5)]
        [InlineData(1.0, 0.5)]
        [InlineData(0.5, 0.0)]
        [InlineData(0.5, 1.0)]
        [InlineData(double.NaN, 0.5)]
        public void Apply_InvalidPriors_Throw(double training, double target)
        {
            Action act = () => PriorShift.Apply(0.5, training, target);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Apply_ProbabilityOutOfRange_Throws()
        {
            Action act = () => PriorShift.Apply(1.5, 0.5, 0.1);
            act.Should().Throw<ArgumentOutOfRangeException>();
            act = () => PriorShift.Apply(null!, 0.5, 0.1);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SyntheticPopulation_BalancedFitCorrectedToKnownBaseRate()
        {
            var (features, labels) = FoundationFixtures.Population();
            var truth = labels.Sum() / (double)labels.Length;
            truth.Should().BeApproximately(0.019383333333333332, 1e-12);

            var positives = Enumerable.Range(0, labels.Length).Where(i => labels[i] == 1).ToArray();

            var negatives = Enumerable.Range(0, labels.Length).Where(i => labels[i] == 0).Take(positives.Length)
                .ToArray();

            var balanced = positives.Concat(negatives).ToArray();
            balanced.Length.Should().Be(2326);

            var calibrator = PlattCalibration.Fit([.. balanced.Select(i => features[i])],
                [.. balanced.Select(i => labels[i])]);
            calibrator.Slope.Should().BeApproximately(3.006724618635352, 1e-6);
            calibrator.Intercept.Should().BeApproximately(-1.5078601871962334, 1e-6);

            var uncorrected = calibrator.Apply(features).Average();
            var atTwoPercent = calibrator.Apply(features, 0.5, 0.02).Average();
            var atTrueRate = calibrator.Apply(features, 0.5, truth).Average();

            uncorrected.Should().BeApproximately(0.2790385199419535, 1e-6);
            atTwoPercent.Should().BeApproximately(0.019514506998546313, 1e-6);
            atTrueRate.Should().BeApproximately(0.018987751009092384, 1e-6);
            Math.Abs(atTrueRate - truth).Should().BeLessThan(0.05 * truth);
            uncorrected.Should().BeGreaterThan(10 * truth);
        }
    }
}