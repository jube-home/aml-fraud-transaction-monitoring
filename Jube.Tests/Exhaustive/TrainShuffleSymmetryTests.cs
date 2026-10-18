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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.Exhaustive;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class TrainShuffleSymmetryTests
    {
        private static (double[][] Data, double[] Outputs) Rows(int count, Func<int, double> label)
        {
            var data = Enumerable.Range(0, count).Select(i => new[] { i, i * 10d }).ToArray();
            var outputs = Enumerable.Range(0, count).Select(label).ToArray();

            return (data, outputs);
        }

        [Fact]
        public void Shuffle_IsAPermutationThatKeepsEveryRowPairedWithItsLabel()
        {
            var (data, outputs) = Rows(200, i => i % 3 == 0 ? 1 : 0);

            Training.Shuffle(new Random(5), data, outputs, out var shuffled, out var shuffledOutputs);

            shuffled.Select(r => r[0]).Should().BeEquivalentTo(data.Select(r => r[0]));
            shuffled.Select(r => r[0]).Should().NotEqual(data.Select(r => r[0]));

            for (var i = 0; i < shuffled.Length; i++)
            {
                var original = (int)shuffled[i][0];
                shuffledOutputs[i].Should().Be(outputs[original]);
                shuffled[i][1].Should().Be(original * 10d);
            }
        }

        [Fact]
        public void Shuffle_SameSeedGivesTheSameOrder_DifferentSeedsGiveDifferentOrders()
        {
            var (data, outputs) = Rows(100, i => i % 2);

            Training.Shuffle(new Random(1), data, outputs, out var a, out _);
            Training.Shuffle(new Random(1), data, outputs, out var b, out _);
            Training.Shuffle(new Random(2), data, outputs, out var c, out _);

            a.Select(r => r[0]).Should().Equal(b.Select(r => r[0]));
            a.Select(r => r[0]).Should().NotEqual(c.Select(r => r[0]));
        }

        [Fact]
        public void Shuffle_DoesNotMutateItsInputs_AndReusesTheRowInstances()
        {
            var (data, outputs) = Rows(30, i => i % 2);
            var originalOrder = data.Select(r => r[0]).ToArray();
            var originalOutputs = outputs.ToArray();

            Training.Shuffle(new Random(3), data, outputs, out var shuffled, out var shuffledOutputs);

            data.Select(r => r[0]).Should().Equal(originalOrder);
            outputs.Should().Equal(originalOutputs);
            shuffled.Should().NotBeSameAs(data);
            shuffled.Should().OnlyContain(r => data.Contains(r));
            shuffledOutputs.Should().NotBeSameAs(outputs);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Shuffle_EmptyAndSingleRow_AreReturnedUnchanged(int count)
        {
            var (data, outputs) = Rows(count, _ => 1);

            Training.Shuffle(new Random(3), data, outputs, out var shuffled, out var shuffledOutputs);

            shuffled.Should().HaveCount(count);
            shuffledOutputs.Should().Equal(outputs);
        }

        [Fact]
        public void Shuffle_IsUniformEnoughThatEveryRowVisitsEveryPositionOverManySeeds()
        {
            var (data, outputs) = Rows(5, _ => 0);
            var seen = new HashSet<(int Row, int Position)>();

            for (var seed = 0; seed < 400; seed++)
            {
                Training.Shuffle(new Random(seed), data, outputs, out var shuffled, out _);

                for (var position = 0; position < 5; position++)
                {
                    seen.Add(((int)shuffled[position][0], position));
                }
            }

            seen.Should().HaveCount(25);
        }

        [Fact]
        public void Shuffle_DoesNotPreserveClassStratification_ClassRatioOfTheFirstHalfVaries()
        {
            var (data, outputs) = Rows(200, i => i < 100 ? 1 : 0);
            var fractions = new List<double>();

            for (var seed = 0; seed < 20; seed++)
            {
                Training.Shuffle(new Random(seed), data, outputs, out _, out var shuffled);
                fractions.Add(shuffled.Take(100).Average());
            }

            fractions.Distinct().Count().Should().BeGreaterThan(3);
        }

        [Fact]
        public void DatasetSymmetry_RemovesOnlyMajorityRows_KeepsOrder_AndIsSeedDeterministic()
        {
            var (data, outputs) = Rows(60, i => i % 5 == 0 ? 1 : 0);

            TrainSupport.NewTraining(4).DatasetSymmetry(data, outputs, out var d1, out var o1);
            TrainSupport.NewTraining(4).DatasetSymmetry(data, outputs, out var d2, out var o2);

            d1.Select(r => r[0]).Should().Equal(d2.Select(r => r[0]));
            o1.Should().Equal(o2);
            o1.Count(o => o == 1).Should().Be(12);
            o1.Count(o => o == 0).Should().Be(12);
            d1.Select(r => r[0]).Should().BeInAscendingOrder();
            d1.Select(r => r[0]).Where(v => (int)v % 5 == 0).Should().HaveCount(12);
        }

        [Fact]
        public void DatasetSymmetry_MajorityPositive_RemovesPositivesInstead()
        {
            var (data, outputs) = Rows(30, i => i % 3 == 0 ? 0 : 1);

            TrainSupport.NewTraining(2).DatasetSymmetry(data, outputs, out var d, out var o);

            o.Count(x => x == 0).Should().Be(10);
            o.Count(x => x == 1).Should().Be(10);
            d.Select(r => r[0]).Where(v => (int)v % 3 == 0).Should().HaveCount(10);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void DatasetSymmetry_SingleClass_RemovesEveryRow_Characterisation(double label)
        {
            var (data, outputs) = Rows(10, _ => label);

            TrainSupport.NewTraining(2).DatasetSymmetry(data, outputs, out var d, out var o);

            d.Should().BeEmpty();
            o.Should().BeEmpty();
        }

        [Fact]
        public void DatasetSymmetry_DoesNotMutateItsInputs_AndKeepsTheRowInstances()
        {
            var (data, outputs) = Rows(21, i => i < 3 ? 1 : 0);
            var copy = outputs.ToArray();

            TrainSupport.NewTraining(2).DatasetSymmetry(data, outputs, out var d, out _);

            outputs.Should().Equal(copy);
            data.Should().HaveCount(21);
            d.Should().OnlyContain(r => data.Contains(r));
        }

        [Fact]
        public void DatasetSymmetry_DifferentSeedsPickDifferentSurvivors()
        {
            var (data, outputs) = Rows(100, i => i < 10 ? 1 : 0);

            var survivors = Enumerable.Range(0, 8).Select(seed =>
            {
                TrainSupport.NewTraining(seed).DatasetSymmetry(data, outputs, out var d, out _);

                return string.Join(",", d.Select(r => (int)r[0]));
            }).Distinct();

            survivors.Count().Should().BeGreaterThan(3);
        }

        [Fact]
        public void DatasetSymmetry_ExplicitTargetBelowBothClasses_TrimsBothClassesToTheTarget()
        {
            var (data, outputs) = Rows(150, i => i < 100 ? 0 : 1);

            TrainSupport.NewTraining(3).DatasetSymmetry(data, outputs, 30, out var d, out var o);

            o.Count(x => x == 0).Should().Be(30);
            o.Count(x => x == 1).Should().Be(30);
            d.Should().HaveCount(60);
        }

        [Fact]
        public void
            DatasetSymmetry_ExplicitTargetAboveMinority_TrimsOnlyTheMajority_LeavingMinorityForBootstrapToTopUp()
        {
            var (data, outputs) = Rows(120, i => i < 100 ? 0 : 1);

            TrainSupport.NewTraining(3).DatasetSymmetry(data, outputs, 60, out var d, out var o);

            o.Count(x => x == 0).Should().Be(60);
            o.Count(x => x == 1).Should().Be(20);
            d.Should().HaveCount(80);
        }

        [Fact]
        public void DatasetSymmetry_ExplicitTargetAboveBothClasses_LeavesBothClassesUntouched()
        {
            var (data, outputs) = Rows(50, i => i < 30 ? 0 : 1);

            TrainSupport.NewTraining(3).DatasetSymmetry(data, outputs, 1000, out var d, out var o);

            o.Count(x => x == 0).Should().Be(30);
            o.Count(x => x == 1).Should().Be(20);
            d.Should().HaveCount(50);
        }

        [Fact]
        public void NormaliseData_OnlyZScoresTypeTwoColumns_AndMutatesInPlace()
        {
            var variables = new Dictionary<int, Variable>
            {
                [0] = new() { Mean = 2, Sd = 2, NormalisationType = 2 },
                [1] = new() { Mean = 2, Sd = 2, NormalisationType = 1 },
                [2] = new() { Mean = 2, Sd = 2, NormalisationType = 0 }
            };

            double[][] data = [[4, 4, 4], [0, 0, 0]];

            var result = TrainSupport.NewTraining(1).NormaliseData(variables, data);

            result.Should().BeSameAs(data);
            data[0].Should().Equal(1, 4, 4);
            data[1].Should().Equal(-1, 0, 0);
        }

        [Fact]
        public void NormaliseData_UsesTheFittedStatisticsForEveryRow_SoARowIsNotAffectedByOthers()
        {
            var variables = new Dictionary<int, Variable> { [0] = new() { Mean = 10, Sd = 5, NormalisationType = 2 } };
            double[][] a = [[20], [30]];
            double[][] b = [[20], [1000]];

            TrainSupport.NewTraining(1).NormaliseData(variables, a);
            TrainSupport.NewTraining(1).NormaliseData(variables, b);

            a[0][0].Should().Be(b[0][0]);
            a[0][0].Should().Be(2);
        }

        [Fact]
        public void NormaliseData_MatrixWithFewerColumnsThanVariables_Throws()
        {
            var variables = new Dictionary<int, Variable>
            {
                [0] = new() { NormalisationType = 2, Sd = 1 },
                [1] = new() { NormalisationType = 2, Sd = 1 }
            };

            var act = () => TrainSupport.NewTraining(1).NormaliseData(variables, [[1d]]);

            act.Should().Throw<IndexOutOfRangeException>();
        }

        [Fact]
        public async Task NormaliseData_FitOnPopulationIsAppliedToAllRows_LeakageOfTestRowsIntoTheStatistics_H6Async()
        {
            var variables = new Dictionary<int, Variable>
            {
                [0] = new() { Name = "a", NormalisationType = 2 }
            };

            var rows = Enumerable.Range(0, 100).Select(i => new[] { i < 80 ? 1d : 100d }).ToArray();
            var training = TrainSupport.NewTraining(1);

            await training.CalculateStatisticsAsync(1, variables, rows, StatisticsTarget.Population,
                new TrainRecordingRepository(), new TrainRecordingRepository(), default);

            var trainOnly = rows.Take(80).Select(r => r[0]).ToArray();

            variables[0].Mean.Should().BeApproximately(20.8, 1e-9);
            variables[0].Mean.Should().NotBeApproximately(trainOnly.Average(), 1);
        }

        [Fact]
        public void OnlyOutputWithClassificationForData_ReturnsThePositiveRowsInOrder_AndThrowsIfTheCountIsTooLow()
        {
            double[] outputs = [1, 0, 1, 1, 0];
            double[][] data = [[0], [1], [2], [3], [4]];

            Training.OnlyOutputWithClassificationForData(3, outputs, data).Select(r => r[0]).Should()
                .Equal(0, 2, 3);

            var act = () => Training.OnlyOutputWithClassificationForData(2, outputs, data);
            act.Should().Throw<IndexOutOfRangeException>();
        }

        [Fact]
        public void OnlyOutputWithClassificationForData_CountTooHigh_LeavesNullRows_Characterisation()
        {
            var result = Training.OnlyOutputWithClassificationForData(3, [1, 0, 1], [[0], [1], [2]]);

            result.Should().HaveCount(3);
            result[2].Should().BeNull();
        }
    }
}