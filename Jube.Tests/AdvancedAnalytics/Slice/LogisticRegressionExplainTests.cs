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
using Jube.Analytics;
using Jube.Engine.AdvancedAnalytics.Regression;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class LogisticRegressionExplainTests
    {
        private static LogisticRegressionPipeline Pipeline(double[] weights, double intercept,
            bool standardise = false, double[]? means = null, double[]? sds = null, int[]? indicatorInputs = null,
            Calibrator? calibrator = null)
        {
            var count = weights.Length - (indicatorInputs?.Length ?? 0);

            return new LogisticRegressionPipeline(standardise, false, new double[count],
                means ?? new double[count], sds ?? [.. Enumerable.Repeat(1d, count)],
                indicatorInputs ?? [], weights, intercept, calibrator, 0.5, 0.5);
        }

        [Fact]
        public void Explain_ContributionsAreWeightTimesInputAndSumWithInterceptToTheRawLogit()
        {
            var pipeline = Pipeline([2, -1], 0.5);

            var explain = pipeline.Explain([3, 4]);

            explain.Contributions.Should().Equal(6, -4);
            explain.Intercept.Should().Be(0.5);
            explain.RawLogit.Should().Be(2.5);
        }

        [Fact]
        public void Explain_RawLogitMatchesTheExistingRawLogitMethodExactly()
        {
            var pipeline = Pipeline([2, -1, 0.7], 0.5, true, [10, 5, 0], [2, 1, 1]);

            var input = new[] { 14d, 3d, -2d };

            pipeline.Explain(input).RawLogit.Should().Be(pipeline.RawLogit(input));
        }

        [Fact]
        public void Explain_ContributionsSumWithInterceptExactlyEvenWithStandardisation()
        {
            var pipeline = Pipeline([2, -1, 0.7], 0.5, true, [10, 5, 0], [2, 1, 1]);

            var explain = pipeline.Explain([14, 3, -2]);

            (explain.Contributions.Sum() + explain.Intercept).Should().Be(explain.RawLogit);
        }

        [Fact]
        public void Explain_AMissingInputContributesThroughItsIndicatorWeightNotTheRealWeight()
        {
            var pipeline = Pipeline([2, -1, 0.7], 0, false, null, null, [1]);

            var explain = pipeline.Explain([5, double.NaN]);

            explain.Contributions[0].Should().Be(10);
            explain.Contributions[1].Should().Be(0);
            explain.Contributions[2].Should().Be(0.7);
            explain.RawLogit.Should().Be(10.7);
        }

        [Fact]
        public void Explain_APresentInputWithAnIndicatorContributesZeroFromTheIndicatorWeight()
        {
            var pipeline = Pipeline([2, -1, 0.7], 0, false, null, null, [1]);

            var explain = pipeline.Explain([5, 9]);

            explain.Contributions[1].Should().Be(-9);
            explain.Contributions[2].Should().Be(0);
            explain.RawLogit.Should().Be(1);
        }
    }
}