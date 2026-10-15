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
using Jube.Service.Helpers.TextLists;
using Xunit;

namespace Jube.Test.Service.Helpers
{
    [Trait("Category", "Unit")]
    public sealed class TextListsToolsTests
    {
        [Fact]
        [Trait("Covers", "DistinctValues")]
        [Trait("Covers", "CountDistinctValues")]
        [Trait("Covers", "JoinValues")]
        public void DistinctAndJoin()
        {
            TextListsTools.DistinctValues(["a", " A ", "b", "a"]).Should().Equal("a", "A", "b");
            TextListsTools.DistinctValues(["a", " A ", "b"], true).Should().Equal("a", "b");
            TextListsTools.CountDistinctValues(["a", "A", "b"]).Should().Be(3);
            TextListsTools.CountDistinctValues(["a", "A", "b"], true).Should().Be(2);
            TextListsTools.JoinValues(["x", "y"]).Should().Be("x, y");
        }

        [Fact]
        [Trait("Covers", "UnionOfLists")]
        [Trait("Covers", "IntersectionOfLists")]
        [Trait("Covers", "DifferenceOfLists")]
        [Trait("Covers", "CompareLists")]
        public void SetOperations()
        {
            string[] june = ["dev-1", "dev-2", "dev-3"];
            string[] july = ["DEV-2", "dev-4"];
            TextListsTools.UnionOfLists(june, july, true).Should().Equal("dev-1", "dev-2", "dev-3", "dev-4");
            TextListsTools.IntersectionOfLists(june, july, true).Should().Equal("dev-2");
            TextListsTools.DifferenceOfLists(july, june, true).Should().Equal("dev-4");
            var comparison = TextListsTools.CompareLists(june, july, true);
            comparison.OnlyInFirst.Should().Equal("dev-1", "dev-3");
            comparison.OnlyInSecond.Should().Equal("dev-4");
            comparison.SameValues.Should().BeFalse();
            TextListsTools.CompareLists(["a", "b"], ["b", "a", "a"]).SameValues.Should().BeTrue();
        }
    }
}