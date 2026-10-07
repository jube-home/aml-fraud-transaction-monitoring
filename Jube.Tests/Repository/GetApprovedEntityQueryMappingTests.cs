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

using Jube.Data.Query.GetApprovedEntityQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Xunit;

namespace Jube.Test.Repository
{
    [Trait("Category", "Unit")]
    public class GetApprovedEntityQueryMappingTests
    {
        private static EntityAnalysisModelListVersion ListVersion(int parentId, int version, string name = "Deny")
        {
            return new EntityAnalysisModelListVersion
            {
                Id = 9000 + version,
                EntityAnalysisModelListId = parentId,
                Name = name,
                Active = 1,
                Locked = 0,
                Version = version,
                Guid = Guid.NewGuid(),
                CreatedUser = "maker",
                CreatedDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        }

        [Fact]
        public void AVersionRowMapsOntoTheEntityItIsAVersionOf()
        {
            var version = ListVersion(41, 3, "Blocklist");

            var entity = GetApprovedEntityQuery<EntityAnalysisModelList, EntityAnalysisModelListVersion>
                .MapVersionToEntity(version, 41);

            using var scope = new AssertionScope();
            entity.Name.Should().Be("Blocklist");
            entity.Active.Should().Be(1);
            entity.Version.Should().Be(3);
            entity.CreatedUser.Should().Be("maker");
            entity.Guid.Should().Be(version.Guid);
        }

        [Fact]
        public void TheEntityTakesTheParentIdAndNotTheIdOfTheVersionRow()
        {
            var version = ListVersion(41, 3);

            var entity = GetApprovedEntityQuery<EntityAnalysisModelList, EntityAnalysisModelListVersion>
                .MapVersionToEntity(version, 41);

            using var scope = new AssertionScope();
            entity.Id.Should().Be(41);
            entity.Id.Should().NotBe(version.Id);
        }

        [Fact]
        public void MembersTheVersionTableDoesNotCarryAreLeftUnset()
        {
            var entity = GetApprovedEntityQuery<EntityAnalysisModelList, EntityAnalysisModelListVersion>
                .MapVersionToEntity(ListVersion(41, 3), 41);

            using var scope = new AssertionScope();
            entity.UpdatedUser.Should().BeNull();
            entity.ImportId.Should().BeNull();
            entity.EntityAnalysisModel.Should().BeNull();
        }

        [Fact]
        public void TheVersionNumberIsReadFromTheVersionRow()
        {
            GetApprovedEntityQuery<EntityAnalysisModelList, EntityAnalysisModelListVersion>
                .VersionNumberOf(ListVersion(41, 7)).Should().Be(7);
        }

        [Fact]
        public void AVersionRowWithNoVersionNumberNeverMatchesAWantedVersion()
        {
            var version = ListVersion(41, 3);
            version.Version = null;

            GetApprovedEntityQuery<EntityAnalysisModelList, EntityAnalysisModelListVersion>
                .VersionNumberOf(version).Should().Be(0);
        }

        [Fact]
        public void TheParentIdPredicateSelectsOnlyTheRequestedParents()
        {
            var query = new GetApprovedEntityQuery<EntityAnalysisModelList, EntityAnalysisModelListVersion>(
                null, EntityApprovalKind.EntityAnalysisModelList,
                nameof(EntityAnalysisModelListVersion.EntityAnalysisModelListId), 1);

            var predicate = query.ParentIdInPredicate([41, 43]).Compile();

            var rows = new List<EntityAnalysisModelListVersion>
            {
                ListVersion(41, 1), ListVersion(42, 1), ListVersion(43, 1)
            };

            var matched = rows.Where(predicate).Select(s => s.EntityAnalysisModelListId).ToList();

            matched.Should().BeEquivalentTo([41, 43]);
        }

        [Fact]
        public void TheParentIdPredicateSkipsRowsWhoseNullableParentIsNotSet()
        {
            var query = new GetApprovedEntityQuery<EntityAnalysisModelListValue, EntityAnalysisModelListValueVersion>(
                null, EntityApprovalKind.EntityAnalysisModelListValue,
                nameof(EntityAnalysisModelListValueVersion.EntityAnalysisModelListValueId), 1);

            var predicate = query.ParentIdInPredicate([61]).Compile();

            var wanted = new EntityAnalysisModelListValueVersion
                { EntityAnalysisModelListValueId = 61, Version = 1 };
            var orphan = new EntityAnalysisModelListValueVersion
                { EntityAnalysisModelListValueId = null, Version = 1 };

            using var scope = new AssertionScope();
            predicate(wanted).Should().BeTrue();
            predicate(orphan).Should().BeFalse();
        }

        [Fact]
        public void AListValueVersionMapsOntoTheValueItIsAVersionOf()
        {
            var version = new EntityAnalysisModelListValueVersion
            {
                Id = 5555,
                EntityAnalysisModelListValueId = 61,
                EntityAnalysisModelListId = 41,
                ListValue = "1.2.3.4",
                Version = 2,
                CreatedUser = "maker"
            };

            var entity = GetApprovedEntityQuery<EntityAnalysisModelListValue, EntityAnalysisModelListValueVersion>
                .MapVersionToEntity(version, 61);

            using var scope = new AssertionScope();
            entity.Id.Should().Be(61);
            entity.EntityAnalysisModelListId.Should().Be(41);
            entity.ListValue.Should().Be("1.2.3.4");
            entity.Version.Should().Be(2);
        }

        [Fact]
        public void ADeletedRowIsRecognisedAsDeletedSoAnApprovedDeleteTakesEffect()
        {
            var entity = new EntityAnalysisModelList { Id = 41, Version = 5, Deleted = 1 };

            GetApprovedEntityQuery<EntityAnalysisModelList, EntityAnalysisModelListVersion>
                .IsDeleted(entity).Should().BeTrue();
        }

        [Fact]
        public void AnUndeletedRowIsNotTreatedAsDeletedWhetherTheFlagIsZeroOrUnset()
        {
            using var scope = new AssertionScope();

            GetApprovedEntityQuery<EntityAnalysisModelList, EntityAnalysisModelListVersion>
                .IsDeleted(new EntityAnalysisModelList { Id = 41, Deleted = 0 }).Should().BeFalse();
            GetApprovedEntityQuery<EntityAnalysisModelList, EntityAnalysisModelListVersion>
                .IsDeleted(new EntityAnalysisModelList { Id = 41, Deleted = null }).Should().BeFalse();
        }

        [Fact]
        public void TheVersionRowBehindAPendingDeleteMapsBackToAnUndeletedEntity()
        {
            var version = ListVersion(41, 4);
            version.Deleted = 0;

            var entity = GetApprovedEntityQuery<EntityAnalysisModelList, EntityAnalysisModelListVersion>
                .MapVersionToEntity(version, 41);

            using var scope = new AssertionScope();
            GetApprovedEntityQuery<EntityAnalysisModelList, EntityAnalysisModelListVersion>
                .IsDeleted(entity).Should()
                .BeFalse("the pre-delete snapshot is what keeps running while the delete awaits a checker");
            entity.Version.Should().Be(4);
        }

        [Fact]
        public void AVersionTypeWithoutTheNamedParentPropertyIsRefusedWhenTheQueryIsBuilt()
        {
            var act = () => new GetApprovedEntityQuery<EntityAnalysisModelList, EntityAnalysisModelListVersion>(
                null, EntityApprovalKind.EntityAnalysisModelList, "NotAProperty", 1);

            act.Should().Throw<ArgumentException>();
        }
    }
}