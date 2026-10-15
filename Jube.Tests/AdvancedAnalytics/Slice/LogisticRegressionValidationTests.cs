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
using System.Threading;
using FluentAssertions;
using Jube.Analytics;
using Jube.Engine.AdvancedAnalytics.Regression;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class LogisticRegressionValidationTests
    {
        private static readonly string[] names = ["a", "b"];

        private static (double[][] Inputs, int[] Labels) Good(int n = 200)
        {
            return (SliceData.Logistic(n, 101, 0d, [1.0, 0.5]).Inputs,
                SliceData.Logistic(n, 101, 0d, [1.0, 0.5]).Labels);
        }

        private static Action Train(double[][] inputs, int[] labels, string[]? overrideNames = null,
            LogisticRegressionOptions? options = null)
        {
            return () => LogisticRegressionTrainer.Train(inputs, labels, overrideNames ?? names, options);
        }

        [Fact]
        [Trait("Covers", "TrainLogisticRegression")]
        public void RaggedRowsAreRejected()
        {
            var (inputs, labels) = Good();
            inputs[7] = [1d];

            Train(inputs, labels).Should().Throw<ArgumentException>().WithMessage("*Row 7*same length*");
        }

        [Fact]
        public void LabelsOtherThanZeroAndOneAreRejected()
        {
            var (inputs, labels) = Good();
            labels[3] = 2;
            Train(inputs, labels).Should().Throw<ArgumentException>().WithMessage("*0 or 1*");

            labels[3] = int.MinValue;
            Train(inputs, labels).Should().Throw<ArgumentException>().WithMessage("*0 or 1*");
        }

        [Fact]
        public void NaNInputsAreRejectedUnderTheRejectPolicyAndImputedOtherwise()
        {
            var (inputs, labels) = Good();
            inputs[5][1] = double.NaN;

            Train(inputs, labels, options: new LogisticRegressionOptions
            {
                MissingValuePolicy = LogisticMissingValuePolicy.Reject
            }).Should().Throw<ArgumentException>().WithMessage("*Row 5*Reject*");

            var result = LogisticRegressionTrainer.Train(inputs, labels, names);
            result.Preprocessing.Single(p => p.Name == "b").MissingInTrain.Should().BeInRange(0, 1);
        }

        [Fact]
        public void ASingleClassIsRejected()
        {
            var (inputs, _) = Good();

            Train(inputs, new int[inputs.Length]).Should().Throw<ArgumentException>().WithMessage("*Both classes*");
            Train(inputs, [.. Enumerable.Repeat(1, inputs.Length)]).Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FewerRowsThanVariablesIsRejected()
        {
            var inputs = new[] { new[] { 1d, 2d, 3d }, new[] { 2d, 1d, 5d } };

            Action act = () => LogisticRegressionTrainer.Train(inputs, [0, 1], ["a", "b", "c"]);
            act.Should().Throw<ArgumentException>().WithMessage("*more rows than variables*");
        }

        [Fact]
        public void MismatchedLengthsBlankAndDuplicateNamesAndEmptyInputAreRejected()
        {
            var (inputs, labels) = Good();

            Train(inputs, [.. labels.Take(10)]).Should().Throw<ArgumentException>().WithMessage("*labels*");
            Train(inputs, labels, ["a"]).Should().Throw<ArgumentException>().WithMessage("*variable names*");
            Train(inputs, labels, ["a", "a"]).Should().Throw<ArgumentException>().WithMessage("*unique*");
            Train(inputs, labels, ["a", " "]).Should().Throw<ArgumentException>().WithMessage("*blank*");
            Train([], []).Should().Throw<ArgumentException>();
            Train(null!, labels).Should().Throw<ArgumentNullException>();
            Train(inputs, null!).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void NonsensicalOptionsAreRejected()
        {
            var (inputs, labels) = Good();

            foreach (var options in new[]
                     {
                         new LogisticRegressionOptions { ValidationFraction = -0.1 },
                         new LogisticRegressionOptions { TestFraction = 1d },
                         new LogisticRegressionOptions { ValidationFraction = 0.5, TestFraction = 0.5 },
                         new LogisticRegressionOptions { RidgeLambda = -1 },
                         new LogisticRegressionOptions { RidgeLambda = double.NaN },
                         new LogisticRegressionOptions { BaseRateOverride = 0d },
                         new LogisticRegressionOptions { BaseRateOverride = 1d },
                         new LogisticRegressionOptions { DecisionThreshold = 2d },
                         new LogisticRegressionOptions { MaximumIterations = 0 },
                         new LogisticRegressionOptions { Tolerance = 0 },
                         new LogisticRegressionOptions { Costs = new CostMatrix(0, 0) }
                     })
            {
                Train(inputs, labels, options: options).Should().Throw<ArgumentException>();
            }
        }

        [Fact]
        public void AVariableWithNoObservedTrainingValueIsRejected()
        {
            var (inputs, labels) = Good();

            foreach (var row in inputs)
            {
                row[1] = double.NaN;
            }

            Train(inputs, labels).Should().Throw<ArgumentException>().WithMessage("*no observed value*");
        }

        [Fact]
        public void EveryVariableConstantIsRejected()
        {
            var (inputs, labels) = Good();

            foreach (var row in inputs)
            {
                row[0] = 1d;
                row[1] = 2d;
            }

            Train(inputs, labels).Should().Throw<ArgumentException>().WithMessage("*constant*");
        }

        [Fact]
        public void ACancelledTokenStopsTrainingBeforeAnyWork()
        {
            var (inputs, labels) = Good();
            using var source = new CancellationTokenSource();
            source.Cancel();
            var token = source.Token;

            Action act = () => LogisticRegressionTrainer.Train(inputs, labels, names, null, token);
            act.Should().Throw<OperationCanceledException>();
        }

        [Fact]
        public void CancellationDuringALongFitIsHonoured()
        {
            var data = SliceData.Logistic(200000, 102, 0d, [1.0, 0.5, -0.4, 0.3, 0.2, 0.1], 10);

            foreach (var ridge in new[] { 0d, 5d })
            {
                using var source = new CancellationTokenSource();
                source.CancelAfter(TimeSpan.FromMilliseconds(30));
                var token = source.Token;

                Action act = () => LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names,
                    new LogisticRegressionOptions { RidgeLambda = ridge }, token);
                act.Should().Throw<OperationCanceledException>();
            }
        }
    }
}