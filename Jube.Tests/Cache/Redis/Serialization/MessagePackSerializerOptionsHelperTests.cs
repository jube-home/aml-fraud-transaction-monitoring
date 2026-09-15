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
using Jube.Cache.Redis.Serialization;
using Jube.Cache.Redis.Serialization.DictionaryNoBoxing.MessagePack;
using Jube.Dictionary;
using MessagePack;
using Xunit;

namespace Jube.Test.Cache.Redis.Serialization
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class MessagePackSerializerOptionsHelperTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EnveloperOptionsRoundTripAnEnvelopeDictionaryNoBoxingPayload(bool compression)
        {
            var options =
                MessagePackSerializerOptionsHelper.EnveloperMessagePackSerializerWithCompressionOptions(compression);

            var data = new DictionaryNoBoxing<int>();
            data.Add(1, "hello");
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = data };

            var bytes = MessagePackSerializer.Serialize(envelope, options);
            var deserialized = MessagePackSerializer.Deserialize<EnvelopeDictionaryNoBoxing<int>>(bytes, options);

            deserialized.Version.Should().Be(1);
            deserialized.Data.TryGetValue(1, out var value).Should().BeTrue();
            value.AsString().Should().Be("hello");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ContractlessStandardResolverOptionsRoundTripAPlainObject(bool compression)
        {
            var options = MessagePackSerializerOptionsHelper
                .ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(compression);

            var payload = new PlainObject { Name = "test", Value = 42 };
            var bytes = MessagePackSerializer.Serialize(payload, options);
            var deserialized = MessagePackSerializer.Deserialize<PlainObject>(bytes, options);

            deserialized.Name.Should().Be("test");
            deserialized.Value.Should().Be(42);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StandardOptionsRoundTripAContractObject(bool compression)
        {
            var options =
                MessagePackSerializerOptionsHelper.StandardMessagePackSerializerWithCompressionOptions(compression);

            var payload = new ContractObject { Name = "test", Value = 42 };
            var bytes = MessagePackSerializer.Serialize(payload, options);
            var deserialized = MessagePackSerializer.Deserialize<ContractObject>(bytes, options);

            deserialized.Name.Should().Be("test");
            deserialized.Value.Should().Be(42);
        }

        [Fact]
        public void CompressionMeaningfullyReducesSizeForARepetitivePayload()
        {
            var withoutCompression =
                MessagePackSerializerOptionsHelper
                    .ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(false);
            var withCompression =
                MessagePackSerializerOptionsHelper
                    .ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(true);

            var payload = new PlainObject { Name = new string('a', 5000), Value = 1 };

            var uncompressedBytes = MessagePackSerializer.Serialize(payload, withoutCompression);
            var compressedBytes = MessagePackSerializer.Serialize(payload, withCompression);

            compressedBytes.Length.Should().BeLessThan(uncompressedBytes.Length);
        }

        [Fact]
        public void CompressedAndUncompressedOptionsAreDistinctInstances()
        {
            var withCompression =
                MessagePackSerializerOptionsHelper.StandardMessagePackSerializerWithCompressionOptions(true);
            var withoutCompression =
                MessagePackSerializerOptionsHelper.StandardMessagePackSerializerWithCompressionOptions(false);

            withCompression.Should().NotBeSameAs(withoutCompression);
        }

        public class PlainObject
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        [MessagePackObject(AllowPrivate = true)]
        internal class ContractObject
        {
            [Key(0)] public string Name { get; set; } = string.Empty;
            [Key(1)] public int Value { get; set; }
        }
    }
}