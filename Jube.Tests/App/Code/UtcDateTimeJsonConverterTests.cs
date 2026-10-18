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
using System.Text.Json;
using FluentAssertions;
using Jube.App.Code;
using Xunit;

namespace Jube.Test.App.Code
{
    [Trait("Category", "Unit")]
    public sealed class UtcDateTimeJsonConverterTests
    {
        private static readonly JsonSerializerOptions options = BuildOptions();

        private static JsonSerializerOptions BuildOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            options.Converters.Add(new UtcDateTimeJsonConverter());

            return options;
        }

        [Fact]
        public void AnUnspecifiedDateTimeSerialisesWithTheUtcSuffix()
        {
            var value = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);

            JsonSerializer.Serialize(value, options).Should().Be("\"2026-01-02T03:04:05Z\"");
        }

        [Fact]
        public void AUtcDateTimeSerialisesUnchangedWithTheUtcSuffix()
        {
            var value = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

            JsonSerializer.Serialize(value, options).Should().Be("\"2026-01-02T03:04:05Z\"");
        }

        [Fact]
        public void ALocalDateTimeSerialisesAsItsUtcInstant()
        {
            var value = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Local);

            var expected = "\"" + value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss") + "Z\"";

            JsonSerializer.Serialize(value, options).Should().Be(expected);
        }

        [Fact]
        public void ANullableDateTimeSerialisesWithTheUtcSuffixAndNullStaysNull()
        {
            DateTime? value = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
            DateTime? empty = null;

            JsonSerializer.Serialize(value, options).Should().Be("\"2026-01-02T03:04:05Z\"");
            JsonSerializer.Serialize(empty, options).Should().Be("null");
        }

        [Fact]
        public void ADateTimeBoxedAsObjectInsideADictionarySerialisesWithTheUtcSuffix()
        {
            var payload = new Dictionary<string, object?>
            {
                ["CreatedDate"] = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified)
            };

            JsonSerializer.Serialize(payload, options).Should().Contain("2026-01-02T03:04:05Z");
        }

        [Fact]
        public void AnOffsetlessStringDeserialisesAsThatWallClockTimeInUtc()
        {
            var value = JsonSerializer.Deserialize<DateTime>("\"2026-01-02T03:04:05\"", options);

            value.Should().Be(new DateTime(2026, 1, 2, 3, 4, 5));
            value.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void AnOffsetStringDeserialisesAsTheSameInstantInUtc()
        {
            var value = JsonSerializer.Deserialize<DateTime>("\"2026-01-02T03:04:05+02:00\"", options);

            value.Should().Be(new DateTime(2026, 1, 2, 1, 4, 5));
            value.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void AZuluStringDeserialisesUnchangedInUtc()
        {
            var value = JsonSerializer.Deserialize<DateTime>("\"2026-01-02T03:04:05Z\"", options);

            value.Should().Be(new DateTime(2026, 1, 2, 3, 4, 5));
            value.Kind.Should().Be(DateTimeKind.Utc);
        }
    }
}