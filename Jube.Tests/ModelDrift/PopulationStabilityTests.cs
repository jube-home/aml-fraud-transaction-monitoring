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
using Jube.Service.Query.ModelDrift;
using Xunit;

namespace Jube.Test.ModelDrift
{
    [Trait("Category", "Unit")]
    public sealed class PopulationStabilityTests
    {
        private static double[] Uniform(int count, double scale = 1, double shift = 0)
        {
            return Enumerable.Range(0, count).Select(i => shift + scale * i / count).ToArray();
        }

        [Fact]
        public void IdenticalDistributionsHaveNoDrift()
        {
            PopulationStability.Compute(Uniform(1000), Uniform(1000)).Should().BeApproximately(0, 1e-9);
        }

        [Fact]
        public void ShiftedDistributionIsSignificant()
        {
            var psi = PopulationStability.Compute(Uniform(1000), Uniform(1000, 1, 0.6));

            psi.Should().BeGreaterThan(PopulationStability.SignificantThreshold);
            PopulationStability.Status(psi).Should().Be(3);
        }

        [Fact]
        public void SlightlyShiftedDistributionIsModerateOrStable()
        {
            var psi = PopulationStability.Compute(Uniform(1000), Uniform(1000, 1, 0.01));

            PopulationStability.Status(psi).Should().BeLessThan(3);
        }

        [Fact]
        public void PsiIsNeverNegativeAndIsFinite()
        {
            var psi = PopulationStability.Compute(Uniform(500), Uniform(300, 0.2, 5));

            psi.Should().BeGreaterThanOrEqualTo(0);
            double.IsFinite(psi).Should().BeTrue();
        }

        [Fact]
        public void ConstantBaselineDoesNotThrow()
        {
            var psi = PopulationStability.Compute(Enumerable.Repeat(1d, 100).ToArray(), Uniform(100, 2));

            double.IsFinite(psi).Should().BeTrue();
        }

        [Fact]
        public void EmptyWindowIsRefused()
        {
            var act = () => PopulationStability.Compute([], Uniform(10));

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(0.0, 1)]
        [InlineData(0.099, 1)]
        [InlineData(0.1, 2)]
        [InlineData(0.249, 2)]
        [InlineData(0.25, 3)]
        [InlineData(5.0, 3)]
        public void StatusFollowsThresholds(double psi, int expected)
        {
            PopulationStability.Status(psi).Should().Be((short)expected);
        }
    }
}
