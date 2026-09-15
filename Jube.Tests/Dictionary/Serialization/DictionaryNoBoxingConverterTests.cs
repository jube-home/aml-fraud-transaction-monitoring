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
using System.IO;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Dictionary.Models;
using Jube.Dictionary.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Dictionary.Serialization
{
    [Trait("Category", "Unit")]
    public sealed class DictionaryNoBoxingConverterTests
    {
        private static JsonSerializerSettings Settings()
        {
            return new JsonSerializerSettings
            {
                Converters = { new DictionaryNoBoxingConverter() }
            };
        }

        [Fact]
        public void WritingANullDictionaryAtTheTopLevelProducesJsonNullRatherThanInvokingTheConverter()
        {
            var json = JsonConvert.SerializeObject(null, Settings());

            json.Should().Be("null");
        }

        [Fact]
        public void WritingANullDictionaryAsANestedPropertyAlsoProducesJsonNullWithoutInvokingTheConverter()
        {
            var wrapper = new { Payload = (DictionaryNoBoxing<string>?)null };

            var json = JsonConvert.SerializeObject(wrapper, Settings());

            JObject.Parse(json)["Payload"]!.Type.Should().Be(JTokenType.Null);
        }

        [Fact]
        public void CallingWriteJsonDirectlyWithANullValueStillProducesAnEmptyObjectPerItsOwnDefensiveGuard()
        {
            var converter = new DictionaryNoBoxingConverter();
            using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);

            converter.WriteJson(jsonWriter, null, JsonSerializer.CreateDefault());

            stringWriter.ToString().Should().Be("{}");
        }

        [Fact]
        public void WritingAnEmptyDictionaryProducesAnEmptyJsonObject()
        {
            using var dictionary = new DictionaryNoBoxing<string>();

            var json = JsonConvert.SerializeObject(dictionary, Settings());

            json.Should().Be("{}");
        }

        [Fact]
        public void EachStoredTypeIsWrittenAsItsNaturalJsonRepresentation()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            var dateTime = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
            var guid = Guid.NewGuid();
            dictionary.Add("s", "text");
            dictionary.Add("i", 42);
            dictionary.Add("d", 3.5);
            dictionary.Add("b", true);
            dictionary.Add("dt", dateTime);
            dictionary.Add("g", new InternalValue(guid));
            dictionary.Add("none", default);

            var json = JsonConvert.SerializeObject(dictionary, Settings());

            using var stringReader = new StringReader(json);
            using var jsonTextReader = new JsonTextReader(stringReader);
            jsonTextReader.DateParseHandling = DateParseHandling.None;
            var obj = JObject.Load(jsonTextReader);

            obj["s"]!.Type.Should().Be(JTokenType.String);
            obj["s"]!.Value<string>().Should().Be("text");
            obj["i"]!.Type.Should().Be(JTokenType.Integer);
            obj["i"]!.Value<int>().Should().Be(42);
            obj["d"]!.Type.Should().Be(JTokenType.Float);
            obj["d"]!.Value<double>().Should().Be(3.5);
            obj["b"]!.Type.Should().Be(JTokenType.Boolean);
            obj["b"]!.Value<bool>().Should().BeTrue();
            obj["dt"]!.Value<string>().Should().Be(dateTime.ToString("O"));
            obj["g"]!.Value<string>().Should().Be(guid.ToString());
            obj["none"]!.Type.Should().Be(JTokenType.Null);
        }

        [Fact]
        public void ANullBackedStringValueIsWrittenAsAnEmptyJsonStringNotJsonNull()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("s", null);

            var json = JsonConvert.SerializeObject(dictionary, Settings());
            var obj = JObject.Parse(json);

            obj["s"]!.Type.Should().Be(JTokenType.String);
            obj["s"]!.Value<string>().Should().Be(string.Empty);
        }

        [Fact]
        public void ReadingAJsonIntegerProducesAnIntTypedEntry()
        {
            var dictionary = JsonConvert.DeserializeObject<DictionaryNoBoxing<string>>("""{"i": 42}""", Settings())!;

            dictionary["i"].Type.Should().Be(InternalValue.ValueType.Int);
            dictionary["i"].AsInt().Should().Be(42);
        }

        [Fact]
        public void ReadingAJsonFloatProducesADoubleTypedEntry()
        {
            var dictionary = JsonConvert.DeserializeObject<DictionaryNoBoxing<string>>("""{"d": 3.5}""", Settings())!;

            dictionary["d"].Type.Should().Be(InternalValue.ValueType.Double);
            dictionary["d"].AsDouble().Should().Be(3.5);
        }

        [Fact]
        public void ReadingAJsonBooleanProducesABoolTypedEntry()
        {
            var dictionary = JsonConvert.DeserializeObject<DictionaryNoBoxing<string>>("""{"b": true}""", Settings())!;

            dictionary["b"].Type.Should().Be(InternalValue.ValueType.Bool);
            dictionary["b"].AsBool().Should().BeTrue();
        }

        [Fact]
        public void ReadingAJsonNullProducesAStringTypedEntryWithANullBackingValue()
        {
            var dictionary = JsonConvert.DeserializeObject<DictionaryNoBoxing<string>>("""{"n": null}""", Settings())!;

            dictionary["n"].Type.Should().Be(InternalValue.ValueType.String);
            dictionary["n"].AsString().Should().Be(string.Empty);
            dictionary["n"].ToString().Should().Be("null");
        }

        [Fact]
        public void ReadingAPlainJsonStringProducesAStringTypedEntry()
        {
            var dictionary =
                JsonConvert.DeserializeObject<DictionaryNoBoxing<string>>("""{"s": "hello"}""", Settings())!;

            dictionary["s"].Type.Should().Be(InternalValue.ValueType.String);
            dictionary["s"].AsString().Should().Be("hello");
        }

        [Fact]
        public void ReadingAJsonStringThatLooksLikeAGuidStillProducesAPlainStringTypedEntry()
        {
            var guid = Guid.NewGuid();

            var dictionary =
                JsonConvert.DeserializeObject<DictionaryNoBoxing<string>>($$"""{"g": "{{guid}}"}""", Settings())!;

            dictionary["g"].Type.Should().Be(InternalValue.ValueType.String,
                "the converter never reconstructs a Guid-typed value from JSON - it only recognises JSON's own Float/Integer/Boolean/Null tokens");
            dictionary["g"].AsString().Should().Be(guid.ToString());
        }

        [Fact]
        public void ReadingAJsonStringThatLooksLikeAnIsoDateIsStillDecodedAsAStringTypedEntry()
        {
            var dictionary = JsonConvert.DeserializeObject<DictionaryNoBoxing<string>>(
                """{"dt": "2024-06-15T10:30:00.0000000Z"}""", Settings())!;

            dictionary["dt"].Type.Should().Be(InternalValue.ValueType.String);
        }

        [Fact]
        public void ReadJsonPopulatesAnExistingDictionaryInsteadOfCreatingANewOneWhenOneIsSupplied()
        {
            using var existing = new DictionaryNoBoxing<string>();
            existing.Add("already-there", "keep-me");
            var converter = new DictionaryNoBoxingConverter();
            using var stringReader = new StringReader("""{"new-field": 7}""");
            using var jsonReader = new JsonTextReader(stringReader);
            jsonReader.Read();

            var result = converter.ReadJson(jsonReader, typeof(DictionaryNoBoxing<string>), existing, true,
                JsonSerializer.CreateDefault());

            result.Should().BeSameAs(existing);
            result["already-there"].AsString().Should().Be("keep-me");
            result["new-field"].AsInt().Should().Be(7);
        }

        [Fact]
        public void ReadJsonWithNoExistingInstanceCreatesANewDictionary()
        {
            var converter = new DictionaryNoBoxingConverter();
            using var stringReader = new StringReader("""{"a": 1}""");
            using var jsonReader = new JsonTextReader(stringReader);
            jsonReader.Read();

            var result = converter.ReadJson(jsonReader, typeof(DictionaryNoBoxing<string>), null, false,
                JsonSerializer.CreateDefault());

            result["a"].AsInt().Should().Be(1);
        }

        [Fact]
        public void MultipleFieldsOfDifferentTypesRoundTripThroughWriteThenReadCorrectlyForJsonNativeTypes()
        {
            using var original = new DictionaryNoBoxing<string>();
            original.Add("name", "Alice");
            original.Add("age", 30);
            original.Add("balance", 99.95);
            original.Add("active", true);

            var json = JsonConvert.SerializeObject(original, Settings());
            var roundTripped = JsonConvert.DeserializeObject<DictionaryNoBoxing<string>>(json, Settings())!;

            roundTripped["name"].AsString().Should().Be("Alice");
            roundTripped["age"].AsInt().Should().Be(30);
            roundTripped["balance"].AsDouble().Should().Be(99.95);
            roundTripped["active"].AsBool().Should().BeTrue();
        }
    }
}