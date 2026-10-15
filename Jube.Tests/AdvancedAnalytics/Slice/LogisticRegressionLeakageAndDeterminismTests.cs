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
using System.Linq;
using FluentAssertions;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Engine.AdvancedAnalytics.Regression;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class LogisticRegressionLeakageAndDeterminismTests
    {
        private static readonly LogisticRegressionOptions options = new() { Seed = 12 };

        private static SliceDataset Data()
        {
            return SliceData.Logistic(5000, 81, -1.0, [1.0, -0.7], 1, 0.05);
        }

        private static DataSplitIndices SplitOf(LogisticRegressionResult result)
        {
            return new DataSplitIndices(result.TrainRows, result.ValidationRows, result.TestRows);
        }

        private static LogisticRegressionResult Run(SliceDataset data, int[] labels, double[][] inputs,
            DataSplitIndices split, LogisticRegressionOptions? overrideOptions = null)
        {
            return LogisticRegressionTrainer.TrainWithSplit(inputs, labels, data.Names, overrideOptions ?? options,
                split);
        }

        [Fact]
        [Trait("Covers", "TrainLogisticRegression")]
        public void NoTestRowInfluencesPreprocessingFitOrCalibration()
        {
            var data = Data();
            var baseline = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names, options);
            var split = SplitOf(baseline);

            var flipped = (int[])data.Labels.Clone();
            var garbled = SliceData.Copy(data.Inputs);

            foreach (var row in split.Test)
            {
                flipped[row] = 1 - flipped[row];

                for (var j = 0; j < garbled[row].Length; j++)
                {
                    garbled[row][j] = j % 2 == 0 ? 1e6 : double.NaN;
                }
            }

            var tampered = Run(data, flipped, garbled, split);

            tampered.SnapshotJson.Should().Be(baseline.SnapshotJson);
            tampered.Weights.Should().Equal(baseline.Weights);
            tampered.Intercept.Should().Be(baseline.Intercept);
            tampered.Calibration.Should().Be(baseline.Calibration);
            tampered.Priors.Should().Be(baseline.Priors);
            tampered.Preprocessing.Should().BeEquivalentTo(baseline.Preprocessing);
            tampered.Coefficients.Should().BeEquivalentTo(baseline.Coefficients);
            tampered.ValidationMetrics.Should().BeEquivalentTo(baseline.ValidationMetrics);
            tampered.TrainMetrics.Should().BeEquivalentTo(baseline.TrainMetrics);
            tampered.TestMetrics.Should().NotBeEquivalentTo(baseline.TestMetrics);
        }

        [Fact]
        public void ValidationRowsChangeOnlyTheCalibrationAndTrainRowsChangeTheFit()
        {
            var data = Data();
            var baseline = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names, options);
            var split = SplitOf(baseline);

            var validationFlipped = (int[])data.Labels.Clone();

            foreach (var row in split.Validation)
            {
                validationFlipped[row] = 1 - validationFlipped[row];
            }

            var afterValidation = Run(data, validationFlipped, data.Inputs, split);
            afterValidation.Weights.Should().Equal(baseline.Weights);
            afterValidation.Intercept.Should().Be(baseline.Intercept);
            afterValidation.Preprocessing.Should().BeEquivalentTo(baseline.Preprocessing);
            afterValidation.Calibration.Slope.Should().NotBe(baseline.Calibration.Slope);

            var trainFlipped = (int[])data.Labels.Clone();

            foreach (var row in split.Train.Take(200))
            {
                trainFlipped[row] = 1 - trainFlipped[row];
            }

            var afterTrain = Run(data, trainFlipped, data.Inputs, split);
            afterTrain.Weights.Should().NotEqual(baseline.Weights);
        }

        [Fact]
        public void PreprocessingStatisticsComeFromTrainRowsAlone()
        {
            var data = Data();
            var result = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names, options);

            for (var j = 0; j < data.Names.Length; j++)
            {
                var values = result.TrainRows.Select(r => data.Inputs[r][j]).Where(double.IsFinite).ToArray();
                var scaler = Standardisation.Fit(values);
                var preprocessing = result.Preprocessing[j];

                preprocessing.Mean.Should().BeApproximately(scaler.Mean, 1e-12);
                preprocessing.StandardDeviation.Should().BeApproximately(scaler.StandardDeviation, 1e-12);

                preprocessing.FillValue.Should().BeApproximately(
                    Imputer.Fit(values, ImputationStrategy.Median).FillValue, 1e-12);

                preprocessing.MissingInTrain.Should().Be(
                    result.TrainRows.Count(r => double.IsNaN(data.Inputs[r][j])));
            }
        }

        [Fact]
        public void SplitsAreDisjointCoverEveryRowAndKeepClassProportions()
        {
            var data = Data();
            var result = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names, options);

            var all = result.TrainRows.Concat(result.ValidationRows).Concat(result.TestRows).ToArray();
            all.Distinct().Count().Should().Be(all.Length);
            all.Length.Should().Be(data.Labels.Length);

            var overall = data.Labels.Average();
            result.SplitSizes.Test.Should().BeInRange(990, 1010);
            (result.SplitSizes.TestPositives / (double)result.SplitSizes.Test).Should().BeApproximately(overall, 0.005);
            (result.SplitSizes.ValidationPositives / (double)result.SplitSizes.Validation).Should()
                .BeApproximately(overall, 0.005);
        }

        [Fact]
        public void TheSameSeedGivesByteIdenticalResultsAndADifferentSeedDoesNot()
        {
            var data = Data();

            var first = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names, options);
            var second = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names, options);

            var other = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names,
                options with { Seed = 13 });

            second.SnapshotJson.Should().Be(first.SnapshotJson);
            second.TrainRows.Should().Equal(first.TrainRows);
            second.TestRows.Should().Equal(first.TestRows);

            second.TestPredictions.Probabilities.Select(SliceData.Bits)
                .Should().Equal(first.TestPredictions.Probabilities.Select(SliceData.Bits));

            second.Coefficients.Should().BeEquivalentTo(first.Coefficients);
            second.Warnings.Should().BeEquivalentTo(first.Warnings);

            other.SnapshotJson.Should().NotBe(first.SnapshotJson);
            other.TestRows.Should().NotEqual(first.TestRows);
            first.Seed.Should().Be(12);
        }

        [Fact]
        public void TrainWithSplitRejectsOverlappingOrOutOfRangeSplits()
        {
            var data = Data();
            var inputs = data.Inputs;

            var overlapping = new DataSplitIndices([0, 1, 2, 3], [3], []);

            Action a = () =>
                LogisticRegressionTrainer.TrainWithSplit(inputs, data.Labels, data.Names, options, overlapping);

            a.Should().Throw<ArgumentException>().WithMessage("*more than one*");

            var outside = new DataSplitIndices([0, 1, 99999], [], []);

            Action b = () =>
                LogisticRegressionTrainer.TrainWithSplit(inputs, data.Labels, data.Names, options, outside);

            b.Should().Throw<ArgumentException>().WithMessage("*out of range*");
        }
    }
}