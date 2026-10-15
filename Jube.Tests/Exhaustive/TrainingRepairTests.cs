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
using Jube.Data.Repository.Interfaces;
using Jube.Engine.Exhaustive;
using Jube.Engine.Exhaustive.Algorithms.Models;
using log4net;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class TrainingRepairTests
    {
        private static Training NewTraining(int seed = 1)
        {
            return new Training(LogManager.GetLogger(typeof(TrainingRepairTests)), null!, null!)
            {
                seeded = new Random(seed)
            };
        }

        [Fact]
        public void NormaliseData_NormalisesEveryRowIncludingTheLast()
        {
            var variables = new Dictionary<int, Variable>
            {
                [0] = new() { Mean = 10, Sd = 2, NormalisationType = 2 },
                [1] = new() { Mean = 0, Sd = 1, NormalisationType = 0 }
            };

            double[][] data = [[12, 5], [8, 5], [10, 5], [14, 5]];

            var result = NewTraining().NormaliseData(variables, data);

            result.Select(r => r[0]).Should().Equal(1, -1, 0, 2);
            result.Select(r => r[1]).Should().Equal(5, 5, 5, 5);
        }

        [Fact]
        public void NormaliseData_ConstantNonBinaryColumn_ProducesZeroNotNaN()
        {
            var variables = new Dictionary<int, Variable>
            {
                [0] = new() { Mean = 7, Sd = 0, NormalisationType = 2 }
            };

            double[][] data = [[7], [7], [7]];

            var result = NewTraining().NormaliseData(variables, data);

            result.Select(r => r[0]).Should().Equal(0, 0, 0);
        }

        [Fact]
        public void DatasetSymmetry_BalancesClassesExactlyAndCanKeepTheLastRow()
        {
            var outputs = Enumerable.Repeat(0d, 8).Concat(Enumerable.Repeat(1d, 3)).ToArray();
            var data = Enumerable.Range(0, outputs.Length).Select(i => new[] { (double)i }).ToArray();

            for (var seed = 0; seed < 50; seed++)
            {
                NewTraining(seed).DatasetSymmetry(data, outputs, out var outData, out var outOutputs);

                outOutputs.Count(o => o == 1).Should().Be(3, "no positive row may be dropped (seed {0})", seed);
                outOutputs.Count(o => o == 0).Should().Be(3, "exactly the surplus is removed (seed {0})", seed);
                outData.Select(r => r[0]).Should().Contain(10, "the last row is a positive and is kept");
                outData.Length.Should().Be(outOutputs.Length);
            }
        }

        [Fact]
        public void DatasetSymmetry_Balanced_ReturnsEveryRow()
        {
            double[] outputs = [0, 1, 0, 1];
            double[][] data = [[0], [1], [2], [3]];

            NewTraining().DatasetSymmetry(data, outputs, out var outData, out var outOutputs);

            outData.Select(r => r[0]).Should().Equal(0, 1, 2, 3);
            outOutputs.Should().Equal(outputs);
        }

        [Fact]
        public void OnlyOutputWithClassificationForData_KeepsAPositiveFinalRow()
        {
            double[] outputs = [0, 1, 0, 1];
            double[][] data = [[0], [1], [2], [3]];

            var result = Training.OnlyOutputWithClassificationForData(2, outputs, data);

            result.Select(r => r[0]).Should().Equal(1, 3);
        }

        [Fact]
        public async Task CalculateStatistics_ProducesStatisticsForEveryVariableIncludingTheLastAsync()
        {
            var variables = new Dictionary<int, Variable>
            {
                [0] = new() { Name = "a" },
                [1] = new() { Name = "b" },
                [2] = new() { Name = "c" }
            };

            var random = new Random(4);

            var data = Enumerable.Range(0, 60)
                .Select(_ => new[] { random.NextDouble(), random.NextDouble() * 10, random.NextDouble() * 100 })
                .ToArray();

            var repositoryVariables = new RecordingRepository();
            var repositoryHistograms = new RecordingRepository();

            await NewTraining().CalculateStatisticsAsync(1, variables, data, repositoryVariables,
                repositoryHistograms, CancellationToken.None);

            repositoryVariables.Inserted.Should().HaveCount(3, "one statistics record per variable");
        }

        private class RecordingRepository : IGenericRepository
        {
            public List<object> Inserted { get; } = [];

            public Task<int> InsertAsync(object arg, CancellationToken token = default)
            {
                Inserted.Add(arg);

                return Task.FromResult(Inserted.Count);
            }
        }
    }
}