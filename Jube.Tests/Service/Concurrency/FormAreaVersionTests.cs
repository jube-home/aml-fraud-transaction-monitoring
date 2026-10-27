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
using System.Reflection;
using FluentAssertions;
using Jube.Dto.Forms;
using Jube.Service.Query.FormModel;
using Xunit;

namespace Jube.Test.Service.Concurrency
{
    [Trait("Category", "Unit")]
    public sealed class FormAreaVersionTests
    {
        private static readonly Assembly Dtos = typeof(FormEndpointAttribute).Assembly;
        private static readonly Assembly Services = typeof(FormModelRegistry).Assembly;

        public static IEnumerable<object[]> VersionedAreas()
        {
            return Dtos.GetTypes()
                .Where(type => type.GetCustomAttribute<FormEndpointAttribute>() != null &&
                               type.GetProperty("Version") != null)
                .Select(type => type.GetCustomAttribute<FormEndpointAttribute>()!.Area)
                .Order()
                .Select(area => new object[] { area });
        }

        private static MethodInfo? ToPocoOf(Type dto)
        {
            return Services.GetTypes()
                .Where(type => type.Name.EndsWith("Mapper", StringComparison.Ordinal) && type.IsAbstract && type.IsSealed)
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                .FirstOrDefault(method => method.Name == "ToPoco" && method.GetParameters().Length == 1 &&
                                          method.GetParameters()[0].ParameterType == dto);
        }

        [Theory]
        [MemberData(nameof(VersionedAreas))]
        public void TheMapperFromTheFormDtoCarriesTheVersionTheClientSawToThePoco(string area)
        {
            var dto = Dtos.GetTypes().Single(type => type.GetCustomAttribute<FormEndpointAttribute>()?.Area == area);
            var toPoco = ToPocoOf(dto);

            if (toPoco == null)
            {
                return;
            }

            var instance = Activator.CreateInstance(dto)!;

            dto.GetProperty("Version")!.SetValue(instance, 7);

            var poco = toPoco.Invoke(null, [instance]);

            var version = poco!.GetType().GetProperty("Version")?.GetValue(poco);

            Convert.ToInt32(version).Should().Be(7,
                $"{area} must pass the version the client saw to the repository, or a stale save overwrites silently");
        }

        [Fact]
        public void MostFormAreasHaveAMapperThatTheGuardActuallyReaches()
        {
            var reached = VersionedAreas().Select(row => (string)row[0])
                .Count(area => ToPocoOf(Dtos.GetTypes().Single(type =>
                    type.GetCustomAttribute<FormEndpointAttribute>()?.Area == area)) != null);

            reached.Should().BeGreaterThan(25);
        }
    }
}
