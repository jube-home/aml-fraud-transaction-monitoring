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

using FluentAssertions;
using Jube.Service.Query.CaseTriage;
using Xunit;

namespace Jube.Test.Service.Query.CaseTriage
{
    [Trait("Category", "Unit")]
    public sealed class QaDisagreementSamplingTests
    {
        [Theory]
        [InlineData(0.05, 3, 0.15)]
        [InlineData(0.5, 3, 1.0)]
        [InlineData(0.05, 1, 0.05)]
        [InlineData(0.05, 0.2, 0.05)]
        [InlineData(0, 3, 0)]
        public void TheRateIsMultipliedAndCappedAtOne(double rate, double factor, double expected)
        {
            QaDisagreementSampling.BoostedRate(rate, factor).Should().BeApproximately(expected, 1e-12);
        }
    }
}
