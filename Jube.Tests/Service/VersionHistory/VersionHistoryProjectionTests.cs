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
using FluentAssertions.Execution;
using Jube.Data.Poco;
using Jube.Data.Query;
using Jube.Service.Query.VersionHistory;
using Jube.Test.Service.VersionHistory.Models;
using Xunit;

namespace Jube.Test.Service.VersionHistory
{
    [Trait("Category", "Unit")]
    public sealed class VersionHistoryProjectionTests
    {
        [Fact]
        public void TheProbeShapesCarryTheValuesTheyAreGivenAsTheyAreRead()
        {
            var dto = new ProbeDto { Name = "n", Items = [2] };
            var version = new ProbeVersion
            {
                Id = 1, Name = "v", Items = [3], Secret = "s", Version = 4, CreatedUser = "u"
            };
            var parent = new ProbeParentVersion { Id = 5, ProbeParentId = 6, Name = "p" };

            using var scope = new AssertionScope();
            dto.Name.Should().Be("n");
            dto.Items.Should().Equal(2);
            version.Id.Should().Be(1);
            version.Name.Should().Be("v");
            version.Items.Should().Equal(3);
            version.Secret.Should().Be("s");
            version.Version.Should().Be(4);
            version.CreatedUser.Should().Be("u");
            parent.Id.Should().Be(5);
            parent.ProbeParentId.Should().Be(6);
            parent.Name.Should().Be("p");
        }

        [Fact]
        public void AVersionFieldTheDtoDoesNotExposeIsNotExposedAtAll()
        {
            var fields = VersionHistoryProjection.ExposedFields(typeof(ProbeDto), typeof(ProbeVersion));

            fields.Should().NotContain("Secret");
        }

        [Fact]
        public void TheUserPasswordHashIsNotExposedByTheUserRegistryDto()
        {
            var fields = VersionHistoryProjection.ExposedFields(
                typeof(Jube.Dto.Repository.UserRegistry.UserRegistryDto), typeof(UserRegistryVersion));

            using var scope = new AssertionScope();
            fields.Should().NotContain("Password");
            fields.Should().Contain(["Name", "Email", "CreatedUser"]);
        }

        [Fact]
        public void TheExhaustiveFilterTokensAreExposedOnlyBecauseItsDtoAlreadyExposesThem()
        {
            var fields = VersionHistoryProjection.ExposedFields(
                typeof(Jube.Dto.ExhaustiveSearchInstance.ExhaustiveSearchInstanceDto),
                typeof(ExhaustiveSearchInstanceVersion));

            fields.Should().Contain("FilterTokens");
        }

        [Fact]
        public void TheVersionMetadataAndParentKeyAreExposedWhetherOrNotTheDtoNamesThem()
        {
            var fields = VersionHistoryProjection.ExposedFields(typeof(ProbeDto), typeof(ProbeVersion));

            using var scope = new AssertionScope();
            fields.Should().Contain(["Id", "Version", "CreatedUser"]);
        }

        [Fact]
        public void TheParentKeyIsExposedFromTheVersionTypeName()
        {
            var fields = VersionHistoryProjection.ExposedFields(typeof(ProbeDto), typeof(ProbeParentVersion));

            fields.Should().Contain("ProbeParentId");
        }

        [Fact]
        public void ComplexValuesAreNeverExposedEvenWhenTheDtoNamesThem()
        {
            var fields = VersionHistoryProjection.ExposedFields(typeof(ProbeDto), typeof(ProbeVersion));

            fields.Should().NotContain("Items");
        }

        [Fact]
        public void ProjectingARowReturnsOnlyTheExposedFields()
        {
            var fields = VersionHistoryProjection.ExposedFields(typeof(ProbeDto), typeof(ProbeVersion));
            var row = new ProbeVersion { Id = 3, Name = "a", Items = [1], Secret = "hidden", Version = 2 };

            var projected = VersionHistoryProjection.Project(row, fields);

            using var scope = new AssertionScope();
            projected.Keys.Should().NotContain("Secret");
            projected.Keys.Should().NotContain("Items");
            projected["Name"].Should().Be("a");
            projected["Version"].Should().Be(2);
        }

        [Fact]
        public void ChangesOutsideTheExposedFieldsAreDropped()
        {
            var fields = VersionHistoryProjection.ExposedFields(typeof(ProbeDto), typeof(ProbeVersion));
            var changes = new List<VersionFieldChange>
            {
                new("Name", "a", "b"),
                new("Secret", "x", "y")
            };

            var kept = VersionHistoryProjection.Changes(changes, fields);

            kept.Select(c => c.PropertyName).Should().Equal("Name");
        }

        [Fact]
        public void ChangeValuesAreShownUpToTwoHundredCharactersThenEllipsed()
        {
            var fields = VersionHistoryProjection.ExposedFields(typeof(ProbeDto), typeof(ProbeVersion));
            var oversized = new string('x', 250);
            var changes = new List<VersionFieldChange>
            {
                new("Name", oversized, "short")
            };

            var kept = VersionHistoryProjection.Changes(changes, fields);

            kept[0].FromValue.Should().Be(new string('x', 200) + "...");
            kept[0].ToValue.Should().Be("short");
        }
    }
}