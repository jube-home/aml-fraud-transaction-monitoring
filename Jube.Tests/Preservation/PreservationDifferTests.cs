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
using Jube.Preservation.Diff;
using Jube.Preservation.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Preservation
{
    [Trait("Category", "Unit")]
    public sealed class PreservationDifferTests
    {
        private static JObject Payload(string json)
        {
            return JObject.Parse(json);
        }

        private static DiffResult Compare(string left, string right)
        {
            return PreservationDiffer.Compare(Payload(left), Payload(right));
        }

        [Fact]
        public void TwoIdenticalPayloadsAreIdentical()
        {
            const string json = "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"Active\":1}]}";

            var result = Compare(json, json);

            result.Identical.Should().BeTrue();
            result.Entries.Should().BeEmpty();
            result.Unchanged.Should().Be(1);
        }

        [Fact]
        public void AnEntityOnlyOnTheRightIsAdded()
        {
            var result = Compare("{\"EntityAnalysisModel\":[]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\"}]}");

            result.Added.Should().Be(1);
            result.Entries.Should().ContainSingle(e => e.Kind == DiffEntryKind.Added &&
                                                       e.Path.SequenceEqual(new[] { "EntityAnalysisModel[A]" }));
        }

        [Fact]
        public void AnEntityOnlyOnTheLeftIsRemoved()
        {
            var result = Compare("{\"EntityAnalysisModel\":[{\"Name\":\"A\"}]}", "{}");

            result.Removed.Should().Be(1);
            result.Entries.Should().ContainSingle(e => e.Kind == DiffEntryKind.Removed);
        }

        [Fact]
        public void AChangedPropertyIsReportedWithItsBeforeAndAfter()
        {
            var result = Compare("{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"Active\":1}]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"Active\":0}]}");

            result.Changed.Should().Be(1);
            var change = result.Entries.Single().Changes.Single();
            change.Property.Should().Be("Active");
            change.Before.Should().Be("1");
            change.After.Should().Be("0");
        }

        [Fact]
        public void EntitiesAreMatchedByNameNotByPosition()
        {
            var result = Compare(
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"X\":1},{\"Name\":\"B\",\"X\":2}]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"B\",\"X\":2},{\"Name\":\"A\",\"X\":1}]}");

            result.Identical.Should().BeTrue();
        }

        [Theory]
        [InlineData("Id")]
        [InlineData("Guid")]
        [InlineData("TenantRegistryId")]
        [InlineData("InheritedId")]
        [InlineData("ImportId")]
        [InlineData("Version")]
        [InlineData("CreatedDate")]
        [InlineData("CreatedUser")]
        [InlineData("UpdatedDate")]
        [InlineData("UpdatedUser")]
        [InlineData("DeletedUser")]
        [InlineData("RoleRegistryGuid")]
        public void IdentifiersAndAuditStampsNeverCountAsChanges(string property)
        {
            var result = Compare("{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"" + property + "\":\"1\"}]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"" + property + "\":\"2\"}]}");

            result.Identical.Should().BeTrue();
        }

        [Fact]
        public void AReferenceToAnotherEntityIsIgnoredButCounted()
        {
            var result = Compare(
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"CaseWorkflowId\":5}]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"CaseWorkflowId\":9}]}");

            result.Identical.Should().BeTrue();
            result.ReferencesIgnored.Should().Be(1);
        }

        [Fact]
        public void AnEnumerationIdThatIsNotAnEntityIsARealChange()
        {
            var result = Compare(
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"ParseTypeId\":1}]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"ParseTypeId\":2}]}");

            result.Changed.Should().Be(1);
        }

        [Fact]
        public void NestedEntitiesAreComparedAndTheirPathShowsTheirParent()
        {
            var result = Compare(
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"EntityAnalysisModelTag\":[{\"Name\":\"t\",\"Colour\":\"red\"}]}]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"EntityAnalysisModelTag\":[{\"Name\":\"t\",\"Colour\":\"blue\"}]}]}");

            var entry = result.Entries.Should().ContainSingle().Subject;

            entry.Path.Should().Equal("EntityAnalysisModel[A]", "EntityAnalysisModelTag[t]");
            entry.Changes.Single().After.Should().Be("blue");
        }

        [Fact]
        public void AnAddedEntityCountsEveryEntityNestedBeneathIt()
        {
            var result = Compare("{}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"EntityAnalysisModelTag\":[{\"Name\":\"t\"},{\"Name\":\"u\"}]}]}");

            result.Added.Should().Be(3);
            result.Entries.Should().ContainSingle().Which.NestedCount.Should().Be(2);
        }

        [Fact]
        public void AnArrayWithoutAKeyIsComparedAsASetOfValues()
        {
            var result = Compare(
                "{\"RoleRegistry\":[{\"Name\":\"R\",\"RoleRegistryPermission\":[{\"PermissionSpecificationId\":1},{\"PermissionSpecificationId\":2}]}]}",
                "{\"RoleRegistry\":[{\"Name\":\"R\",\"RoleRegistryPermission\":[{\"PermissionSpecificationId\":2},{\"PermissionSpecificationId\":3}]}]}");

            result.Added.Should().Be(1);
            result.Removed.Should().Be(1);
            result.Changed.Should().Be(0);
        }

        [Fact]
        public void DuplicateNamesFallBackToSetSemanticsInsteadOfPairingTheWrongOnes()
        {
            var result = Compare(
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"X\":1},{\"Name\":\"A\",\"X\":2}]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"X\":2},{\"Name\":\"A\",\"X\":1}]}");

            result.Identical.Should().BeTrue();
        }

        [Fact]
        public void ALongValueIsShortenedWithItsLength()
        {
            var longText = new string('x', 500);

            var result = Compare("{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"Script\":\"a\"}]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"Script\":\"" + longText + "\"}]}");

            result.Entries.Single().Changes.Single().After.Should().EndWith("(500 characters)")
                .And.HaveLength(result.Entries.Single().Changes.Single().After!.Length);

            result.Entries.Single().Changes.Single().After!.Length.Should().BeLessThan(200);
        }

        [Fact]
        public void ANestedObjectThatIsNotACollectionIsComparedPropertyByProperty()
        {
            var result = Compare("{\"EntityPermission\":{\"X\":[{\"Name\":\"a\"}]},\"AdvancedAnalytics\":{\"Setting\":1}}",
                "{\"EntityPermission\":{\"X\":[{\"Name\":\"a\"}]},\"AdvancedAnalytics\":{\"Setting\":2}}");

            result.Changed.Should().Be(1);
            result.Entries.Single().Path.Should().Equal("AdvancedAnalytics");
        }

        [Fact]
        public void TheEntryListIsCappedButTheCountsAreComplete()
        {
            var many = string.Join(",", Enumerable.Range(0, PreservationDiffer.MaximumEntries + 25)
                .Select(i => "{\"Name\":\"n" + i + "\"}"));

            var result = Compare("{}", "{\"EntityAnalysisModel\":[" + many + "]}");

            result.Entries.Should().HaveCount(PreservationDiffer.MaximumEntries);
            result.Truncated.Should().BeTrue();
            result.Added.Should().Be(PreservationDiffer.MaximumEntries + 25);
        }

        [Fact]
        public void SectionsAreCountedSeparately()
        {
            var result = Compare("{\"RoleRegistry\":[{\"Name\":\"R\"}]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\"}]}");

            result.Sections.Select(s => s.Name).Should().Equal("EntityAnalysisModel", "RoleRegistry");
            result.Sections.Single(s => s.Name == "RoleRegistry").Removed.Should().Be(1);
            result.Sections.Single(s => s.Name == "EntityAnalysisModel").Added.Should().Be(1);
        }

        [Fact]
        public void APropertyPresentOnOneSideOnlyIsAChange()
        {
            var result = Compare("{\"EntityAnalysisModel\":[{\"Name\":\"A\"}]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"Note\":\"n\"}]}");

            var change = result.Entries.Single().Changes.Single();
            change.Before.Should().BeNull();
            change.After.Should().Be("n");
        }

        [Fact]
        public void ThePayloadIsTakenFromAWrapperBody()
        {
            PreservationDiffer.PayloadOf("{\"Version\":2,\"Payload\":{\"RoleRegistry\":[]}}")
                .Should().ContainKey("RoleRegistry");

            PreservationDiffer.PayloadOf("{\"Version\":2}").Should().BeEmpty();
        }

        [Fact]
        public void AnEmptyCollectionAndAMissingOneAreTheSame()
        {
            var result = Compare("{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"EntityAnalysisModelTag\":[]}]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\"}]}");

            result.Identical.Should().BeTrue();
        }

        [Fact]
        public void NullAndMissingValuesAreTheSame()
        {
            var result = Compare("{\"EntityAnalysisModel\":[{\"Name\":\"A\",\"Note\":null}]}",
                "{\"EntityAnalysisModel\":[{\"Name\":\"A\"}]}");

            result.Identical.Should().BeTrue();
        }
    }
}
