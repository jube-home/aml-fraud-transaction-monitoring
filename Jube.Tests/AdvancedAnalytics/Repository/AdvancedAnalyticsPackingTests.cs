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
using Xunit;
using Jube.Analytics.Datasets;

namespace Jube.Test.AdvancedAnalytics.Repository
{
    public sealed class AdvancedAnalyticsPackingTests
    {
        [Fact]
        public void PackIsLittleEndianEightBytesPerValue()
        {
            var packed = AdvancedAnalyticsPacking.Pack([1.0]);

            packed.Should().Equal(0, 0, 0, 0, 0, 0, 0xF0, 0x3F);
        }

        [Fact]
        public void UnpackRejectsUnknownVersionAndRaggedLength()
        {
            var unknown = () => AdvancedAnalyticsPacking.Unpack(new byte[8], 2);
            var ragged = () => AdvancedAnalyticsPacking.Unpack(new byte[7], AdvancedAnalyticsPacking.CurrentVersion);

            unknown.Should().Throw<NotSupportedException>();
            ragged.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void EmptyArrayRoundTrips()
        {
            AdvancedAnalyticsPacking.Unpack(AdvancedAnalyticsPacking.Pack([]),
                AdvancedAnalyticsPacking.CurrentVersion).Should().BeEmpty();
        }
    }
}