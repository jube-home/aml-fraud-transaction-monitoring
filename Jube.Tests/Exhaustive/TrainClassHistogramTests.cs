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
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Statistics;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class TrainClassHistogramTests
    {
        private static double[] Ten => [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        private static double[] Start => [1, 4, 7];
        private static double[] End => [4, 7, 10];

        [Fact]
        public void HandCheckedTenValuesSplitOnTheGivenPopulationEdges()
        {
            double[] labels = [0, 1, 0, 0, 1, 0, 0, 0, 1, 0];

            var result = ClassHistogramComputer.Compute(Start, End, Ten, labels);

            result.BinStart.Should().Equal(1, 4, 7);
            result.BinEnd.Should().Equal(4, 7, 10);
            result.Positive.Should().Equal(1, 1, 1);
            result.Negative.Should().Equal(2, 2, 3);
        }

        [Fact]
        public void BoundaryValuesGoToTheUpperBinAndTheMaximumToTheLastBin()
        {
            var result = ClassHistogramComputer.Compute(Start, End, [1, 4, 7, 10, 3.999], [0, 0, 0, 0, 0]);

            result.Negative.Should().Equal(2, 1, 2);
        }

        [Fact]
        public void EveryRowLandsInExactlyOneBinSoClassCountsSumToTheRowCount()
        {
            var random = new Random(7);
            var values = Enumerable.Range(0, 500).Select(_ => random.NextDouble() * 40 - 10).ToArray();
            var labels = values.Select(_ => random.NextDouble() < 0.2 ? 1d : 0d).ToArray();
            var min = values.Min();
            var width = (values.Max() - min) / 7;
            var start = Enumerable.Range(0, 7).Select(i => min + i * width).ToArray();
            var end = Enumerable.Range(1, 7).Select(i => min + i * width).ToArray();

            var result = ClassHistogramComputer.Compute(start, end, values, labels);

            (result.Negative.Sum() + result.Positive.Sum()).Should().Be(500);
            result.Positive.Sum().Should().Be((int)labels.Sum());
            result.Negative.Length.Should().Be(7);
        }

        [Fact]
        public void IsDeterministicForTheSameInput()
        {
            var random = new Random(11);
            var values = Enumerable.Range(0, 300).Select(_ => random.NextDouble() * 5).ToArray();
            var labels = values.Select(v => v > 4 ? 1d : 0d).ToArray();

            var first = ClassHistogramComputer.Compute([0, 2.5], [2.5, 5], values, labels);
            var second = ClassHistogramComputer.Compute([0, 2.5], [2.5, 5], values, labels);

            second.Negative.Should().Equal(first.Negative);
            second.Positive.Should().Equal(first.Positive);
        }

        [Fact]
        public void FilterModeKeepsThePopulationAsTheNegativeClassAndClampsPositivesOutsideTheEdges()
        {
            double[] positives = [-5, 5, 50];
            var labelled = Ten.Concat(positives).ToArray();
            var labels = Ten.Select(_ => 0d).Concat(positives.Select(_ => 1d)).ToArray();

            var result = ClassHistogramComputer.Compute(Start, End, labelled, labels);

            result.Negative.Should().Equal(3, 3, 4);
            result.Positive.Should().Equal(1, 1, 1);
        }

        [Fact]
        public void ConstantColumnIsOneBinAndEveryRowLandsInIt()
        {
            var result = ClassHistogramComputer.Compute([3], [3], [3, 3, 3, 3], [0, 1, 1, 0]);

            result.Negative.Should().Equal(2);
            result.Positive.Should().Equal(2);
        }

        [Fact]
        public void NoBinsYieldsNothingAndNaNValuesFallInTheFirstBin()
        {
            ClassHistogramComputer.Compute([], [], [], []).Should().BeNull();
            ClassHistogramComputer.Compute(Start, End, [double.NaN], [0]).Negative.Should().Equal(1, 0, 0);
        }

        [Fact]
        public async Task WriterInsertsNegativeThenPositiveBinsWithTheirLabelsAsync()
        {
            var result = ClassHistogramComputer.Compute(Start, End, Ten, [0, 1, 0, 0, 1, 0, 0, 0, 1, 0]);
            var repository = new TrainRecordingRepository();

            await ClassHistogramWriter.WriteAsync(42, result, repository, CancellationToken.None);

            var rows = repository.Inserted.Cast<ExhaustiveSearchInstanceVariableHistogramClass>().ToList();
            rows.Should().HaveCount(6);
            rows.Should().OnlyContain(r => r.ExhaustiveSearchInstanceVariableId == 42);
            rows.Take(3).Should().OnlyContain(r => r.ClassLabel == 0);
            rows.Skip(3).Should().OnlyContain(r => r.ClassLabel == 1);
            rows.Take(3).Select(r => r.Frequency).Should().Equal(2, 2, 3);
            rows.Skip(3).Select(r => r.Frequency).Should().Equal(1, 1, 1);
            rows.Take(3).Select(r => r.BinSequence).Should().Equal(0, 1, 2);
            rows.Take(3).Select(r => r.BinRangeStart).Should().Equal(1d, 4d, 7d);
        }

        [Fact]
        public async Task WriteAllProcessesEveryVariableWithItsOwnColumnAndItsOwnStoredEdgesAsync()
        {
            var variables = new Dictionary<int, Variable>
            {
                [0] = new() { Name = "a", ExhaustiveSearchInstanceVariableId = 100 },
                [1] = new() { Name = "b", ExhaustiveSearchInstanceVariableId = 200 }
            };

            var data = Ten.Select(v => new[] { v, 10 - v }).ToArray();
            double[] labels = [0, 0, 0, 0, 0, 0, 0, 1, 1, 1];
            var repository = new TrainRecordingRepository();
            var asked = new List<int>();

            await ClassHistogramWriter.WriteAllAsync(variables, data, labels, (id, _) =>
            {
                asked.Add(id);

                return Task.FromResult((new[] { 0d, 5d }, new[] { 5d, 10d }));
            }, repository, CancellationToken.None);

            asked.Should().Equal(100, 200);
            var rows = repository.Inserted.Cast<ExhaustiveSearchInstanceVariableHistogramClass>().ToList();

            rows.Where(r => r.ExhaustiveSearchInstanceVariableId == 100 && r.ClassLabel == 1)
                .Select(r => r.Frequency).Should().Equal(0, 3);
            rows.Where(r => r.ExhaustiveSearchInstanceVariableId == 200 && r.ClassLabel == 1)
                .Select(r => r.Frequency).Should().Equal(3, 0);
        }

        [Fact]
        public async Task WriteAllSkipsAVariableWhoseStoredPopulationHistogramIsEmptyAsync()
        {
            var variables = new Dictionary<int, Variable> { [0] = new() { ExhaustiveSearchInstanceVariableId = 1 } };
            var repository = new TrainRecordingRepository();

            await ClassHistogramWriter.WriteAllAsync(variables, [[1d], [2d]], [0, 1],
                (_, _) => Task.FromResult((Array.Empty<double>(), Array.Empty<double>())), repository,
                CancellationToken.None);

            repository.Inserted.Should().BeEmpty();
        }
    }
}