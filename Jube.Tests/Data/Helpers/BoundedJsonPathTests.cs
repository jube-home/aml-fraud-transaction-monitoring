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
using Jube.Data.Helpers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.DataHelpers
{
    [Trait("Category", "Unit")]
    public sealed class BoundedJsonPathTests
    {
        private static readonly JObject Document = JObject.Parse(
            "{\"a\":[{\"s\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaX\"}],\"b\":{\"c\":\"v\"}}");

        [Fact]
        public void APlainPathStillSelectsTheValue()
        {
            BoundedJsonPath.Select(Document, "$.b.c")!.ToString().Should().Be("v");
        }

        [Fact]
        public void ACatastrophicRegexFilterIsCutShortAndSelectsNothingInsteadOfHangingOrThrowing()
        {
            BoundedJsonPath.Select(Document, "$.a[?(@.s =~ /^(a+)+$/)]").Should().BeNull();
        }

        [Fact]
        public void ANullDocumentSelectsNothing()
        {
            BoundedJsonPath.Select(null, "$.b.c").Should().BeNull();
        }
    }
}
