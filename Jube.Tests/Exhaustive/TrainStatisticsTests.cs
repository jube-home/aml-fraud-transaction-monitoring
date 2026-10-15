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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Statistics;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class TrainStatisticsTests
    {
        private static Dictionary<int, Variable> Vars(int count)
        {
            return Enumerable.Range(0, count).ToDictionary(i => i, i => new Variable { Name = $"v{i}" });
        }

        private static double[][] Columns(params double[][] columns)
        {
            return [.. Enumerable.Range(0, columns[0].Length).Select(r => columns.Select(c => c[r]).ToArray())];
        }

        private static async Task<(Dictionary<int, Variable> Variables, TrainRecordingRepository Rows,
            TrainRecordingRepository Histograms)> PopulationAsync(params double[][] columns)
        {
            var variables = Vars(columns.Length);
            var rows = new TrainRecordingRepository();
            var histograms = new TrainRecordingRepository();

            await TrainSupport.NewTraining(1).CalculateStatisticsAsync(9, variables, Columns(columns),
                StatisticsTarget.Population, rows, histograms, CancellationToken.None);

            return (variables, rows, histograms);
        }

        [Fact]
        public async Task Population_ComputesMeanMinMaxAndSampleStandardDeviationAsync()
        {
            double[] column = [2, 4, 4, 4, 5, 5, 7, 9];

            var (variables, rows, _) = await PopulationAsync(column);

            variables[0].Mean.Should().BeApproximately(5, 1e-12);
            variables[0].Min.Should().Be(2);
            variables[0].Max.Should().Be(9);
            variables[0].Mode.Should().Be(4);
            variables[0].Sd.Should().BeApproximately(Math.Sqrt(32d / 7d), 1e-9);
            variables[0].DistinctCount.Should().Be(5);
            var row = (ExhaustiveSearchInstanceVariable)rows.Inserted.Single();
            row.ExhaustiveSearchInstanceId.Should().Be(9);
            row.VariableSequence.Should().Be(0);
            row.Name.Should().Be("v0");
            row.Mean.Should().BeApproximately(5, 1e-12);
            row.DistinctValues.Should().Be(5);
        }

        [Fact]
        public async Task Population_AssignsNormalisationTypeByShapeOfTheColumnAsync()
        {
            var (variables, _, _) = await PopulationAsync(
                [0, 0, 0, 0],
                [0, 1, 1, 0],
                [1, 2, 3, 4],
                [7, 7, 7, 7],
                [0, 0.5, 1, 0.25],
                [0, 1.00005, 0, 1],
                [-3, -2, -1, 1],
                [1, 1, 1, 1]);

            variables.Select(v => (int)v.Value.NormalisationType).Should().Equal(0, 1, 2, 2, 1, 1, 2, 2);
        }

        [Fact]
        public async Task
            Population_ConstantNonZeroColumn_IsZScoreTypeWithZeroSd_AndTheStatisticsRowIsStillWrittenAsync()
        {
            var (variables, rows, _) = await PopulationAsync([7, 7, 7, 7]);

            variables[0].NormalisationType.Should().Be(2);
            variables[0].Sd.Should().Be(0);
            variables[0].Min.Should().Be(7);
            variables[0].Max.Should().Be(7);
            rows.Inserted.Should().HaveCount(1);
        }

        [Fact]
        public async Task Population_TriangularDistribution_IsBuiltInNormalisedSpaceOnlyWhenMinBelowMaxAsync()
        {
            var (variables, _, _) = await PopulationAsync(
                [1, 2, 2, 3, 10],
                [5, 5, 5, 5, 5],
                [0, 0, 1, 1, 1],
                [-3, -1, 0, 0, 4]);

            variables[0].TriangularDistribution.Should().NotBeNull();

            variables[0].TriangularDistribution.Min.Should().BeApproximately(
                (1 - variables[0].Mean) / variables[0].Sd, 1e-9);
            variables[0].TriangularDistribution.Max.Should().BeApproximately(
                (10 - variables[0].Mean) / variables[0].Sd, 1e-9);
            variables[0].TriangularDistribution.Mode.Should().BeApproximately(
                (2 - variables[0].Mean) / variables[0].Sd, 1e-9);

            variables[1].TriangularDistribution.Should().BeNull("min equals max cannot form a distribution");

            variables[2].TriangularDistribution.Should().NotBeNull();
            variables[2].TriangularDistribution.Min.Should().Be(0);
            variables[2].TriangularDistribution.Max.Should().Be(1);
            variables[2].TriangularDistribution.Mode.Should().Be(1);

            variables[3].TriangularDistribution.Should().NotBeNull();

            variables[3].TriangularDistribution.Mode.Should().BeApproximately(
                (0 - variables[3].Mean) / variables[3].Sd, 1e-9);
        }

        [Fact]
        public async Task Population_ModeZeroAndMeanNonZero_UsesAFixedSmallModeOfPointZeroOneAsync()
        {
            var (variables, _, _) = await PopulationAsync([0, 0, 0, 1, 2, 5]);

            variables[0].Mode.Should().Be(0);
            variables[0].Mean.Should().NotBe(0);
            variables[0].TriangularDistribution.Mode.Should().BeApproximately(0.01, 1e-12);
        }

        [Fact]
        public async Task Population_ColumnWithOnlyNonPositiveValuesAndAZeroMode_ClampsTheModeIntoTheRangeAsync()
        {
            var (variables, _, _) = await PopulationAsync([-5, -3, 0, 0, 0]);

            var distribution = variables[0].TriangularDistribution;
            distribution.Should().NotBeNull();
            distribution.Min.Should().Be(-5);
            distribution.Max.Should().Be(0);
            distribution.Mode.Should().Be(0);
        }

        [Fact]
        public async Task
            Population_ColumnWithMaximumZeroAndNegativeValues_IsTypedAsNoNormalisationLikeAnAllZeroColumn_CharacterisationAsync()
        {
            var (variables, _, _) = await PopulationAsync([-5, -3, 0, 0, 0]);

            variables[0].Max.Should().Be(0);
            variables[0].Min.Should().Be(-5);
            variables[0].NormalisationType.Should().Be(0);
        }

        [Fact]
        public async Task Population_ConstantColumn_StoresZeroKurtosisAndSkewnessInsteadOfNaNAsync()
        {
            var (variables, rows, _) = await PopulationAsync([5, 5, 5, 5]);

            variables[0].Kurtosis.Should().Be(0);
            variables[0].Skewness.Should().Be(0);
            var row = (ExhaustiveSearchInstanceVariable)rows.Inserted.Single();
            row.Kurtosis.Should().Be(0);
            row.Skewness.Should().Be(0);
            row.Iqr.Should().Be(0);
            row.StandardDeviation.Should().Be(0);
        }

        [Fact]
        public async Task Population_ColumnContainingNaN_StoresZeroedStatisticsAndHasNoDistributionAsync()
        {
            var variables = Vars(1);

            await TrainSupport.NewTraining(1).CalculateStatisticsAsync(9, variables,
                [[1], [double.NaN], [3], [4]], StatisticsTarget.Population, new TrainRecordingRepository(),
                new TrainRecordingRepository(), CancellationToken.None);

            variables[0].TriangularDistribution.Should().BeNull();
            variables[0].Mean.Should().Be(0);
            variables[0].Sd.Should().Be(0);
        }

        [Theory]
        [InlineData(double.NaN, 1, 0)]
        [InlineData(0, double.PositiveInfinity, 0)]
        [InlineData(1, 1, 1)]
        [InlineData(2, 1, 1)]
        [InlineData(0, 1, double.NaN)]
        public void BuildTriangularDistribution_ForADegenerateRange_ReturnsNull(double min, double max, double mode)
        {
            TriangularDistributionFactory.BuildTriangularDistribution(min, max, mode).Should().BeNull();
        }

        [Theory]
        [InlineData(-5, 0, 0.01, 0)]
        [InlineData(0, 1, 0.01, 0.01)]
        [InlineData(0.5, 1, 0.01, 0.5)]
        public void BuildTriangularDistribution_ClampsTheModeIntoTheRange(double min, double max, double mode,
            double expected)
        {
            TriangularDistributionFactory.BuildTriangularDistribution(min, max, mode).Mode.Should().Be(expected);
        }

        [Fact]
        public async Task Population_StoredBinCount_FollowsTheSturgesLikeRuleAndCapsAtTwentyAsync()
        {
            var random = new Random(3);
            var continuous = Enumerable.Range(0, 200).Select(_ => random.NextDouble() * 100).ToArray();
            var bigContinuous = Enumerable.Range(0, 5000).Select(_ => random.NextDouble() * 100).ToArray();
            var binary = Enumerable.Range(0, 200).Select(i => (double)(i % 2)).ToArray();
            var zeroIqr = Enumerable.Range(0, 200).Select(i => i < 190 ? 5d : i).ToArray();

            var (variables, _, _) = await PopulationAsync(continuous, [.. bigContinuous.Take(200)], binary,
                zeroIqr);

            variables[0].Bins.Should().Be((int)Math.Round(1d + 3.3d * Math.Log(200)));
            variables[2].Bins.Should().Be(2);
            variables[3].Bins.Should().Be(20);
            variables[0].Bins.Should().BeLessThanOrEqualTo(20);
        }

        [Fact]
        public async Task Population_HistogramRows_AreWrittenPerBinInSequenceForTheInsertedVariableIdAsync()
        {
            var random = new Random(8);

            var (variables, _, histograms) = await PopulationAsync(
                [.. Enumerable.Range(0, 100).Select(_ => random.NextDouble())],
                [.. Enumerable.Range(0, 100).Select(_ => random.NextDouble())]);

            var rows = histograms.Inserted.Cast<ExhaustiveSearchInstanceVariableHistogram>().ToList();
            rows.Should().NotBeEmpty();

            rows.Select(r => r.ExhaustiveSearchInstanceVariableId).Distinct().Should()
                .BeEquivalentTo(variables.Values.Select(v => (int?)v.ExhaustiveSearchInstanceVariableId));

            foreach (var group in rows.GroupBy(r => r.ExhaustiveSearchInstanceVariableId))
            {
                group.Select(g => g.BinSequence!.Value).Should().Equal(Enumerable.Range(0, group.Count()));
                group.Sum(g => g.Frequency).Should().Be(100);
                group.Should().OnlyContain(g => g.BinRangeStart <= g.BinRangeEnd);
            }
        }

        [Fact]
        public async Task Population_HistogramBinCount_IgnoresTheComputedBinCount_H17Async()
        {
            var random = new Random(4);
            var column = Enumerable.Range(0, 100).Select(_ => random.NextDouble() * 50).ToArray();

            var (variables, _, histograms) = await PopulationAsync(column);

            var histogramBins = histograms.Inserted.Count;
            variables[0].Bins.Should().Be((int)Math.Round(1d + 3.3d * Math.Log(100)));

            histogramBins.Should().NotBe(variables[0].Bins,
                "Histogram.Compute is not given the computed bin count (H17 left as is)");
        }

        [Fact]
        public async Task
            Classification_WritesClassificationRowsLinkedToTheExistingVariableIds_AndLeavesVariablesAloneAsync()
        {
            var variables = Vars(2);
            variables[0].ExhaustiveSearchInstanceVariableId = 11;
            variables[1].ExhaustiveSearchInstanceVariableId = 12;
            var rows = new TrainRecordingRepository();
            var histograms = new TrainRecordingRepository();
            double[][] data = [[1, 0], [2, 1], [3, 1], [4, 0]];

            await TrainSupport.NewTraining(1).CalculateStatisticsAsync(9, variables, data,
                StatisticsTarget.Classification, rows, histograms, CancellationToken.None);

            var written = rows.Inserted.Cast<ExhaustiveSearchInstanceVariableClassification>().ToList();
            written.Select(w => w.ExhaustiveSearchInstanceVariableId).Should().Equal(11, 12);
            written[0].Mean.Should().BeApproximately(2.5, 1e-12);
            variables[0].Mean.Should().Be(0);
            variables[0].NormalisationType.Should().Be(0);
            histograms.Inserted.Should().AllBeOfType<ExhaustiveSearchInstanceVariableHistogramClassification>();

            histograms.Inserted.Cast<ExhaustiveSearchInstanceVariableHistogramClassification>()
                .Select(h => h.ExhaustiveSearchInstanceVariableClassificationId).Distinct().Should()
                .Equal(1, 2);
        }

        [Fact]
        public async Task Anomaly_WritesAnomalyRowsLinkedToTheExistingVariableIdsAsync()
        {
            var variables = Vars(1);
            variables[0].ExhaustiveSearchInstanceVariableId = 21;
            var rows = new TrainRecordingRepository();
            var histograms = new TrainRecordingRepository();
            double[][] data = [[1], [2], [3], [4]];

            await TrainSupport.NewTraining(1).CalculateStatisticsAsync(9, variables, data,
                StatisticsTarget.Anomaly, rows, histograms, CancellationToken.None);

            rows.Inserted.Should().ContainSingle().Which.Should().BeOfType<ExhaustiveSearchInstanceVariableAnomaly>()
                .Which.ExhaustiveSearchInstanceVariableId.Should().Be(21);

            histograms.Inserted.Should().AllBeOfType<ExhaustiveSearchInstanceVariableHistogramAnomaly>();
        }

        [Fact]
        public async Task TheOriginalSignature_ResolvesTheTargetFromTheRepositoryTypeAsync()
        {
            var variables = Vars(1);
            var rows = new TrainRecordingRepository();

            await TrainSupport.NewTraining(1).CalculateStatisticsAsync(9, variables, [[1], [2], [3]], rows,
                new TrainRecordingRepository(), CancellationToken.None);

            rows.Inserted.Single().Should().BeOfType<ExhaustiveSearchInstanceVariableAnomaly>();
            StatisticsTargetResolver.ResolveStatisticsTarget(rows).Should().Be(StatisticsTarget.Anomaly);

            StatisticsTargetResolver.ResolveStatisticsTarget(new ExhaustiveSearchInstanceVariableRepository(null!))
                .Should().Be(StatisticsTarget.Population);
            StatisticsTargetResolver
                .ResolveStatisticsTarget(new ExhaustiveSearchInstanceVariableClassificationRepository(null!))
                .Should().Be(StatisticsTarget.Classification);
        }

        [Fact]
        public async Task OnlyTheFirstVariablesCountRowsAreCovered_WhenTheMatrixHasMoreColumnsAsync()
        {
            var variables = Vars(2);
            var rows = new TrainRecordingRepository();

            await TrainSupport.NewTraining(1).CalculateStatisticsAsync(9, variables, [[1, 5, 9], [2, 6, 9], [3, 7, 9]],
                StatisticsTarget.Population, rows, new TrainRecordingRepository(), CancellationToken.None);

            rows.Inserted.Should().HaveCount(2);
        }

        [Fact]
        public async Task MockPopulation_ClassifiesEveryColumnAndBuildsADistributionForEveryNonConstantColumnAsync()
        {
            var (header, mockRows) = TrainSupport.ReadMock();

            var variables = Enumerable.Range(1, header.Length - 1)
                .ToDictionary(i => i - 1, i => new Variable { Name = header[i] });

            var data = mockRows.Select(r => r.Skip(1).ToArray()).ToArray();
            var rows = new TrainRecordingRepository();

            await TrainSupport.NewTraining(1).CalculateStatisticsAsync(1, variables, data,
                StatisticsTarget.Population, rows, new TrainRecordingRepository(), CancellationToken.None);

            rows.Inserted.Should().HaveCount(26);

            for (var i = 0; i < 26; i++)
            {
                var column = data.Select(r => r[i]).ToArray();
                var min = column.Min();
                var max = column.Max();
                var expectedType = max == 0 ? 0 : min == 0 && Math.Abs(max - 1) < 0.0001 ? 1 : 2;
                variables[i].NormalisationType.Should().Be((byte)expectedType, header[i + 1]);
                variables[i].Mean.Should().BeApproximately(column.Average(), 1e-9);
                (variables[i].TriangularDistribution != null).Should().Be(min < max, header[i + 1]);
            }
        }
    }
}