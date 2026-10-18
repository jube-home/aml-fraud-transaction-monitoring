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
using Jube.Service.Extensions.EncodingAndHashing;
using Jube.Service.Extensions.EncodingAndHashing.Models;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class EncodingAndHashingToolsTests
    {
        [Theory]
        [InlineData("Base64", "Jübe", "SsO8YmU=")]
        [InlineData("Base64Url", "hello?>", "aGVsbG8_Pg")]
        [InlineData("Hex", "Jübe", "4ac3bc6265")]
        [InlineData("Url", "a b&c", "a+b%26c")]
        [InlineData("Html", "<b>", "&lt;b&gt;")]
        [Trait("Covers", "EncodeText")]
        [Trait("Covers", "DecodeText")]
        public void EncodeAndDecodeRoundTrip(string scheme, string plain, string encoded)
        {
            EncodingAndHashingTools.EncodeText(plain, scheme).Should().Be(encoded);
            EncodingAndHashingTools.DecodeText(encoded, scheme).Should().Be(plain);
        }

        [Fact]
        [Trait("Covers", "EncodeText")]
        [Trait("Covers", "DecodeText")]
        public void OneWayAndMalformedEncodings()
        {
            EncodingAndHashingTools.EncodeText("a\"b", "JavaScript").Should().Be("a\\\"b");
            EncodingAndHashingTools.EncodeText("<a & 'b'>", "XmlEscape").Should().Be("&lt;a &amp; &apos;b&apos;&gt;");
            var oneWay = () => EncodingAndHashingTools.DecodeText("x", "XmlEscape");
            oneWay.Should().Throw<ArgumentException>();
            var malformed = () => EncodingAndHashingTools.DecodeText("!!!", "Base64");
            malformed.Should().Throw<ArgumentException>().WithMessage("*not valid Base64*");
        }

        [Fact]
        [Trait("Covers", "HashText")]
        [Trait("Covers", "HmacText")]
        [Trait("Covers", "Crc32Checksum")]
        [Trait("Covers", "HashesMatch")]
        public void DigestsAndChecksums()
        {
            EncodingAndHashingTools.HashText("abc").Should()
                .Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");

            EncodingAndHashingTools.HashText("abc", "md5").Should().Be("900150983cd24fb0d6963f7d28e17f72");
            EncodingAndHashingTools.HmacText("The quick brown fox jumps over the lazy dog", "key")
                .Should().Be("f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8");

            EncodingAndHashingTools.Crc32Checksum("123456789").Should().Be("cbf43926");

            EncodingAndHashingTools
                .HashesMatch("abc", "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD").Should()
                .BeTrue();

            var act = () => EncodingAndHashingTools.HashText("abc", "Sha3");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "DescribeUuid")]
        public void DescribeUuidReportsVersion()
        {
            EncodingAndHashingTools.DescribeUuid("F47AC10B-58CC-4372-A567-0E02B2C3D479")
                .Should().Be(new UuidDetails(true, "f47ac10b-58cc-4372-a567-0e02b2c3d479", 4));

            EncodingAndHashingTools.DescribeUuid("nope").IsValid.Should().BeFalse();
        }
    }
}