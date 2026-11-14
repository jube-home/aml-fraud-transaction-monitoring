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
using System.Text.Json;
using FluentAssertions;
using Jube.App.Wire;
using MessagePack;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireStringKeyedMapTests
    {
        private static byte[] Pack<T>(T value)
        {
            return MessagePackSerializer.Serialize(value, WireSerializer.Options);
        }

        private static T Unpack<T>(byte[] bytes)
        {
            return MessagePackSerializer.Deserialize<T>(bytes, WireSerializer.Options);
        }

        [Fact]
        public void AMapKeyedByGuidIsWrittenExactlyAsAMapKeyedByTheTextOfTheGuid()
        {
            var id = Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301");

            Pack(new Dictionary<Guid, string> { [id] = "one" }).Should()
                .Equal(Pack(new Dictionary<string, string> { [id.ToString()] = "one" }));
        }

        [Fact]
        public void IntegerAndGuidKeyedMapsRoundTripThroughEveryDictionaryInterface()
        {
            var id = Guid.NewGuid();

            Unpack<Dictionary<int, int>>(Pack(new Dictionary<int, int> { [3] = 4, [-7] = 9 }))
                .Should().Equal(new Dictionary<int, int> { [3] = 4, [-7] = 9 });
            Unpack<IDictionary<Guid, string>>(Pack<IDictionary<Guid, string>>(
                new Dictionary<Guid, string> { [id] = "x" })).Should().Equal(
                new Dictionary<Guid, string> { [id] = "x" });
            Unpack<IReadOnlyDictionary<long, IReadOnlyList<string>>>(Pack<IReadOnlyDictionary<long, IReadOnlyList<string>>>(
                    new Dictionary<long, IReadOnlyList<string>> { [5_000_000_000] = ["a", "b"] }))
                [5_000_000_000].Should().Equal("a", "b");
        }

        [Fact]
        public void TheKeysAreExactlyTheNamesTheJsonSerialiserGivesTheSameMap()
        {
            var map = new Dictionary<int, int> { [3] = 4, [-7] = 9, [1_000_000] = 1 };
            var json = JsonSerializer.Serialize(map);

            var names = JsonDocument.Parse(json).RootElement.EnumerateObject().Select(p => p.Name);
            var keys = Unpack<Dictionary<string, int>>(Pack(map)).Keys;

            keys.Should().BeEquivalentTo(names);
        }

        [Fact]
        public void AKeyThatIsNotAValidNumberIsRefusedRatherThanSilentlyDropped()
        {
            var bytes = Pack(new Dictionary<string, int> { ["abc"] = 1 });

            var act = () => Unpack<Dictionary<int, int>>(bytes);

            act.Should().Throw<MessagePackSerializationException>();
        }

        [Fact]
        public void ANullMapStaysNull()
        {
            Unpack<Dictionary<Guid, string>>(Pack<Dictionary<Guid, string>>(null)).Should().BeNull();
        }

        [Theory]
        [InlineData(typeof(Dictionary<Guid, string>), "string{}")]
        [InlineData(typeof(Dictionary<int, int>), "int{}")]
        [InlineData(typeof(IReadOnlyDictionary<long, IReadOnlyList<string>>), "string[]{}")]
        public void TheSchemaDescribesAMapWithANumericOrGuidKeyJustAsAMapWithAStringKey(Type type, string shape)
        {
            var registry = new WireTypeRegistry();
            registry.Attach([]);

            registry.ShapeOf(type).Should().Be(shape);
        }

        [Fact]
        public void AMapWithAKeyTheWireCannotTextualiseStaysOffTheWire()
        {
            var registry = new WireTypeRegistry();
            registry.Attach([]);

            registry.ShapeOf(typeof(Dictionary<DateTime, int>)).Should().BeNull();
        }
    }
}
