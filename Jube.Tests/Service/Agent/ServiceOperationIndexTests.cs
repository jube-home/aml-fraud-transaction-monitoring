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
using System.Reflection;
using FluentAssertions;
using Jube.Service.Agent.PageAgentToolBinder;
using Jube.Service.Agent.ServiceToolCatalogue;
using Xunit;

namespace Jube.Test.Service.Agent
{
    [Trait("Category", "Unit")]
    public sealed class ServiceOperationIndexTests
    {
        [Fact]
        public void TheIndexBuildsWithoutADuplicateOperationName()
        {
            ServiceOperationIndex.All.Should().NotBeEmpty();
        }

        [Fact]
        public void EveryCataloguedToolResolvesToADecoratedMethod()
        {
            var unresolved = ServiceToolCatalogue.All.Select(t => t.Name)
                .Where(name => !ServiceOperationIndex.All.ContainsKey(name))
                .OrderBy(name => name)
                .ToList();

            unresolved.Should().BeEmpty(
                "a catalogued tool with no [ServiceOperation] method behind it cannot be bound: "
                + string.Join(", ", unresolved));
        }

        [Fact]
        public void EveryResolvedMethodAgreesWithTheCatalogueOnKind()
        {
            foreach (var tool in ServiceToolCatalogue.All)
            {
                var binding = ServiceOperationIndex.Get(tool.Name);

                binding.Operation.Kind.Should().Be(tool.Kind, tool.Name);
            }
        }

        [Fact]
        public void EveryResolvedServiceExposesAFactoryTheBinderCanFind()
        {
            var withoutFactory = ServiceToolCatalogue.All
                .Select(t => ServiceOperationIndex.Get(t.Name).ServiceType)
                .Distinct()
                .Where(type => type.GetMethods(BindingFlags.Public
                                               | BindingFlags.Static)
                    .All(method => method.Name != "CreateAsync"))
                .Select(type => type.Name)
                .OrderBy(name => name)
                .ToList();

            withoutFactory.Should().BeEmpty(
                "the binder constructs every service through a public static CreateAsync: "
                + string.Join(", ", withoutFactory));
        }
    }
}