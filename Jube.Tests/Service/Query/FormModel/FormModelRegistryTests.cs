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
using Jube.Dto.Query.FormModel;
using Jube.Service.Query.FormModel;
using Jube.Validations.EntityAnalysisModelTag;
using Xunit;

namespace Jube.Test.Service.Query.FormModel
{
    [Trait("Category", "Unit")]
    public sealed class FormModelRegistryTests
    {
        private static readonly FormModelRegistry Registry =
            FormModelRegistry.Build(typeof(FormEndpointAttribute).Assembly,
                typeof(EntityAnalysisModelTagDtoValidator).Assembly);

        private static FormModelDto Tag()
        {
            Registry.TryGet("EntityAnalysisModelTag", out var model).Should().BeTrue();

            return model!;
        }

        public static IEnumerable<object[]> Areas()
        {
            return Registry.Areas.Order().Select(area => new object[] { area });
        }

        [Fact]
        public void EveryDtoWithAFormEndpointIsRegisteredUnderItsArea()
        {
            var expected = typeof(FormEndpointAttribute).Assembly.GetTypes()
                .Select(type => type.GetCustomAttribute<FormEndpointAttribute>())
                .Where(attribute => attribute != null)
                .Select(attribute => attribute!.Area);

            Registry.Areas.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void AnAreaIsFoundWhateverTheCase()
        {
            Registry.TryGet("entityanalysismodeltag", out var model).Should().BeTrue();

            model!.Area.Should().Be("EntityAnalysisModelTag");
        }

        [Fact]
        public void AnUnknownAreaIsNotFound()
        {
            Registry.TryGet("Nope", out var model).Should().BeFalse();

            model.Should().BeNull();
        }

        [Fact]
        public void TheModelNamesItsEndpointAndKeysInCamelCase()
        {
            var model = Tag();

            model.Endpoint.Should().Be("/api/EntityAnalysisModelTag");
            model.Keys.Id.Should().Be("id");
            model.Keys.Parent.Should().Be("entityAnalysisModelId");
            model.Keys.NaturalKey.Should().Be("name");
            model.LockFields.Should().Equal("locked");
        }

        [Fact]
        public void AnAreaDeclaresAllOperationsUnlessItsDtoSaysOtherwise()
        {
            Tag().Operations.Should().Equal("list", "read", "create", "update", "delete");

            Registry.TryGet("PermissionSpecification", out var catalogue).Should().BeTrue();
            catalogue!.Operations.Should().Equal("list");

            Registry.TryGet("EntityAnalysisModelSynchronisationSchedule", out var schedule).Should().BeTrue();
            schedule!.Operations.Should().Equal("create");

            Registry.TryGet("ExhaustiveSearchInstancePromotedTrialInstance", out var promotion).Should().BeTrue();
            promotion!.Operations.Should().Equal("update");
        }

        [Fact]
        public void GroupsAreOrderedAndCarryTheirCollapsedState()
        {
            var groups = Tag().Groups;

            groups.Select(group => group.Path).Should().Equal("Identity", "Audit");
            groups.Single(group => group.Path == "Audit").Collapsed.Should().BeTrue();
            groups.Single(group => group.Path == "Identity").Collapsed.Should().BeFalse();
        }

        [Fact]
        public void FieldsAreOrderedByGroupThenPositionAndOnlyDecoratedPropertiesAppear()
        {
            var fields = Tag().Fields;

            fields.Select(field => field.Name).Should().Equal(
                "name", "active", "locked", "id", "createdUser", "createdDate", "updatedUser",
                "updatedDate", "version", "deletedUser", "deletedDate");
            fields.Should().NotContain(field => field.Name == "entityAnalysisModelId");
        }

        [Fact]
        public void AFieldCarriesItsTypeWidgetDefaultAndDescription()
        {
            var active = Tag().Fields.Single(field => field.Name == "active");

            active.Type.Should().Be("boolean");
            active.Widget.Should().Be("switch");
            active.NewDefault.Should().Be(false);
            active.Description.Should().NotBeEmpty();
            active.Group.Should().Be("Identity");
        }

        [Theory]
        [InlineData("name", "string", true)]
        [InlineData("version", "integer", false)]
        [InlineData("createdDate", "datetime", true)]
        [InlineData("locked", "boolean", false)]
        public void ATypeAndItsNullabilityAreReadFromTheProperty(string name, string type, bool nullable)
        {
            var field = Tag().Fields.Single(candidate => candidate.Name == name);

            field.Type.Should().Be(type);
            field.Nullable.Should().Be(nullable);
        }

        [Fact]
        public void ALabelIsTheAttributeLabelOrTheHumanisedPropertyName()
        {
            Tag().Fields.Single(field => field.Name == "createdUser").Label.Should().Be("Created user");
            Tag().Groups.Single(group => group.Path == "Audit").Label.Should().Be("Audit");
        }

        [Fact]
        public void ListColumnsCarryTheirTitleOrderAndHiddenColumnsComeLast()
        {
            var columns = Tag().ListColumns;

            columns.Select(column => column.Field).Should().Equal("name", "active", "id", "version");
            columns.Single(column => column.Field == "name").Title.Should().Be("Name");
            columns.Single(column => column.Field == "id").Hidden.Should().BeTrue();
            columns.Single(column => column.Field == "name").Hidden.Should().BeFalse();
        }

        [Fact]
        public void ALookupIsDescribedWithItsSourceAndFields()
        {
            Registry.TryGet("RoleRegistryPermission", out var model).Should().BeTrue();

            var lookup = model!.Fields.Single(field => field.Name == "permissionSpecificationId").Lookup;

            lookup.Should().NotBeNull();
            lookup!.Source.Should().Be("/api/PermissionSpecification");
            lookup.TextField.Should().Be("name");
            lookup.ValueField.Should().Be("id");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryFieldNamesAGroupThatIsDeclared(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            var declared = model!.Groups.Select(group => group.Path).ToHashSet();

            model.Fields.Where(field => field.Group != string.Empty && !declared.Contains(field.Group))
                .Select(field => field.Group)
                .Should().BeEmpty($"{area} must declare every group its fields use");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void NoFieldNameOrListColumnIsDuplicated(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            model!.Fields.Select(field => field.Name).Should().OnlyHaveUniqueItems();
            model.ListColumns.Select(column => column.Field).Should().OnlyHaveUniqueItems();
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryConditionAndKeyNamesAFieldOfTheSameRecord(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            var type = typeof(FormEndpointAttribute).Assembly.GetTypes()
                .Single(candidate => candidate.GetCustomAttribute<FormEndpointAttribute>()?.Area == area);
            var properties = type.GetProperties().Select(property => char.ToLowerInvariant(property.Name[0]) +
                                                                     property.Name[1..]).ToHashSet();

            var referenced = model!.Fields
                .SelectMany(field => field.VisibleWhen.Concat(field.RequiredWhen).Concat(field.EnabledWhen))
                .Select(condition => condition.Property)
                .Concat(model.Fields.Where(field => field.Lookup?.ParentField != null)
                    .Select(field => field.Lookup!.ParentField!))
                .Concat(model.LockFields)
                .Append(model.Keys.Id);

            referenced.Where(name => !properties.Contains(name)).Should().BeEmpty();
        }

        [Fact]
        public void AConditionKeepsItsOperatorValuesAndMatchMode()
        {
            var conditioned = Registry.Areas
                .Select(area =>
                {
                    Registry.TryGet(area, out var model);

                    return model!;
                })
                .SelectMany(model => model.Fields)
                .FirstOrDefault(field => field.VisibleWhen.Count > 0 || field.RequiredWhen.Count > 0);

            if (conditioned == null)
            {
                return;
            }

            var condition = conditioned.VisibleWhen.Concat(conditioned.RequiredWhen).First();

            condition.Op.Should().BeOneOf("equals", "notEquals", "in", "notIn", "greaterThan", "isEmpty",
                "isNotEmpty");
            condition.MatchMode.Should().BeOneOf("all", "any");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void NoFieldLabelShowsAnIdentifierWordTheUserHasNoUseFor(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            var offending = model!.Fields
                .Where(field => !(field.ReadOnly && field.Label == "Guid"))
                .Where(field => System.Text.RegularExpressions.Regex.IsMatch(field.Label,
                    @"\bguid\b|\w\s+id$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                .Select(field => $"{field.Name}: {field.Label}")
                .ToList();

            offending.Should().BeEmpty(
                $"{area} should label the thing the user picks or reads, not how it is stored: give the property a FormField Label");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryFieldLabelIsUniqueWithinItsArea(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            var duplicated = model!.Fields.GroupBy(field => field.Label, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => $"{group.Key} ({string.Join(", ", group.Select(field => field.Name))})")
                .ToList();

            duplicated.Should().BeEmpty(
                $"{area} has fields a user could not tell apart: give one of each pair a FormField Label");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void NoRadioSetOffersMoreThanThreeChoicesAndEverySpanIsOneOrTwo(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            model!.Fields.Where(field => field.Widget == "radio" && field.Options.Count > 3)
                .Select(field => field.Name).Should().BeEmpty(
                    $"{area} has a radio set with more than three choices, which belongs in a select");
            model.Fields.Where(field => field.Span is < 1 or > 2).Select(field => field.Name).Should().BeEmpty();
        }

        [Fact]
        public void ARadioSetWithMoreThanThreeChoicesIsDrawnAsASelectAndTwoOrThreeStayRadios()
        {
            Registry.TryGet("EntityAnalysisModelActivationRule", out var model).Should().BeTrue();

            model!.Fields.Single(field => field.Name == "bypassSuspendInterval").Widget.Should().Be("select");

            Registry.TryGet("EntityAnalysisModelResponseElevationRange", out var range).Should().BeTrue();
            range!.Fields.Single(field => field.Name == "notificationTypeId").Widget.Should().Be("radio");
        }

        [Fact]
        public void AFieldAskingForTwoColumnsKeepsThemAndTheDefaultIsOne()
        {
            Registry.TryGet("EntityAnalysisModelResponseElevationRange", out var model).Should().BeTrue();

            model!.Fields.Single(field => field.Name == "notificationTypeId").Span.Should().Be(2);
            model.Fields.Single(field => field.Name == "name").Span.Should().Be(1);
        }
    }
}
