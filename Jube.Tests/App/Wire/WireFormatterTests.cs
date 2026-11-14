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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.App.Wire;
using MessagePack;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireFormatterTests
    {
        private static T RoundTrip<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(
                MessagePackSerializer.Serialize(value, WireSerializer.Options), WireSerializer.Options);
        }

        [Theory]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Local)]
        public void DateTimeAlwaysComesBackAsTheSameUtcInstant(DateTimeKind kind)
        {
            var source = DateTime.SpecifyKind(new DateTime(2026, 11, 5, 13, 30, 15, 250), kind);
            var expected = kind switch
            {
                DateTimeKind.Local => source.ToUniversalTime(),
                _ => DateTime.SpecifyKind(source, DateTimeKind.Utc)
            };

            var result = RoundTrip(source);

            result.Kind.Should().Be(DateTimeKind.Utc);
            result.Should().Be(expected);
        }

        [Fact]
        public void DateTimeOffsetComesBackAsTheSameInstantAtZeroOffset()
        {
            var source = new DateTimeOffset(2026, 11, 5, 13, 30, 15, TimeSpan.FromHours(2));

            var result = RoundTrip(source);

            result.Should().Be(source);
            result.Offset.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void NullableDateTimeOffsetKeepsNull()
        {
            RoundTrip<DateTimeOffset?>(null).Should().BeNull();
        }

        [Fact]
        public void DynamicCellsOfTheTypesJsonWritesAsTextComeOutAsExactlyThatText()
        {
            var web = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
            object[] cells =
            [
                new byte[] { 1, 2, 250 }, TimeSpan.FromSeconds(90.5), new DateOnly(2026, 10, 13),
                new TimeOnly(13, 30, 15), new TimeOnly(13, 30, 15, 123)
            ];

            foreach (var cell in cells)
            {
                var expected = System.Text.Json.JsonSerializer.Deserialize<string>(
                    System.Text.Json.JsonSerializer.Serialize(cell, web));

                RoundTrip<object>(cell).Should().Be(expected, cell.GetType().Name);
            }
        }

        [Fact]
        public void ADatabaseNullInADynamicCellIsWrittenAsNull()
        {
            RoundTrip<object>(DBNull.Value).Should().BeNull();
        }

        [Fact]
        public void NullableValuesAreWrittenExactlyAsTheirNonNullableForm()
        {
            var moment = new DateTimeOffset(2026, 11, 5, 13, 30, 15, TimeSpan.FromHours(2));

            Bytes<DateTimeOffset?>(moment).Should().Equal(Bytes(moment));
            Bytes<DateTime?>(moment.UtcDateTime).Should().Equal(Bytes(moment.UtcDateTime));
            Bytes<double?>(1.5).Should().Equal(Bytes(1.5));
            Bytes<decimal?>(2.25m).Should().Equal(Bytes(2.25m));
            Bytes<char?>('x').Should().Equal(Bytes('x'));
        }

        [Fact]
        public void NullableValuesRoundTripAndKeepNull()
        {
            var moment = new DateTimeOffset(2026, 11, 5, 13, 30, 15, TimeSpan.FromHours(2));

            RoundTrip<DateTimeOffset?>(moment).Should().Be(moment);
            RoundTrip<DateTime?>(null).Should().BeNull();
            RoundTrip<double?>(null).Should().BeNull();
            RoundTrip<decimal?>(2.25m).Should().Be(2.25m);
            RoundTrip<char?>('x').Should().Be('x');
        }

        [Fact]
        public void NonFiniteNullableDoubleIsRejectedOnRead()
        {
            var bytes = Bytes(double.NaN);

            var act = () => MessagePackSerializer.Deserialize<double?>(bytes, WireSerializer.Options);

            act.Should().Throw<MessagePackSerializationException>();
        }

        private static byte[] Bytes<T>(T value)
        {
            return MessagePackSerializer.Serialize(value, WireSerializer.Options);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void NonFiniteDoubleIsRejectedOnRead(double value)
        {
            var bytes = MessagePackSerializer.Serialize(value, WireSerializer.Options);

            var act = () => MessagePackSerializer.Deserialize<double>(bytes, WireSerializer.Options);

            act.Should().Throw<MessagePackSerializationException>();
        }

        [Fact]
        public void IntegerIsAcceptedWhereADoubleIsExpected()
        {
            var bytes = MessagePackSerializer.Serialize(7, WireSerializer.Options);

            MessagePackSerializer.Deserialize<double>(bytes, WireSerializer.Options).Should().Be(7d);
        }

        [Fact]
        public void DecimalTravelsAsANumber()
        {
            var bytes = MessagePackSerializer.Serialize(12.5m, WireSerializer.Options);

            new MessagePackReader(bytes).NextMessagePackType.Should().Be(MessagePackType.Float);
            MessagePackSerializer.Deserialize<decimal>(bytes, WireSerializer.Options).Should().Be(12.5m);
        }

        [Fact]
        public void RowDictionariesCarryMixedRuntimeValues()
        {
            var when = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var id = Guid.NewGuid();
            var source = new List<Dictionary<string, object>>
            {
                new()
                {
                    ["text"] = "a",
                    ["count"] = 5L,
                    ["ratio"] = 1.5d,
                    ["flag"] = true,
                    ["when"] = when,
                    ["id"] = id,
                    ["nothing"] = null,
                    ["money"] = 3.25m
                }
            };

            var result = RoundTrip(source)[0];

            result["text"].Should().Be("a");
            result["count"].Should().Be(5L);
            result["ratio"].Should().Be(1.5d);
            result["flag"].Should().Be(true);
            result["when"].Should().Be(when);
            result["id"].Should().Be(id.ToString());
            result["nothing"].Should().BeNull();
            result["money"].Should().Be(3.25d);
        }

        [Fact]
        public void UntypedMapsComeBackAsStringKeyedDictionaries()
        {
            var source = new Dictionary<string, object>
            {
                ["inner"] = new Dictionary<string, object> { ["list"] = new List<object> { 1L, "two" } }
            };

            var result = RoundTrip(source);

            var inner = (Dictionary<string, object>)result["inner"];
            ((List<object>)inner["list"]).Should().Equal(1L, "two");
        }

        [Fact]
        public void ACharTravelsAsAOneCharacterStringLikeJson()
        {
            var bytes = MessagePackSerializer.Serialize('h', WireSerializer.Options);

            new MessagePackReader(bytes).NextMessagePackType.Should().Be(MessagePackType.String);
            MessagePackSerializer.Deserialize<char>(bytes, WireSerializer.Options).Should().Be('h');
        }

        [Fact]
        public void AMultiCharacterStringIsRejectedWhereACharIsExpected()
        {
            var bytes = MessagePackSerializer.Serialize("hh", WireSerializer.Options);

            var act = () => MessagePackSerializer.Deserialize<char>(bytes, WireSerializer.Options);

            act.Should().Throw<MessagePackSerializationException>();
        }

        [Fact]
        public void ColumnarRowsRestoreEveryRowExactlyIncludingMissingKeysAndNullRows()
        {
            var source = new List<Dictionary<string, object>>
            {
                new() { ["id"] = 1L, ["name"] = "a", ["note"] = null },
                new() { ["id"] = 2L, ["name"] = "b" },
                null,
                new() { ["extra"] = true, ["id"] = 4L }
            };

            var result = RoundTrip(source);

            result.Should().HaveCount(4);
            result[0].Should().Equal(source[0]);
            result[1].Should().Equal(source[1]);
            result[1].ContainsKey("note").Should().BeFalse();
            result[2].Should().BeNull();
            result[3].Keys.Should().BeEquivalentTo("extra", "id");
        }

        [Fact]
        public void ColumnarRowsKeepTheRowCountOfEmptyDictionariesAndOfNoRows()
        {
            RoundTrip(new List<Dictionary<string, object>> { new(), new(), new() }).Should().HaveCount(3);
            RoundTrip(new List<Dictionary<string, object>>()).Should().BeEmpty();
            RoundTrip<List<Dictionary<string, object>>>(null).Should().BeNull();
        }

        [Fact]
        public void ColumnarRowsKeepColumnOrderAndTypedStringValues()
        {
            var source = new List<Dictionary<string, string>> { new() { ["z"] = "1", ["a"] = null } };

            RoundTrip(source)[0].Keys.Should().Equal("z", "a");
            RoundTrip(source)[0]["a"].Should().BeNull();
        }

        [Fact]
        public void ColumnarRowsAreMuchSmallerThanTheSameRowsWrittenAsMaps()
        {
            var rows = Enumerable.Range(0, 1000).Select(i => new Dictionary<string, object>
            {
                ["transactionIdentifier"] = (long)i,
                ["counterpartyName"] = "ACME",
                ["amountInSettlementCurrency"] = i * 1.5d,
                ["flagged"] = i % 2 == 0
            }).ToList();

            var columnar = MessagePackSerializer.Serialize(rows, WireSerializer.Options).Length;
            var rowWise = MessagePackSerializer.Serialize(rows.ToArray(), WireSerializer.Options).Length;

            columnar.Should().BeLessThan(rowWise * 6 / 10);
        }

        [Theory]
        [InlineData(new byte[] { 0x95, 0x02, 0x91, 0xa1, 0x61, 0x91, 0x91, 0x01, 0xc0, 0xc0 })]
        [InlineData(new byte[] { 0x95, 0xd2, 0xff, 0xff, 0xff, 0xff, 0x90, 0x90, 0xc0, 0xc0 })]
        [InlineData(new byte[] { 0x95, 0xce, 0x7f, 0xff, 0xff, 0xff, 0x90, 0x90, 0xc0, 0xc0 })]
        [InlineData(new byte[] { 0x94, 0x00, 0x90, 0x90, 0xc0 })]
        [InlineData(new byte[] { 0x95, 0x01, 0x91, 0xa1, 0x61, 0x91, 0x91, 0x01, 0x91, 0x92, 0x05, 0x90, 0xc0 })]
        public void MalformedColumnarRowsAreRejected(byte[] bytes)
        {
            var act = () => MessagePackSerializer.Deserialize<List<Dictionary<string, object>>>(bytes,
                WireSerializer.Options);

            act.Should().Throw<MessagePackSerializationException>();
        }

        private const string GoldenColumnarRows =
            "950493a26964a46e616d65a5657874726193940102c00494a161c0c0a17a94c0c0c0c3929201910192029200019102";

        private static List<Dictionary<string, object>> GoldenRows()
        {
            return
            [
                new Dictionary<string, object> { ["id"] = 1L, ["name"] = "a" },
                new Dictionary<string, object> { ["id"] = 2L },
                null,
                new Dictionary<string, object> { ["name"] = "z", ["id"] = 4L, ["extra"] = true }
            ];
        }

        [Fact]
        public void ColumnarRowsWriteExactlyTheBytesTheBrowserCodecWrites()
        {
            var bytes = MessagePackSerializer.Serialize(GoldenRows(), WireSerializer.Options);

            Convert.ToHexString(bytes).ToLowerInvariant().Should().Be(GoldenColumnarRows);
        }

        [Fact]
        public void ColumnarRowsReadTheBytesTheBrowserCodecWrites()
        {
            var rows = MessagePackSerializer.Deserialize<List<Dictionary<string, object>>>(
                Convert.FromHexString(GoldenColumnarRows), WireSerializer.Options);

            rows.Should().HaveCount(4);
            rows[0].Should().Equal(new Dictionary<string, object> { ["id"] = 1L, ["name"] = "a" });
            rows[1].Should().Equal(new Dictionary<string, object> { ["id"] = 2L });
            rows[2].Should().BeNull();
            rows[3].Should().Equal(new Dictionary<string, object> { ["id"] = 4L, ["name"] = "z", ["extra"] = true });
        }

        [Fact]
        public void ExcessiveNestingIsRejected()
        {
            var bytes = new byte[200];
            Array.Fill(bytes, (byte)0x91);
            bytes[^1] = 0xc0;

            var act = () => MessagePackSerializer.Deserialize<object>(bytes, WireSerializer.Options);

            act.Should().Throw<MessagePackSerializationException>();
        }
    }
}
