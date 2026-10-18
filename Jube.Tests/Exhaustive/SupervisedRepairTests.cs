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
using Accord.Neuro;
using FluentAssertions;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Promotion;
using Jube.Engine.Exhaustive.Selection;
using Jube.Engine.Exhaustive.Topology;
using log4net;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class SupervisedRepairTests
    {
        private static Supervised NewSupervised(Random? seeded = null, Dictionary<int, Variable>? variables = null,
            double[][]? data = null)
        {
            return new Supervised(new ExhaustiveTrainingSettings(), null!, 0, null!, seeded ?? new Random(1),
                variables ?? new Dictionary<int, Variable>(),
                data ?? [], [],
                LogManager.GetLogger(typeof(SupervisedRepairTests)));
        }

        [Theory]
        [InlineData(1000, 0.6, 0.2, 0.2)]
        [InlineData(1826, 0.6, 0.2, 0.2)]
        [InlineData(101, 0.5, 0.25, 0.25)]
        public void SplitData_TrainCrossValidationAndTest_AreDisjointAndAligned(int n, double train, double cv,
            double test)
        {
            var data = Enumerable.Range(0, n).Select(i => new[] { (double)i, i % 2 }).ToArray();
            var outputs = Enumerable.Range(0, n).Select(i => new[] { (double)(i % 2) }).ToArray();

            NewSupervised().SplitDataForTest(train, cv, test, data, outputs,
                out var dTrain, out var oTrain, out var dCv, out var oCv, out var dTest, out var oTest);

            var trainIds = dTrain.Select(r => r[0]).ToHashSet();
            var cvIds = dCv.Select(r => r[0]).ToHashSet();
            var testIds = dTest.Select(r => r[0]).ToHashSet();

            trainIds.Overlaps(cvIds).Should().BeFalse("cross validation rows must not be training rows");
            trainIds.Overlaps(testIds).Should().BeFalse("test rows must not be training rows");
            cvIds.Overlaps(testIds).Should().BeFalse("test rows must not be cross validation rows");

            oTrain.Length.Should().Be(dTrain.Length);
            oCv.Length.Should().Be(dCv.Length);
            oTest.Length.Should().Be(dTest.Length);
            dTrain.Select(r => r[1]).Should().Equal(oTrain.Select(r => r[0]));
            dCv.Select(r => r[1]).Should().Equal(oCv.Select(r => r[0]));
            dTest.Select(r => r[1]).Should().Equal(oTest.Select(r => r[0]));
            dTest.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ReduceDataForOnlyTrialVariables_PopulatesEveryTrialColumnInOrder()
        {
            double[][] data =
            [
                [10, 11, 12, 13, 14, 15],
                [20, 21, 22, 23, 24, 25]
            ];

            var trialVariables = new Dictionary<int, TrialVariable>
            {
                [1] = new(), [3] = new(), [4] = new(), [5] = new(), [0] = new()
            };

            var reduced = NewSupervised(data: data).Selector.ReduceDataForOnlyTrialVariables(trialVariables);

            reduced.Should().HaveCount(2);
            reduced[0].Should().Equal(11, 13, 14, 15, 10);
            reduced[1].Should().Equal(21, 23, 24, 25, 20);
        }

        [Theory]
        [InlineData(27, 5)]
        [InlineData(27, 26)]
        [InlineData(27, 27)]
        [InlineData(5, 9)]
        [InlineData(0, 3)]
        public void DrawVariableIndices_AreDistinctAndInRange(int variableCount, int count)
        {
            var drawn = VariableSelector.DrawVariableIndices(new Random(3), variableCount, count);

            drawn.Distinct().Should().HaveCount(drawn.Length);
            drawn.Should().HaveCount(Math.Min(variableCount, count));
            drawn.All(i => i >= 0 && i < variableCount).Should().BeTrue();
        }

        [Fact]
        public void DrawVariableIndices_MinimumSubset_CanReachTheLastVariable()
        {
            var random = new Random(11);
            var seen = new HashSet<int>();

            for (var trial = 0; trial < 2000; trial++)
            {
                foreach (var i in VariableSelector.DrawVariableIndices(random, 27, 5))
                {
                    seen.Add(i);
                }
            }

            seen.Should().HaveCount(27);
            seen.Should().Contain(26);
        }

        [Fact]
        public void LeastSensitiveVariableKey_EliminationRemovesLeastSensitiveEveryTimeAndNeverRunsOffTheEnd()
        {
            var sensitivity = new Dictionary<int, double>();

            for (var key = 0; key < 12; key++)
            {
                sensitivity[key] = (key * 7 % 12 + 1) / 10d;
            }

            var expectedOrder = sensitivity.OrderBy(p => p.Value).Select(p => p.Key).ToList();
            var removed = new List<int>();
            var remaining = new Dictionary<int, double>(sensitivity);

            while (remaining.Count > 0)
            {
                var ranking = remaining.OrderBy(p => Math.Abs(p.Value)).ToDictionary(p => p.Key, p => p.Value);
                var key = VariableSelector.LeastSensitiveVariableKey(ranking);
                removed.Add(key);
                remaining.Remove(key);
            }

            removed.Should().Equal(expectedOrder);
        }

        [Fact]
        public void Sensitivity_PerturbsByAFactorOfEightyToOneTwentyPercent_NotBySquaringTheValue()
        {
            var network = new ActivationNetwork(new IdentityFunction(), 1, [], 1);
            network.Layers[0].Neurons[0].Weights[0] = 1;
            ((ActivationNeuron)network.Layers[0].Neurons[0]).Threshold = 0;

            double[][] inputs = [[10], [10], [-10], [10]];
            double[] baseline = [10, 10, -10, 10];

            var differences = Performance.Sensitivity(new Random(5), network, inputs, 0, baseline);

            differences.Should().HaveCount(4);
            differences.All(d => d <= 2.0 + 1e-9).Should().BeTrue();
            differences.Any(d => d > 0).Should().BeTrue();
        }

        [Fact]
        public void InterquartileRange_IsNonNegativeAndInOriginalUnits()
        {
            var values = Enumerable.Range(1, 100).Select(i => (double)i).ToArray();

            PrescriptionBuilder.InterquartileRange(values).Should().BeApproximately(50, 1.5);
            PrescriptionBuilder.InterquartileRange([5, 5, 5, 5]).Should().Be(0);
        }

        [Fact]
        public void TryGrowLayers_WidensUntilWideThenAddsALayerUntilTheDepthLimit()
        {
            var layers = new List<int> { 1 };

            for (var expected = 2; expected <= 11; expected++)
            {
                TopologyLayers.TryGrowLayers(layers, layers[^1], 5, 2, 3).Should().BeTrue();
                layers.Should().Equal(expected);
            }

            TopologyLayers.TryGrowLayers(layers, layers[^1], 5, 2, 3).Should().BeTrue();
            layers.Should().Equal(11, 1);

            for (var i = 0; i < 10; i++)
            {
                TopologyLayers.TryGrowLayers(layers, layers[^1], 5, 2, 3);
            }

            TopologyLayers.TryGrowLayers(layers, layers[^1], 5, 2, 3).Should().BeTrue();
            layers.Should().Equal(11, 11, 1);

            for (var i = 0; i < 10; i++)
            {
                TopologyLayers.TryGrowLayers(layers, layers[^1], 5, 2, 3);
            }

            TopologyLayers.TryGrowLayers(layers, layers[^1], 5, 2, 3).Should().BeFalse();
            layers.Should().Equal(11, 11, 11);
        }

        [Fact]
        public void Performance_Score_RewardsGoodModelsAndDoesNotRewardInvertedOnes()
        {
            double[][] actual = [[1], [1], [1], [0], [0], [0]];
            double[] good = [0.9, 0.8, 0.7, 0.2, 0.1, 0.3];
            double[] inverted = [0.1, 0.2, 0.3, 0.8, 0.9, 0.7];

            var performance = new Performance();

            performance.CalculatePerformance(good, actual, 0.5).Should().BeApproximately(1, 1e-9);
            performance.Tp.Should().Be(3);
            performance.Tn.Should().Be(3);

            performance.CalculatePerformance(inverted, actual, 0.5).Should().BeLessThanOrEqualTo(0);
            performance.Fp.Should().Be(3);
            performance.Fn.Should().Be(3);
        }
    }
}