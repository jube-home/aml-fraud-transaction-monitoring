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
using Jube.Cache.Redis.Serialization;
using Jube.Cache.Redis.Serialization.DictionaryNoBoxing.MessagePack;
using Jube.Dictionary.Models;
using MessagePack;
using Xunit;
using IntDictionaryNoBoxing = Jube.Dictionary.DictionaryNoBoxing<int>;
using StringDictionaryNoBoxing = Jube.Dictionary.DictionaryNoBoxing<string>;

namespace Jube.Test.Cache.Redis.Serialization.DictionaryNoBoxing
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class EnvelopeDictionaryNoBoxingMessagePackTests
    {
        private static readonly MessagePackSerializerOptions options =
            MessagePackSerializerOptionsHelper.EnveloperMessagePackSerializerWithCompressionOptions(false);

        public static TheoryData<DateTimeKind> DateTimeKinds => new()
        {
            DateTimeKind.Utc, DateTimeKind.Local, DateTimeKind.Unspecified
        };

        [Fact]
        public void RoundTripsAnEmptyIntKeyedDictionary()
        {
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = new IntDictionaryNoBoxing() };

            var bytes = MessagePackSerializer.Serialize(envelope, options);
            var deserialized = MessagePackSerializer.Deserialize<EnvelopeDictionaryNoBoxing<int>>(bytes, options);

            deserialized.Version.Should().Be(1);
            deserialized.Data.Count.Should().Be(0);
        }

        [Fact]
        public void RoundTripsAnEmptyStringKeyedDictionary()
        {
            var envelope = new EnvelopeDictionaryNoBoxing<string>
                { Version = 1, Data = new StringDictionaryNoBoxing() };

            var bytes = MessagePackSerializer.Serialize(envelope, options);
            var deserialized = MessagePackSerializer.Deserialize<EnvelopeDictionaryNoBoxing<string>>(bytes, options);

            deserialized.Version.Should().Be(1);
            deserialized.Data.Count.Should().Be(0);
        }

        [Fact]
        public void RoundTripsStringValueForIntKey()
        {
            var data = new IntDictionaryNoBoxing();
            data.Add(1, "hello world");
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = data };

            var deserialized = RoundTrip(envelope);

            deserialized.Data.TryGetValue(1, out var value).Should().BeTrue();
            value.Type.Should().Be(InternalValue.ValueType.String);
            value.AsString().Should().Be("hello world");
        }

        [Fact]
        public void RoundTripsStringValueForStringKey()
        {
            var data = new StringDictionaryNoBoxing();
            data.Add("field", "hello world");
            var envelope = new EnvelopeDictionaryNoBoxing<string> { Version = 1, Data = data };

            var deserialized = RoundTrip(envelope);

            deserialized.Data.TryGetValue("field", out var value).Should().BeTrue();
            value.AsString().Should().Be("hello world");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1)]
        public void RoundTripsSmallIntValues(int intValue)
        {
            var data = new IntDictionaryNoBoxing();
            data.Add(1, intValue);
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = data };

            var deserialized = RoundTrip(envelope);

            deserialized.Data.TryGetValue(1, out var value).Should().BeTrue();
            value.Type.Should().Be(InternalValue.ValueType.Int);
            value.AsInt().Should().Be(intValue);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(0d)]
        [InlineData(-123.456)]
        public void RoundTripsDoubleValuesIncludingSpecialValues(double doubleValue)
        {
            var data = new IntDictionaryNoBoxing();
            data.Add(1, doubleValue);
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = data };

            var deserialized = RoundTrip(envelope);

            deserialized.Data.TryGetValue(1, out var value).Should().BeTrue();
            value.Type.Should().Be(InternalValue.ValueType.Double);
            if (double.IsNaN(doubleValue))
            {
                double.IsNaN(value.AsDouble()).Should().BeTrue();
            }
            else
            {
                value.AsDouble().Should().Be(doubleValue);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RoundTripsBoolValues(bool boolValue)
        {
            var data = new IntDictionaryNoBoxing();
            data.Add(1, boolValue);
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = data };

            var deserialized = RoundTrip(envelope);

            deserialized.Data.TryGetValue(1, out var value).Should().BeTrue();
            value.Type.Should().Be(InternalValue.ValueType.Bool);
            value.AsBool().Should().Be(boolValue);
        }

        [Theory]
        [MemberData(nameof(DateTimeKinds))]
        public void RoundTripsDateTimeValuesRegardlessOfKind(DateTimeKind kind)
        {
            var original = DateTime.SpecifyKind(new DateTime(2024, 6, 15, 10, 30, 0), kind);
            var data = new IntDictionaryNoBoxing();
            data.Add(1, original);
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = data };

            var deserialized = RoundTrip(envelope);

            deserialized.Data.TryGetValue(1, out var value).Should().BeTrue();
            value.Type.Should().Be(InternalValue.ValueType.DateTime);
            value.AsDateTime().Should().Be(original.ToUniversalTime());
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue - 1)]
        [InlineData(int.MinValue + 1)]
        public void RoundTripsIntValuesAtTheInt32BoundaryWithoutBeingMisreadAsADateTime(int boundaryValue)
        {
            var data = new IntDictionaryNoBoxing();
            data.Add(1, boundaryValue);
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = data };

            var deserialized = RoundTrip(envelope);

            deserialized.Data.TryGetValue(1, out var value).Should().BeTrue();
            value.Type.Should().Be(InternalValue.ValueType.Int);
            value.AsInt().Should().Be(boundaryValue);
        }

        [Fact]
        public void RoundTripsMultipleMixedTypedEntriesInOneDictionary()
        {
            var data = new StringDictionaryNoBoxing();
            data.Add("s", "text");
            data.Add("i", 7);
            data.Add("d", 3.14);
            data.Add("b", true);
            data.Add("t", new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var envelope = new EnvelopeDictionaryNoBoxing<string> { Version = 2, Data = data };

            var deserialized = RoundTrip(envelope);

            deserialized.Version.Should().Be(2);
            deserialized.Data.Count.Should().Be(5);
            deserialized.Data.TryGetValue("s", out var s).Should().BeTrue();
            s.AsString().Should().Be("text");
            deserialized.Data.TryGetValue("i", out var i).Should().BeTrue();
            i.AsInt().Should().Be(7);
            deserialized.Data.TryGetValue("d", out var d).Should().BeTrue();
            d.AsDouble().Should().Be(3.14);
            deserialized.Data.TryGetValue("b", out var b).Should().BeTrue();
            b.AsBool().Should().BeTrue();
            deserialized.Data.TryGetValue("t", out var t).Should().BeTrue();
            t.AsDateTime().Should().Be(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void GuidTypedValueRoundTripsCorrectly()
        {
            var originalGuid = Guid.NewGuid();
            var data = new IntDictionaryNoBoxing();
            data.Add(1, new InternalValue(originalGuid));
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = data };

            var deserialized = RoundTrip(envelope);

            deserialized.Data.TryGetValue(1, out var value).Should().BeTrue();
            value.Type.Should().Be(InternalValue.ValueType.Guid);
            value.AsGuid().Should().Be(originalGuid);
        }

        [Fact]
        public void EmptyGuidRoundTripsCorrectly()
        {
            var data = new IntDictionaryNoBoxing();
            data.Add(1, new InternalValue(Guid.Empty));
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = data };

            var deserialized = RoundTrip(envelope);

            deserialized.Data.TryGetValue(1, out var value).Should().BeTrue();
            value.Type.Should().Be(InternalValue.ValueType.Guid);
            value.AsGuid().Should().Be(Guid.Empty);
        }

        [Fact]
        public void MultipleGuidTypedValuesInTheSameDictionaryEachRoundTripCorrectly()
        {
            var firstGuid = Guid.NewGuid();
            var secondGuid = Guid.NewGuid();
            var data = new IntDictionaryNoBoxing();
            data.Add(1, new InternalValue(firstGuid));
            data.Add(2, new InternalValue("not-a-guid"));
            data.Add(3, new InternalValue(secondGuid));
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = data };

            var deserialized = RoundTrip(envelope);

            deserialized.Data.TryGetValue(1, out var first).Should().BeTrue();
            first.Type.Should().Be(InternalValue.ValueType.Guid);
            first.AsGuid().Should().Be(firstGuid);

            deserialized.Data.TryGetValue(2, out var middle).Should().BeTrue();
            middle.Type.Should().Be(InternalValue.ValueType.String);
            middle.AsString().Should().Be("not-a-guid");

            deserialized.Data.TryGetValue(3, out var third).Should().BeTrue();
            third.Type.Should().Be(InternalValue.ValueType.Guid);
            third.AsGuid().Should().Be(secondGuid);
        }

        [Fact]
        public void GuidTypedValueRoundTripsCorrectlyForStringKeyedDictionaryAsWell()
        {
            var originalGuid = Guid.NewGuid();
            var data = new StringDictionaryNoBoxing();
            data.Add("id", new InternalValue(originalGuid));
            var envelope = new EnvelopeDictionaryNoBoxing<string> { Version = 1, Data = data };

            var deserialized = RoundTrip(envelope);

            deserialized.Data.TryGetValue("id", out var value).Should().BeTrue();
            value.Type.Should().Be(InternalValue.ValueType.Guid);
            value.AsGuid().Should().Be(originalGuid);
        }

        [Fact]
        public void EnvelopeWithNullDataRoundTripsBackToNullData()
        {
            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = null };

            var deserialized = RoundTrip(envelope);

            deserialized.Version.Should().Be(1);
            deserialized.Data.Should().BeNull();
        }

        [Fact]
        public void EnvelopeWithNullDataRoundTripsBackToNullDataForStringKeyedDictionaryAsWell()
        {
            var envelope = new EnvelopeDictionaryNoBoxing<string> { Version = 1, Data = null };

            var deserialized = RoundTrip(envelope);

            deserialized.Version.Should().Be(1);
            deserialized.Data.Should().BeNull();
        }

        [Fact]
        public void EnvelopeWithNullDataIsDistinguishableFromAnEmptyDataOnRoundTrip()
        {
            var nullDataEnvelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = null };
            var emptyDataEnvelope = new EnvelopeDictionaryNoBoxing<int>
                { Version = 1, Data = new IntDictionaryNoBoxing() };

            RoundTrip(nullDataEnvelope).Data.Should().BeNull();
            RoundTrip(emptyDataEnvelope).Data.Should().NotBeNull();
        }

        [Fact]
        public void GetFormatterForAnUnsupportedKeyTypeThrowsTypeInitializationException()
        {
            var act = () =>
                EnvelopeDictionaryNoBoxingResolver.Instance.GetFormatter<EnvelopeDictionaryNoBoxing<Guid>>();

            var exception = act.Should().Throw<TypeInitializationException>().Which;
            exception.InnerException.Should().BeOfType<NotSupportedException>();
        }

        private static EnvelopeDictionaryNoBoxing<int> RoundTrip(EnvelopeDictionaryNoBoxing<int> envelope)
        {
            var bytes = MessagePackSerializer.Serialize(envelope, options);
            return MessagePackSerializer.Deserialize<EnvelopeDictionaryNoBoxing<int>>(bytes, options);
        }

        private static EnvelopeDictionaryNoBoxing<string> RoundTrip(EnvelopeDictionaryNoBoxing<string> envelope)
        {
            var bytes = MessagePackSerializer.Serialize(envelope, options);
            return MessagePackSerializer.Deserialize<EnvelopeDictionaryNoBoxing<string>>(bytes, options);
        }
    }
}