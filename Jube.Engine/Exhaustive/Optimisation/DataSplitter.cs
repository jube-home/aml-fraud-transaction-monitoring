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
using Jube.Analytics;
using Jube.Data.Poco;
using log4net;

namespace Jube.Engine.Exhaustive.Optimisation
{
    internal class DataSplitter(int splitSeed, ILog log)
    {
        internal void SplitData(
            double trainingDataSamplePercentage,
            double crossValidationDataSamplePercentage,
            double testingDataSamplePercentage,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            double[][] dataToSplit,
            double[][] outputToSplit,
            out double[][] dataTraining,
            out double[][] outputsTraining,
            out double[][] dataCrossValidation,
            out double[][] outputsCrossValidation,
            out double[][] dataTesting,
            out double[][] outputsTesting,
            double[][] syntheticData = null,
            double[][] syntheticOutput = null
        )
        {
            var trainingLength = (int)(outputToSplit.Length * trainingDataSamplePercentage);
            var crossValidationLength = (int)(outputToSplit.Length * crossValidationDataSamplePercentage);
            var testingLength = (int)(outputToSplit.Length * testingDataSamplePercentage);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID " +
                    $"of {exhaustiveSearchInstanceTrialInstance.Id} is separating the dataset. " +
                    $"There are {trainingLength} training records, {crossValidationLength} cross validation records " +
                    $"and {testingLength} testing records.  Will split the dataset.");
            }

            var split = DataSplit.Split([.. outputToSplit.Select(o => o[0])],
                trainingDataSamplePercentage, crossValidationDataSamplePercentage, testingDataSamplePercentage,
                splitSeed);

            dataTraining = DataSplit.Take(dataToSplit, split.Train);
            outputsTraining = DataSplit.Take(outputToSplit, split.Train);

            dataCrossValidation = DataSplit.Take(dataToSplit, split.Validation);
            outputsCrossValidation = DataSplit.Take(outputToSplit, split.Validation);

            dataTesting = DataSplit.Take(dataToSplit, split.Test);
            outputsTesting = DataSplit.Take(outputToSplit, split.Test);

            if (syntheticData == null || syntheticData.Length == 0 || syntheticOutput == null)
            {
                return;
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: For trial instance Exhaustive Search Instance Trial Instance ID " +
                    $"of {exhaustiveSearchInstanceTrialInstance.Id} is appending {syntheticData.Length} " +
                    "bootstrapped synthetic rows to the training partition only.");
            }

            dataTraining = [.. dataTraining, .. syntheticData];
            outputsTraining = [.. outputsTraining, .. syntheticOutput];
        }
    }
}