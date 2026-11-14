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
using Jube.Dto.Forms;
using Jube.Dto.Query.FormModel;
using Jube.Service.Query.FormModel;
using Jube.Validations.EntityAnalysisModelTag;
using Xunit;

namespace Jube.Test.Service.Query.FormModel
{
    [Trait("Category", "Unit")]
    public sealed class FormModelChecklistTests
    {
        private static readonly FormModelRegistry Registry =
            FormModelRegistry.Build(typeof(FormEndpointAttribute).Assembly,
                typeof(EntityAnalysisModelTagDtoValidator).Assembly);

        private static FormModelDto Model(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            return model!;
        }

        [Fact]
        public void AStatusOffersItsChecklistsAndAChecklistOffersItsItems()
        {
            Model("CaseWorkflowStatus").Children.Select(child => child.Area).Should()
                .Contain("CaseWorkflowChecklist");
            Model("CaseWorkflowChecklist").Children.Select(child => child.Area).Should()
                .Equal("CaseWorkflowChecklistItem");
        }

        [Fact]
        public void TheConditionsOfAnItemAreCodeFieldsRevealedByTheirSwitches()
        {
            var model = Model("CaseWorkflowChecklistItem");

            var show = model.Fields.Single(f => f.Name == "showSql");
            var validate = model.Fields.Single(f => f.Name == "validateSql");

            show.Widget.Should().Be("code");
            show.Group.Should().Be("Show");
            validate.Widget.Should().Be("code");
            validate.Group.Should().Be("Validation");

            model.Fields.Single(f => f.Name == "validateMessage").Group.Should().Be("Validation");
            model.Fields.Single(f => f.Name == "slaMinutes").Required.Should().BeFalse();
        }

        [Fact]
        public void TheShowAndValidationSwitchesAreSwitchesThatOpenTheirOwnCards()
        {
            var model = Model("CaseWorkflowChecklistItem");

            var enableShow = model.Fields.Single(f => f.Name == "enableShow");
            var enableValidation = model.Fields.Single(f => f.Name == "enableValidation");

            enableShow.Widget.Should().Be("switch");
            enableShow.Group.Should().Be("Show");
            enableValidation.Widget.Should().Be("switch");
            enableValidation.Group.Should().Be("Validation");

            model.Fields.Single(f => f.Name == "showSql").VisibleWhen.Should().ContainSingle()
                .Which.Property.Should().Be("enableShow");
            model.Fields.Single(f => f.Name == "showSql").RequiredWhen.Should().ContainSingle()
                .Which.Property.Should().Be("enableShow");
            model.Fields.Single(f => f.Name == "validateSql").VisibleWhen.Should().ContainSingle()
                .Which.Property.Should().Be("enableValidation");
            model.Fields.Single(f => f.Name == "validateSql").RequiredWhen.Should().ContainSingle()
                .Which.Property.Should().Be("enableValidation");
            model.Fields.Single(f => f.Name == "validateMessage").VisibleWhen.Should().ContainSingle()
                .Which.Property.Should().Be("enableValidation");
        }

        [Fact]
        public void ThePriorityOfAChecklistAndOfAnItemIsANumberThatOrdersTheList()
        {
            Model("CaseWorkflowChecklistItem").Fields.Single(f => f.Name == "priority").Widget.Should()
                .Be("number");
            Model("CaseWorkflowChecklist").Fields.Single(f => f.Name == "priority").Widget.Should()
                .Be("number");
        }

        [Fact]
        public void EveryGroupAnItemFieldUsesIsDeclared()
        {
            var model = Model("CaseWorkflowChecklistItem");
            var declared = model.Groups.Select(g => g.Path).ToHashSet();

            model.Fields.Where(f => f.Group != string.Empty).Select(f => f.Group).Distinct().Should()
                .BeSubsetOf(declared);
        }
    }
}
