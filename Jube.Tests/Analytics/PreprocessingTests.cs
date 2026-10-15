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
using Xunit;

namespace Jube.Test.Analytics
{
    public class PreprocessingTests
    {
        private static readonly double[] values = [1, 2, 2, 3, double.NaN, 10, 4];

        [Fact]
        public void ZScore_FitIgnoresNaNAndMatchesIndependentValues()
        {
            var scaler = Standardisation.Fit(values);
            scaler.Mean.Should().BeApproximately(3.6666666666666665, 1e-12);
            scaler.StandardDeviation.Should().BeApproximately(3.265986323710904, 1e-12);
            scaler.Transform(3.6666666666666665).Should().BeApproximately(0, 1e-12);
            double.IsNaN(scaler.Transform(double.NaN)).Should().BeTrue();
            scaler.Inverse(scaler.Transform(7.5)).Should().BeApproximately(7.5, 1e-12);
        }

        [Fact]
        public void ZScore_ConstantColumnMapsToZero_SingleValueHasZeroSd()
        {
            var scaler = Standardisation.Fit([5, 5, 5]);
            scaler.StandardDeviation.Should().Be(0);
            scaler.Transform([5, 6]).Should().Equal(0, 0);
            Standardisation.Fit([4]).StandardDeviation.Should().Be(0);
        }

        [Fact]
        public void ZScore_IsFitOnTrainingOnlyAndReappliedUnchanged()
        {
            double[] train = [1, 2, 3, 4, 5];
            double[] other = [100, 200, 300];
            var scaler = Standardisation.Fit(train);
            var again = Standardisation.Fit(train);
            scaler.Should().Be(again);
            scaler.Transform(other)[0].Should().BeApproximately((100 - 3) / Math.Sqrt(2.5), 1e-12);
        }

        [Fact]
        public void MinMax_KnownAnswers_ClipAndConstant()
        {
            var scaler = MinMaxScaler.Fit(values);
            scaler.Minimum.Should().Be(1);
            scaler.Maximum.Should().Be(10);
            scaler.Transform(10).Should().Be(1);
            scaler.Transform(5.5).Should().BeApproximately(0.5, 1e-12);
            scaler.Transform(19).Should().BeApproximately(2, 1e-12);
            MinMaxScaler.Fit(values, true).Transform(19).Should().Be(1);
            MinMaxScaler.Fit(values, true).Transform(-9).Should().Be(0);
            double.IsNaN(scaler.Transform(double.NaN)).Should().BeTrue();
            MinMaxScaler.Fit([3, 3]).Transform(3).Should().Be(0);
            scaler.Transform([1, double.PositiveInfinity]).Length.Should().Be(2);
        }

        [Fact]
        public void Robust_KnownAnswers()
        {
            var scaler = RobustScaler.Fit(values);
            scaler.Median.Should().Be(2.5);
            scaler.Scale.Should().BeApproximately(1.482602218505602, 1e-12);
            scaler.Transform(2.5).Should().Be(0);
            scaler.Transform(4).Should().BeApproximately(1.5 / 1.482602218505602, 1e-12);
            double.IsNaN(scaler.Transform(double.NaN)).Should().BeTrue();
        }

        [Fact]
        public void Robust_OutlierDoesNotMoveTheFit()
        {
            var a = RobustScaler.Fit([1, 2, 3, 4, 5]);
            var b = RobustScaler.Fit([1, 2, 3, 4, 5000]);
            b.Median.Should().Be(a.Median);
            b.Scale.Should().Be(a.Scale);
        }

        [Fact]
        public void Robust_ZeroMadMapsToZero()
        {
            RobustScaler.Fit([2, 2, 2, 2, 9]).Transform(9).Should().Be(0);
        }

        [Fact]
        public void Winsoriser_QuantilesAndClamp()
        {
            var w = Winsoriser.Fit(values, 0.1, 0.9);
            w.Lower.Should().BeApproximately(1.5, 1e-12);
            w.Upper.Should().BeApproximately(7.0, 1e-12);
            w.Transform([1, 2, 10, double.NaN]).Take(3).Should().Equal(1.5, 2, 7);
            double.IsNaN(w.Transform(double.NaN)).Should().BeTrue();
            Winsoriser.Bounds(0, 1).Transform(5).Should().Be(1);
        }

        [Fact]
        public void Winsoriser_InvalidArgumentsThrow()
        {
            Action act = () => Winsoriser.Fit(values, 0.9, 0.1);
            act.Should().Throw<ArgumentOutOfRangeException>();
            act = () => Winsoriser.Fit(values, -0.1, 0.5);
            act.Should().Throw<ArgumentOutOfRangeException>();
            act = () => Winsoriser.Bounds(2, 1);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Imputer_MeanMedianModeConstant_KnownAnswers()
        {
            Imputer.Fit(values, ImputationStrategy.Mean).FillValue.Should().BeApproximately(3.6666666666666665, 1e-12);
            Imputer.Fit(values, ImputationStrategy.Median).FillValue.Should().Be(2.5);
            Imputer.Fit(values, ImputationStrategy.Mode).FillValue.Should().Be(2);
            Imputer.Fit(values, ImputationStrategy.Constant, -1).FillValue.Should().Be(-1);
        }

        [Fact]
        public void Imputer_ModeTiesResolveToSmallest()
        {
            Imputer.Fit([5, 5, 3, 3, 9], ImputationStrategy.Mode).FillValue.Should().Be(3);
        }

        [Fact]
        public void Imputer_ReplacesNaNAndInfinityAndFlagsThem()
        {
            var imputer = Imputer.Fit(values, ImputationStrategy.Median, addMissingIndicator: true);
            double[] recall = [1, double.NaN, double.PositiveInfinity, 8];
            imputer.Transform(recall).Should().Equal(1, 2.5, 2.5, 8);
            imputer.MissingIndicator(recall).Should().Equal(0, 1, 1, 0);
            imputer.AddMissingIndicator.Should().BeTrue();
        }

        [Fact]
        public void Imputer_ConstantNeedsNoData_OthersNeedFiniteValues()
        {
            Imputer.Fit([double.NaN], ImputationStrategy.Constant, 7).Transform(double.NaN).Should().Be(7);
            Action act = () => Imputer.Fit([double.NaN, double.NaN], ImputationStrategy.Mean);
            act.Should().Throw<ArgumentException>();
            act = () => Imputer.Fit([1], ImputationStrategy.Constant, double.NaN);
            act.Should().Throw<ArgumentException>();
            act = () => Imputer.Fit(null!, ImputationStrategy.Mean);
            act.Should().Throw<ArgumentNullException>();
            act = () => Imputer.Fit([1], (ImputationStrategy)9);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void FitOnEmptyOrAllMissing_ThrowsForEveryScaler()
        {
            Action act = () => Standardisation.Fit([]);
            act.Should().Throw<ArgumentException>();
            act = () => MinMaxScaler.Fit([double.NaN]);
            act.Should().Throw<ArgumentException>();
            act = () => RobustScaler.Fit([double.PositiveInfinity]);
            act.Should().Throw<ArgumentException>();
            act = () => Winsoriser.Fit([]);
            act.Should().Throw<ArgumentException>();
            act = () => Standardisation.Fit(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Pipeline_ImputeThenScale_MatchesResolveForTheMean()
        {
            var train = new[] { 1d, 2, 3, 4, double.NaN };
            var imputer = Imputer.Fit(train, ImputationStrategy.Mean);
            var scaler = Standardisation.Fit(imputer.Transform(train));
            var viaPipeline = scaler.Transform(imputer.Transform(double.NaN));
            viaPipeline.Should().BeApproximately(0, 1e-12);
        }
    }
}