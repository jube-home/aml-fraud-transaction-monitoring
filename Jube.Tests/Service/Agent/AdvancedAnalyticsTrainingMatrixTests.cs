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

using System.Collections.Generic;
using FluentAssertions;
using Jube.AskJooby.DataCache.Models;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Repository.AdvancedAnalyticsTrainingData;
using Xunit;
using Jube.Service.Repository.AdvancedAnalyticsTrainingData.Models;
using Jube.Analytics.Datasets;

namespace Jube.Test.Service.Agent
{
    [Trait("Category", "Unit")]
    public sealed class AdvancedAnalyticsTrainingMatrixTests
    {
        private static CachedTable BuildTable()
        {
            return new CachedTable(
                [
                    new CachedColumn("Dependent", "double"), new CachedColumn("IsChip", "double"),
                    new CachedColumn("ATM", "double"), new CachedColumn("Amount", "double")
                ],
                [
                    new Dictionary<string, object?>
                        { ["ATM"] = 0L, ["IsChip"] = 1L, ["Dependent"] = 1L, ["Amount"] = 30.14 },
                    new Dictionary<string, object?>
                        { ["ATM"] = 1L, ["IsChip"] = 0L, ["Dependent"] = 0L, ["Amount"] = 7.5 }
                ]);
        }

        private static double[] Unpack(AdvancedAnalyticsTrainingMatrixResult result)
        {
            return AdvancedAnalyticsPacking.Unpack(result.Packed, AdvancedAnalyticsPacking.CurrentVersion);
        }

        [Fact]
        public void ThePredictorsArePackedInTheOrderTheCallerNamedThemAndNotTheTableOrder()
        {
            var result = AdvancedAnalyticsTrainingMatrix.Build(BuildTable(), ["Amount", "IsChip", "ATM"],
                "Dependent");

            result.Rows.Should().Be(2);
            result.Columns.Should().Be(4);
            Unpack(result).Should().Equal(30.14, 1, 0, 1, 7.5, 0, 1, 0);
        }

        [Fact]
        public void TheLabelIsAlwaysTheLastColumnWhateverItsPositionInTheTable()
        {
            var result = AdvancedAnalyticsTrainingMatrix.Build(BuildTable(), ["IsChip", "ATM", "Amount"],
                "Dependent");

            Unpack(result)[result.Columns - 1].Should().Be(1);
            Unpack(result)[2 * result.Columns - 1].Should().Be(0);
            result.PositiveLabels.Should().Be(1);
        }

        [Fact]
        public void APredictorMayBeLeftOutAltogether()
        {
            var result = AdvancedAnalyticsTrainingMatrix.Build(BuildTable(), ["Amount"], "Dependent");

            result.Columns.Should().Be(2);
            Unpack(result).Should().Equal(30.14, 1, 7.5, 0);
        }

        [Fact]
        public void ABlankPredictorBecomesAMissingValueRatherThanAZero()
        {
            var table = new CachedTable(
                [new CachedColumn("Amount", "double"), new CachedColumn("Dependent", "double")],
                [
                    new Dictionary<string, object?> { ["Amount"] = null, ["Dependent"] = 1L },
                    new Dictionary<string, object?> { ["Amount"] = 4.0, ["Dependent"] = 0L }
                ]);

            var result = AdvancedAnalyticsTrainingMatrix.Build(table, ["Amount"], "Dependent");

            result.MissingValues.Should().Be(1);
            double.IsNaN(Unpack(result)[0]).Should().BeTrue();
        }

        [Fact]
        public void ABooleanCountsAsOneOrNought()
        {
            var table = new CachedTable(
                [new CachedColumn("Flag", "bool"), new CachedColumn("Dependent", "double")],
                [
                    new Dictionary<string, object?> { ["Flag"] = true, ["Dependent"] = 1L },
                    new Dictionary<string, object?> { ["Flag"] = false, ["Dependent"] = 0L }
                ]);

            Unpack(AdvancedAnalyticsTrainingMatrix.Build(table, ["Flag"], "Dependent"))
                .Should().Equal(1, 1, 0, 0);
        }

        [Fact]
        public void ANumberHeldAsTextIsParsedRatherThanRefused()
        {
            var table = new CachedTable(
                [new CachedColumn("Amount", "string"), new CachedColumn("Dependent", "double")],
                [
                    new Dictionary<string, object?> { ["Amount"] = "30.14", ["Dependent"] = 1L },
                    new Dictionary<string, object?> { ["Amount"] = "7.5", ["Dependent"] = 0L }
                ]);

            Unpack(AdvancedAnalyticsTrainingMatrix.Build(table, ["Amount"], "Dependent"))
                .Should().Equal(30.14, 1, 7.5, 0);
        }

        [Fact]
        public void TextThatIsNotANumberIsRefusedRatherThanQuietlyEncoded()
        {
            var table = new CachedTable(
                [new CachedColumn("Country", "string"), new CachedColumn("Dependent", "double")],
                [
                    new Dictionary<string, object?> { ["Country"] = "SE", ["Dependent"] = 1L },
                    new Dictionary<string, object?> { ["Country"] = "NO", ["Dependent"] = 0L }
                ]);

            var act = () => AdvancedAnalyticsTrainingMatrix.Build(table, ["Country"], "Dependent");

            act.Should().Throw<DtoValidationException>().Which.Message.Should().Contain("Country");
        }

        [Fact]
        public void ALabelThatIsNotBinaryIsRefused()
        {
            var table = new CachedTable(
                [new CachedColumn("Amount", "double"), new CachedColumn("Dependent", "double")],
                [
                    new Dictionary<string, object?> { ["Amount"] = 1.0, ["Dependent"] = 2L },
                    new Dictionary<string, object?> { ["Amount"] = 2.0, ["Dependent"] = 0L }
                ]);

            var act = () => AdvancedAnalyticsTrainingMatrix.Build(table, ["Amount"], "Dependent");

            act.Should().Throw<DtoValidationException>();
        }

        [Fact]
        public void AMissingLabelIsRefusedBecauseThereIsNothingToImputeAnOutcomeFrom()
        {
            var table = new CachedTable(
                [new CachedColumn("Amount", "double"), new CachedColumn("Dependent", "double")],
                [
                    new Dictionary<string, object?> { ["Amount"] = 1.0, ["Dependent"] = null },
                    new Dictionary<string, object?> { ["Amount"] = 2.0, ["Dependent"] = 0L }
                ]);

            var act = () => AdvancedAnalyticsTrainingMatrix.Build(table, ["Amount"], "Dependent");

            act.Should().Throw<DtoValidationException>();
        }

        [Fact]
        public void AColumnTheTableDoesNotHoldIsReportedWithTheOnesItDoes()
        {
            var act = () => AdvancedAnalyticsTrainingMatrix.Build(BuildTable(), ["Amount", "Nonsense"],
                "Dependent");

            act.Should().Throw<DtoValidationException>().Which.Message.Should().Contain("Dependent");
        }

        [Fact]
        public void TheSameColumnCannotBeBothAPredictorAndTheLabel()
        {
            var act = () => AdvancedAnalyticsTrainingMatrix.Build(BuildTable(), ["Amount", "Dependent"],
                "Dependent");

            act.Should().Throw<DtoValidationException>();
        }

        [Fact]
        public void APredictorNamedTwiceIsRefused()
        {
            var act = () => AdvancedAnalyticsTrainingMatrix.Build(BuildTable(), ["Amount", "Amount"],
                "Dependent");

            act.Should().Throw<DtoValidationException>();
        }

        [Fact]
        public void NamingNoPredictorAtAllIsRefused()
        {
            var act = () => AdvancedAnalyticsTrainingMatrix.Build(BuildTable(), [], "Dependent");

            act.Should().Throw<DtoValidationException>();
        }
    }
}