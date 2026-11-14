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
using System.Linq;
using FluentAssertions;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Convention
{
    [Trait("Category", "Convention")]
    public sealed class NoAsParametersConventionTests
    {
        private static string EndpointsDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.App", "Jube.App.csproj")))
            {
                directory = directory.Parent;
            }

            return Path.Combine(directory.Required().FullName, "Jube.App");
        }

        [Fact]
        public void NothingInTheAppBindsAnEndpointWithAsParameters()
        {
            var separator = Path.DirectorySeparatorChar;

            var offenders = Directory.EnumerateFiles(EndpointsDirectory(), "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{separator}obj{separator}") && !p.Contains($"{separator}bin{separator}"))
                .Where(p => File.ReadAllText(p).Contains("[AsParameters]", StringComparison.Ordinal))
                .Select(Path.GetFileName)
                .ToList();

            offenders.Should().BeEmpty("an endpoint takes a JSON or MessagePack body, or explicit scalar parameters");
        }
    }
}
