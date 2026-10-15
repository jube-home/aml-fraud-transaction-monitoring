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
using Jube.Analytics.Datasets;
using Jube.Analytics.Datasets.Models;
using Xunit;

namespace Jube.Test.Exhaustive.Datasets
{
    public class DatasetMappingRulesTests
    {
        private static List<DatasetColumnDefinition> Profile()
        {
            return
            [
                new DatasetColumnDefinition
                {
                    Index = 0, Header = "Amount", Kind = DatasetColumnKinds.Numeric, NumericCount = 10, Min = 1,
                    Max = 9
                },
                new DatasetColumnDefinition
                {
                    Index = 1, Header = "Country", Kind = DatasetColumnKinds.Text, TextCount = 10
                },
                new DatasetColumnDefinition
                {
                    Index = 2, Header = "Fraud", Kind = DatasetColumnKinds.Numeric, NumericCount = 10, IsBinary = true,
                    ZeroCount = 6, OneCount = 4, Min = 0, Max = 1
                },
                new DatasetColumnDefinition
                {
                    Index = 3, Header = "Const", Kind = DatasetColumnKinds.Numeric, NumericCount = 10,
                    IsConstant = true, Min = 2, Max = 2
                },
                new DatasetColumnDefinition
                {
                    Index = 4, Header = "Count", Kind = DatasetColumnKinds.Numeric, NumericCount = 10, Min = 0, Max = 3
                }
            ];
        }

        private static DatasetMappingInput Input(params DatasetMappingColumn[] columns)
        {
            return new DatasetMappingInput(columns);
        }

        private static DatasetMappingColumn Col(int index, bool include, string? name = null, int type = 5)
        {
            return new DatasetMappingColumn(index, include, name ?? "C" + index, type);
        }

        private static DatasetMappingColumn[] Valid()
        {
            return
            [
                Col(0, true, "Amount"), Col(1, false), Col(2, true, "Fraud"), Col(3, false), Col(4, true, "Count", 1)
            ];
        }

        private static List<string> Codes(DatasetMappingInput input)
        {
            return [.. DatasetMappingRules.Validate(Profile(), input).Select(s => s.Code)];
        }

        [Fact]
        public void ValidMapping_HasNoIssues()
        {
            Codes(Input(Valid())).Should().BeEmpty();
        }

        [Fact]
        public void MissingColumns_AreRejected()
        {
            Codes(new DatasetMappingInput(null)).Should().Equal(DatasetMappingCodes.ColumnsRequired);
            Codes(Input()).Should().Equal(DatasetMappingCodes.ColumnsRequired);
        }

        [Fact]
        public void ColumnCountAndIndexProblems_AreRejected()
        {
            Codes(Input(Col(0, true))).Should().Equal(DatasetMappingCodes.ColumnCountMismatch);

            var duplicate = Valid();
            duplicate[1] = Col(0, false);
            Codes(Input(duplicate)).Should().Contain(DatasetMappingCodes.ColumnIndexDuplicate);

            var unknown = Valid();
            unknown[1] = Col(99, false);
            Codes(Input(unknown)).Should().Contain(DatasetMappingCodes.ColumnIndexInvalid);
        }

        [Fact]
        public void CategoricalTextVariable_IsAcceptedAsAnIndependentVariable()
        {
            var columns = Valid();
            columns[1] = Col(1, true, "Country");

            var profile = Profile();
            profile[1].IsCategorical = true;
            profile[1].CategoricalEncoderJson = "{}";

            DatasetMappingRules.Validate(profile, Input(columns)).Select(s => s.Code)
                .Should().NotContain(DatasetMappingCodes.VariableNotNumeric);
        }

        [Fact]
        public void TextAndConstantVariables_AreRejected()
        {
            var columns = Valid();
            columns[1] = Col(1, true, "Country");
            columns[3] = Col(3, true, "Const");

            var codes = Codes(Input(columns));

            codes.Should().Contain(DatasetMappingCodes.VariableNotNumeric);
            codes.Should().Contain(DatasetMappingCodes.VariableConstant);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("1abc")]
        [InlineData("has space")]
        [InlineData("a.b")]
        [InlineData("<b>")]
        [InlineData("a;drop")]
        [InlineData("naïve")]
        [InlineData("tab\tname")]
        public void UnsafeNames_AreRejected(string? name)
        {
            var columns = Valid();
            columns[0] = new DatasetMappingColumn(0, true, name, 5);

            Codes(Input(columns)).Should().Contain(DatasetMappingCodes.NameInvalid);
        }

        [Fact]
        public void OverlongName_IsRejected()
        {
            var columns = Valid();
            columns[0] = Col(0, true, "A" + new string('b', 64));

            Codes(Input(columns)).Should().Contain(DatasetMappingCodes.NameInvalid);
        }

        [Fact]
        public void DuplicateNames_AreRejectedCaseInsensitively()
        {
            var columns = Valid();
            columns[0] = Col(0, true, "Same");
            columns[4] = Col(4, true, "same", 1);

            Codes(Input(columns)).Should().Contain(DatasetMappingCodes.NameDuplicate);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(7)]
        [InlineData(-1)]
        public void UnknownProcessingTypes_AreRejected(int type)
        {
            var columns = Valid();
            columns[0] = Col(0, true, "Amount", type);

            Codes(Input(columns)).Should().Contain(DatasetMappingCodes.ProcessingTypeInvalid);
        }

        [Fact]
        public void NoIncludedVariable_IsRejected()
        {
            Codes(Input(Col(0, false), Col(1, false), Col(2, false), Col(3, false), Col(4, false)))
                .Should().Contain(DatasetMappingCodes.NoVariables);
        }

        [Fact]
        public void ExcludedColumnsAreNotValidated()
        {
            var columns = Valid();
            columns[1] = new DatasetMappingColumn(1, false, "!!bad", 99);

            Codes(Input(columns)).Should().BeEmpty();
        }

        [Fact]
        public void ProcessingTypeHelpers_Behave()
        {
            DatasetProcessingTypes.IsKnown(1).Should().BeTrue();
            DatasetProcessingTypes.IsKnown(5).Should().BeTrue();
            DatasetProcessingTypes.IsKnown(2).Should().BeTrue();
            DatasetProcessingTypes.IsKnown(3).Should().BeTrue();
            DatasetProcessingTypes.IsKnown(4).Should().BeTrue();
            DatasetProcessingTypes.IsKnown(6).Should().BeTrue();
            DatasetProcessingTypes.IsKnown(7).Should().BeFalse();
            DatasetProcessingTypes.Prefix(1).Should().Be("Payload");
            DatasetProcessingTypes.Prefix(5).Should().Be("Abstraction");
        }
    }
}