/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Linq;
using FluentAssertions;
using Jube.Dto.Repository.PreservationSnapshot;
using Jube.Preservation.Models;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class PreservationSnapshotCoverageTests
    {
        [Fact]
        public void TheRequestExposesEverySwitchTheExportItselfSupports()
        {
            var exported = typeof(ExportOptions).GetProperties()
                .Where(property => property.PropertyType == typeof(bool))
                .Select(property => property.Name);

            var requestable = typeof(PreservationSnapshotRequestDto).GetProperties()
                .Where(property => property.PropertyType == typeof(bool))
                .Select(property => property.Name)
                .ToHashSet();

            exported.Should().OnlyContain(name => requestable.Contains(name),
                "a snapshot body that cannot be asked to include something is a rollback that deletes it: "
                + "recovery removes whatever the body does not hold, so an export switch with no way to "
                + "request it is silent data loss rather than a smaller snapshot");
        }

        [Fact]
        public void EverythingButTheLargestIsIncludedUnlessTheCallerSaysOtherwise()
        {
            var request = new PreservationSnapshotRequestDto();

            request.Lists.Should().BeTrue();
            request.Dictionaries.Should().BeTrue();
            request.Visualisations.Should().BeTrue();
            request.Roles.Should().BeTrue("a rollback without roles loses the permission model");
            request.AdvancedAnalytics.Should().BeTrue(
                "a rollback without them loses trained models and their training history");
        }
    }
}