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
using Jube.Data.Concurrency;
using Xunit;

namespace Jube.Test.Middlewares
{
    [Trait("Category", "Unit")]
    public sealed class OptimisticConcurrencyTests
    {
        [Theory]
        [InlineData(null, 3)]
        [InlineData(0, 3)]
        [InlineData(3, 3)]
        [InlineData(5, null)]
        public void AVersionThatIsAbsentOrMatchesIsAccepted(int? supplied, int? current)
        {
            if (supplied is > 0 && current is null)
            {
                var conflict = () => OptimisticConcurrency.Verify(supplied, current);

                conflict.Should().Throw<VersionConflictException>();

                return;
            }

            OptimisticConcurrency.Verify(supplied, current);
        }

        [Fact]
        public void AVersionThatHasMovedOnIsRefusedAndNamesBothVersions()
        {
            var conflict = () => OptimisticConcurrency.Verify(2, 4);

            var thrown = conflict.Should().Throw<VersionConflictException>().Which;

            thrown.SuppliedVersion.Should().Be(2);
            thrown.CurrentVersion.Should().Be(4);
            thrown.Message.Should().Contain("4").And.Contain("2");
        }
    }
}
