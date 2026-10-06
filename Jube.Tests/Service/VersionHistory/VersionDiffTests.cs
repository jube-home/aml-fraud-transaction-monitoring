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

namespace Jube.Test.Service.VersionHistory
{
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using Jube.Data.Query;
    using Jube.Test.Service.VersionHistory.Models;
    using Xunit;

    [Trait("Category", "Unit")]
    public sealed class VersionDiffTests
    {
        [Fact]
        public void LayoutOnlyDifferencesAreNotReportedAsChanges()
        {
            var from = new ProbeVersion { Id = 1, Name = "Return a / \r\n (b _  \r\n+ c)" };
            var to = new ProbeVersion { Id = 2, Name = "Return a /\n(b _\n+ c)" };

            VersionDiff.Compare(from, to, new HashSet<string> { "Id" }).Should().BeEmpty();
        }

        [Fact]
        public void ATokenChangeIsReported()
        {
            var from = new ProbeVersion { Name = "Return a / b" };
            var to = new ProbeVersion { Name = "Return a * b" };

            var changes = VersionDiff.Compare(from, to);

            changes.Select(c => c.PropertyName).Should().Equal("Name");
            changes[0].FromValue.Should().Be("Return a / b");
            changes[0].ToValue.Should().Be("Return a * b");
        }

        [Fact]
        public void IgnoredFieldsAreSkipped()
        {
            var from = new ProbeVersion { Id = 1, Name = "a", Secret = "x" };
            var to = new ProbeVersion { Id = 2, Name = "b", Secret = "y" };

            var changes = VersionDiff.Compare(from, to, new HashSet<string> { "Name", "Id" });

            changes.Select(c => c.PropertyName).Should().Equal("Secret");
        }

        [Fact]
        public void NonSimplePropertiesAreNotCompared()
        {
            var from = new ProbeVersion { Items = [1] };
            var to = new ProbeVersion { Items = [2] };

            VersionDiff.Compare(from, to).Should().BeEmpty();
        }

        [Fact]
        public void AcrossTypesOnlySharedSimplePropertiesAreCompared()
        {
            var earlier = new ProbeVersion { Id = 10, Name = "old", Items = [1], Version = 3 };
            var current = new ProbeParentVersion { Id = 99, ProbeParentId = 6, Name = "new" };

            var changes = VersionDiff.Compare(earlier, current, new HashSet<string> { "Id", "Version" });

            changes.Select(c => c.PropertyName).Should().Equal("Name");
        }

        [Fact]
        public void MissingSidesProduceNoChanges()
        {
            VersionDiff.Compare(null, new ProbeVersion { Name = "a" }, new HashSet<string>()).Should().BeEmpty();
        }
    }
}