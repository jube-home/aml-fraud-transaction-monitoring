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

using System.Linq;
using FluentAssertions;
using Jube.App.Wire;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireJitSchemaTests
    {
        private const string Schema = "Jube.Dto.Query.WireSchema.WireSchemaDto";
        private const string Type = "Jube.Dto.Query.WireSchema.WireTypeDto";
        private const string Field = "Jube.Dto.Query.WireSchema.WireFieldDto";

        [Fact]
        public void HeadCarriesTheRoutesAndSettingsButNoTypes()
        {
            var registry = new WireTypeRegistry();
            var full = registry.GetSchema();

            var head = registry.GetHead();

            head.Hash.Should().Be(full.Hash);
            head.Mode.Should().Be(full.Mode);
            head.TypesHash.Should().Be(full.TypesHash);
            head.Types.Should().BeEmpty();
            head.Routes.Should().BeEquivalentTo(full.Routes);
            registry.GetHead().Should().BeSameAs(head);
        }

        [Fact]
        public void ShapesCarryOnlyTheClosureOfWhatWasAskedFor()
        {
            var registry = new WireTypeRegistry();
            var full = registry.GetSchema();

            var shapes = registry.GetShapes([Type + "[]"]);

            shapes.Hash.Should().Be(full.Hash);
            shapes.Routes.Should().BeEmpty();
            shapes.Types.Keys.Should().Contain([Type, Field]);
            shapes.Types.Keys.Should().NotContain(Schema);
            shapes.Types.Count.Should().BeLessThan(full.Types.Count);

            foreach (var type in shapes.Types)
            {
                type.Value.Should().BeSameAs(full.Types[type.Key]);
            }
        }

        [Fact]
        public void UnknownAndPrimitiveShapesYieldNoTypes()
        {
            var registry = new WireTypeRegistry();

            registry.GetShapes(["string", "Not.A.Type{}#", "int[]"]).Types.Should().BeEmpty();
        }

        [Fact]
        public void EveryTypeIsReachableFromItsOwnShape()
        {
            var registry = new WireTypeRegistry();
            var full = registry.GetSchema();

            var union = registry.GetShapes(full.Types.Keys.ToList());

            full.Types.Keys.Except(union.Types.Keys).Should().BeEmpty();
        }
    }
}
