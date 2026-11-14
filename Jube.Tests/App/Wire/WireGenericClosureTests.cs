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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.App.Wire;
using MessagePack;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireGenericClosureTests(ITestOutputHelper output)
    {
        [MessagePackObject]
        public sealed class Item
        {
            [Key(0)]
            public string Name { get; set; }
        }

        [MessagePackObject]
        public sealed class Envelope<T>
        {
            [Key(0)]
            public T Value { get; set; }

            [Key(1)]
            public int Count { get; set; }
        }

        [MessagePackObject]
        public sealed class Holder
        {
            [Key(0)]
            public Envelope<Item> Direct { get; set; }
        }

        private static string NameOf<T>()
        {
            return typeof(T).FullName;
        }

        private static WireTypeRegistry Registry(params System.Type[] types)
        {
            var registry = new WireTypeRegistry();
            registry.Attach([]);

            foreach (var type in types)
            {
                registry.ShapeOf(type);
            }

            return registry;
        }

        [Theory]
        [InlineData(typeof(Envelope<Item>), "")]
        [InlineData(typeof(Envelope<Item>[]), "[]")]
        [InlineData(typeof(List<Envelope<Item>>), "[]")]
        [InlineData(typeof(Dictionary<string, Envelope<Item>>), "{}")]
        public void AClosedGenericTypeAndItsArgumentAreInTheGeneratedSetWhateverCollectionWrapsIt(
            System.Type type, string suffix)
        {
            var registry = Registry(type);
            var shape = registry.ShapeOf(type);

            shape.Should().EndWith(suffix);

            var generated = registry.GetShapes([shape]).Types.Keys;

            generated.Should().Contain(NameOf<Envelope<Item>>()).And.Contain(NameOf<Item>());
        }

        [Fact]
        public void AMemberTypedAsAClosedGenericIsFollowedToItsFields()
        {
            var registry = Registry(typeof(Holder));

            var generated = registry.GetShapes([registry.ShapeOf(typeof(Holder))]).Types.Keys;

            generated.Should().Contain(NameOf<Envelope<Item>>()).And.Contain(NameOf<Item>());
        }

        [Fact]
        public void EveryTypeTheSchemaHoldsIsInTheGeneratedSetAndSoGetsAGeneratedDecoder()
        {
            var registry = new WireTypeRegistry();
            registry.Attach([]);

            var schema = registry.GetSchema();
            var missing = schema.Types.Keys.Where(name => !registry.GeneratedTypeNames.Contains(name))
                .OrderBy(name => name, System.StringComparer.Ordinal).ToList();

            foreach (var name in missing)
            {
                output.WriteLine("no generated decoder: " + name);
            }

            output.WriteLine($"{missing.Count} of {schema.Types.Count} types have no generated decoder");

            missing.Should().BeEmpty("a type outside the generated set decodes through the slower fallback");
        }

        [Fact]
        public void ARowGridBehindAGenericResponseIsStillSeenAsCarryingRows()
        {
            var registry = Registry(typeof(Envelope<List<Dictionary<string, object>>>), typeof(Envelope<Item>));

            registry.CarriesRows(registry.ShapeOf(typeof(Envelope<List<Dictionary<string, object>>>)))
                .Should().BeTrue();
            registry.CarriesRows(registry.ShapeOf(typeof(Envelope<Item>))).Should().BeFalse();
        }
    }
}
