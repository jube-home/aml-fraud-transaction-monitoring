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
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Reprocessing;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelManager.Reprocessing
{
    [Trait("Category", "Unit")]
    public sealed class ReprocessingPageLimitTests
    {
        [Theory]
        [InlineData("500", 500)]
        [InlineData("1", 1)]
        [InlineData("100000", 100000)]
        [InlineData("100001", 100000)]
        [InlineData("2147483647", 100000)]
        [InlineData("0", 10000)]
        [InlineData("-5", 10000)]
        [InlineData("", 10000)]
        [InlineData(null, 10000)]
        [InlineData("many", 10000)]
        [InlineData("99999999999", 10000)]
        public void ThePageSizeIsAlwaysPositiveAndBounded(string? setting, int expected)
        {
            ReprocessingPageLimit.Resolve(setting!).Should().Be(expected);
        }
    }
}
