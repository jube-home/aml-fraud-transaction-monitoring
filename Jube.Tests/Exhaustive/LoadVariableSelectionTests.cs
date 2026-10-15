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

using System.IO;
using System.Linq;
using FluentAssertions;
using Jube.Engine.Exhaustive.Utilities;
using Xunit;
using Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery;

namespace Jube.Test.Exhaustive
{
    public class LoadVariableSelectionTests
    {
        private static GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQueryDto Field(string name,
            byte processingTypeId, int dataTypeId = 3)
        {
            return new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQueryDto
            {
                Name = name,
                ProcessingTypeId = processingTypeId,
                DataTypeId = dataTypeId,
                ValueJsonPath = "path." + name
            };
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(5, true)]
        [InlineData(6, false)]
        [InlineData(7, true)]
        [InlineData(8, false)]
        [InlineData(255, false)]
        public void BuildVariablesFromFields_KeepsOnlyProcessingTypesThreeFiveAndSeven(int processingTypeId,
            bool kept)
        {
            var variables = Extraction.BuildVariablesFromFields([Field("F", (byte)processingTypeId)]);

            variables.Count.Should().Be(kept ? 1 : 0);
        }

        [Fact]
        public void BuildVariablesFromFields_PreservesQueryOrderAndNumbersKeysFromZeroWithoutGaps()
        {
            var variables = Extraction.BuildVariablesFromFields(
            [
                Field("Payload.X", 1),
                Field("Zeta", 7),
                Field("Dictionary.D", 2),
                Field("Alpha", 3),
                Field("Sanction.S", 4),
                Field("Mid", 5),
                Field("Activation.R", 0)
            ]);

            variables.Keys.Should().Equal(0, 1, 2);
            variables.Values.Select(v => v.Name).Should().Equal("Zeta", "Alpha", "Mid");
        }

        [Fact]
        public void BuildVariablesFromFields_CarriesNameProcessingTypeAndJsonPath()
        {
            var variables = Extraction.BuildVariablesFromFields([Field("Abstraction.A", 5)]);

            var variable = variables[0];
            variable.Name.Should().Be("Abstraction.A");
            variable.ProcessingTypeId.Should().Be(5);
            variable.ValueJsonPath.Should().Be("path.Abstraction.A");
        }

        [Fact]
        public void BuildVariablesFromFields_NoFieldsOrNoEligibleFieldsGivesNoVariables()
        {
            Extraction.BuildVariablesFromFields([]).Should().BeEmpty();

            Extraction.BuildVariablesFromFields([Field("A", 2), Field("B", 4), Field("C", 1)])
                .Should().BeEmpty();
        }

        [Fact]
        public void BuildMockVariables_IsTwentySixAbstractionsInCsvHeaderOrderEachOnce()
        {
            var header = File.ReadAllLines("Exhaustive/Mock.csv").First().Split(',').Where(c => c != "Dependent")
                .ToArray();

            var variables = Extraction.BuildMockVariables();

            variables.Should().HaveCount(26);
            variables.Keys.Should().Equal(Enumerable.Range(0, 26));
            variables.Values.Select(v => v.Name).Should().Equal(header.Select(h => "Abstraction." + h));
            variables.Values.Select(v => v.Name).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void BuildMockVariables_EveryVariableIsProcessingTypeFiveWithADollarAbstractionPath()
        {
            foreach (var variable in Extraction.BuildMockVariables().Values)
            {
                variable.ProcessingTypeId.Should().Be(5);
                variable.ValueJsonPath.Should().Be("$.abstraction." + variable.Name["Abstraction.".Length..]);
            }
        }

        [Fact]
        public void BuildMockVariables_ReturnsAFreshDictionaryEachTime()
        {
            var first = Extraction.BuildMockVariables();
            first[0].Mean = 42;
            first.Remove(1);

            var second = Extraction.BuildMockVariables();

            second.Should().HaveCount(26);
            second[0].Mean.Should().Be(0);
        }

        [Fact]
        public void BuildMockVariables_MockJsonWithEveryFeatureProducesAFullyPopulatedRow()
        {
            var variables = Extraction.BuildMockVariables();
            var names = variables.Values.Select(v => v.Name["Abstraction.".Length..]).ToArray();
            var json = "{\"abstraction\":{" + string.Join(",", names.Select((n, i) => $"\"{n}\":{i + 1}")) + "}}";

            var rows = Extraction.BuildRows(variables, [json]);

            rows.Should().ContainSingle().Which.Should().Equal(Enumerable.Range(1, 26).Select(i => (double)i));
        }
    }
}