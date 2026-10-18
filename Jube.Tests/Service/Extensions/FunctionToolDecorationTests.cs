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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Jube.Service.Agent.Models;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FunctionToolDecorationTests
    {
        public static readonly string[] RootNamespaces = ["Jube.Service.Extensions", "Jube.Service.Helpers"];

        private static IEnumerable<(Type Group, FunctionToolGroupAttribute GroupAttribute, MethodInfo Method, string
            Name)> Tools()
        {
            foreach (var type in typeof(FunctionToolAttribute).Assembly.GetTypes()
                         .OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                var group = type.GetCustomAttribute<FunctionToolGroupAttribute>(false);

                if (group is null)
                {
                    continue;
                }

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                                       BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    var tool = method.GetCustomAttribute<FunctionToolAttribute>(false);

                    if (tool is not null)
                    {
                        yield return (type, group, method, tool.Name);
                    }
                }
            }
        }

        [Fact]
        public void EveryGroupLivesInItsOwnNamespaceAndSuitsOneAgent()
        {
            var groups = Tools().GroupBy(t => t.GroupAttribute.Name).ToList();

            groups.Should().HaveCount(22);

            foreach (var group in groups)
            {
                group.Select(t => t.Group).Distinct().Should().ContainSingle(group.Key + " is declared by one class");

                RootNamespaces.Should().Contain(root => group.First().Group.Namespace == root + "." + group.Key,
                    group.Key + " sits in its own namespace");

                group.Count().Should().BeInRange(7, 20, group.Key + " is sized for one agent");
                group.First().GroupAttribute.Description.Length.Should().BeGreaterThanOrEqualTo(30);
            }
        }

        [Fact]
        public void EveryToolAndParameterIsDescribedForTheModel()
        {
            foreach (var (group, _, method, name) in Tools())
            {
                var label = group.Name + "." + method.Name;
                name.Should().NotBeNullOrWhiteSpace(label);
                method.ReturnType.Should().NotBe(typeof(void), label);

                (method.GetCustomAttribute<DescriptionAttribute>(false)?.Description ?? "").Length.Should()
                    .BeGreaterThanOrEqualTo(30, label);

                foreach (var parameter in method.GetParameters())
                {
                    (parameter.GetCustomAttribute<DescriptionAttribute>(false)?.Description ?? "").Length
                        .Should().BeGreaterThanOrEqualTo(8, label + "(" + parameter.Name + ")");
                }
            }
        }

        [Fact]
        public void ToolNamesAreGloballyUnique()
        {
            Tools().GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key)
                .Should().BeEmpty();
        }

        [Fact]
        public void EveryToolHasATestThatDeclaresItCovers()
        {
            var covered = typeof(FunctionToolDecorationTests).Assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .SelectMany(m => m.GetCustomAttributesData())
                .Where(a => a.AttributeType == typeof(TraitAttribute) &&
                            (string?)a.ConstructorArguments[0].Value == "Covers")
                .Select(a => (string)a.ConstructorArguments[1].Value!)
                .ToHashSet(StringComparer.Ordinal);

            Tools().Select(t => t.Name).Where(name => !covered.Contains(name)).Should().BeEmpty();
        }
    }
}