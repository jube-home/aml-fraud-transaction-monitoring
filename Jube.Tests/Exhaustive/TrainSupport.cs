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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Engine.Exhaustive;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Interfaces;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Helpers;
using log4net;

namespace Jube.Test.Exhaustive
{
    internal sealed record TrainPrepared(Dictionary<int, Variable> Variables, double[][] Data, double[] Outputs);

    internal static class TrainSupport
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(TrainSupport));

        public static ExhaustiveTrainingSettings Settings(int trialsLimit = 4, int minVariables = 5,
            int maxVariables = 30)
        {
            return new ExhaustiveTrainingSettings
            {
                TrialsLimit = trialsLimit,
                MinVariableCount = minVariables,
                MaxVariableCount = maxVariables,
                TrainingDataSamplePercentage = 0.6,
                CrossValidationDataSamplePercentage = 0.2,
                TestingDataSamplePercentage = 0.2,
                ValidationTestingActivationThreshold = 0.5,
                TopologySinceImprovementLimit = 2,
                LayerDepthLimit = 4,
                LayerWidthLimitInputLayerFactor = 4,
                TopologyComplexityLimit = 10000,
                ActivationFunctionExplorationEpochs = 3,
                TopologyExplorationEpochs = 2,
                TopologyFinalisationEpochs = 2,
                SimulationsCount = 100
            };
        }

        public static Training NewTraining(int seed)
        {
            return new Training(Log, null!, null!) { seeded = new Random(seed) };
        }

        public static (string[] Header, List<double[]> Rows) ReadMock()
        {
            var lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Exhaustive", "Mock.csv"))
                .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            var header = lines[0].Split(',');

            var rows = lines.Skip(1)
                .Select(l => l.Split(',').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray())
                .ToList();

            return (header, rows);
        }

        public static async Task<TrainPrepared> PrepareMockAsync(int rowCount, int seed, int variableCount = 26)
        {
            var (header, allRows) = ReadMock();
            var random = new Random(seed);
            var sample = allRows.OrderBy(_ => random.Next()).Take(rowCount).ToList();

            var variables = new Dictionary<int, Variable>();

            for (var i = 0; i < variableCount; i++)
            {
                variables[i] = new Variable { Name = header[i + 1], ProcessingTypeId = 1 };
            }

            var data = sample.Select(r => r.Skip(1).Take(variableCount).ToArray()).ToArray();
            var outputs = sample.Select(r => r[0]).ToArray();

            var training = NewTraining(seed);

            await training.CalculateStatisticsAsync(1, variables, data, StatisticsTarget.Population,
                new TrainRecordingRepository(), new TrainRecordingRepository(), CancellationToken.None);

            Training.Shuffle(new Random(seed), data, outputs, out data, out outputs);
            data = training.NormaliseData(variables, data);
            training.DatasetSymmetry(data, outputs, out data, out outputs);

            return new TrainPrepared(variables, data, outputs);
        }

        public static Supervised NewSupervised(ExhaustiveTrainingSettings settings, ITrialRecorder recorder,
            TrainPrepared prepared, int seed)
        {
            return new Supervised(settings, recorder, 1, new JsonSerializationHelper(),
                new Random(seed), prepared.Variables, prepared.Data, prepared.Outputs, Log);
        }
    }
}