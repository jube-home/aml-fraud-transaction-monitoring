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
    public sealed class FormModelParityWidgetTests
    {
        private static readonly FormModelRegistry Registry =
            FormModelRegistry.Build(typeof(FormEndpointAttribute).Assembly,
                typeof(EntityAnalysisModelTagDtoValidator).Assembly);

        private static FormFieldDto Field(string area, string name)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            return model!.Fields.Single(field => field.Name == name);
        }

        [Fact]
        public void ARegistryParameterDefaultValueFollowsItsDataType()
        {
            Field("VisualisationRegistryParameter", "defaultValue").Widget.Should().Be("typed:dataTypeId");
        }

        [Fact]
        public void ARegistryParameterStartsAsAString()
        {
            Field("VisualisationRegistryParameter", "dataTypeId").NewDefault.Should().Be(1);
        }

        [Fact]
        public void AnExhaustiveAnomalyParametersFieldOffersTheVariableReductionChoice()
        {
            Field("ExhaustiveSearchInstance", "anomalyParametersJson").Widget.Should().Be("anomalyReduction");
        }
    }
}
