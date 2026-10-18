/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using FluentAssertions;
using Jube.Validations;
using Xunit;

namespace Jube.Test.Validations
{
    [Trait("Category", "Unit")]
    public sealed class JsonTextTests
    {
        [Fact]
        public void NullOrWhiteSpaceIsAcceptable()
        {
            JsonText.IsAcceptable(null).Should().BeTrue();
            JsonText.IsAcceptable("   ").Should().BeTrue();
        }

        [Fact]
        public void OrdinaryJsonIsAcceptable()
        {
            JsonText.IsAcceptable("{\"a\":1,\"b\":[\"c\",\"d\"]}").Should().BeTrue();
        }

        [Fact]
        public void InvalidJsonIsRejected()
        {
            JsonText.IsAcceptable("{not json").Should().BeFalse();
        }

        [Fact]
        public void NullCodePointEscapeIsRejected()
        {
            JsonText.IsAcceptable("{\"a\":\"\\u0000\"}").Should().BeFalse();
        }

        [Fact]
        public void ProperlyPairedSurrogateEscapeIsAcceptable()
        {
            JsonText.IsAcceptable("{\"a\":\"\\ud83d\\ude00\"}").Should().BeTrue();
        }

        [Fact]
        public void LoneHighSurrogateEscapeIsRejected()
        {
            JsonText.IsAcceptable("{\"a\":\"\\ud800\"}").Should().BeFalse();
        }

        [Fact]
        public void LoneLowSurrogateEscapeIsRejected()
        {
            JsonText.IsAcceptable("{\"a\":\"\\udc00\"}").Should().BeFalse();
        }

        [Fact]
        public void HighSurrogateFollowedByNonSurrogateEscapeIsRejected()
        {
            JsonText.IsAcceptable("{\"a\":\"\\ud800\\u0041\"}").Should().BeFalse();
        }
    }
}