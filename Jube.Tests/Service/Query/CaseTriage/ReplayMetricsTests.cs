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
    public sealed class ReplayMetricsTests
    {
        [Fact]
        public void EveryKindOfDisagreementIsCountedOnItsOwn()
        {
            var metrics = ReplayMetrics.Compute(
            [
                (1, 1), (2, 2), (1, 2), (1, 0), (2, 1), (2, 0), (1, null)
            ]);

            metrics.Scored.Should().Be(6);
            metrics.Unusable.Should().Be(1);
            metrics.Agreed.Should().Be(2);
            metrics.MissedRisk.Should().Be(2);
            metrics.OverEscalation.Should().Be(1);
            metrics.Inconclusive.Should().Be(2);
            metrics.AgreementRate.Should().BeApproximately(2d / 6, 1e-12);
        }

        [Fact]
        public void NothingScoredHasNoRate()
        {
            ReplayMetrics.Compute([]).AgreementRate.Should().BeNull();
            ReplayMetrics.Compute([(1, null)]).AgreementRate.Should().BeNull();
        }
    }
}
