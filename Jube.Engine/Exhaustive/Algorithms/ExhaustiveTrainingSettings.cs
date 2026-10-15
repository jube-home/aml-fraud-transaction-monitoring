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

namespace Jube.Engine.Exhaustive.Algorithms
{
    public class ExhaustiveTrainingSettings
    {
        public int TrialsLimit { get; init; }
        public int MinVariableCount { get; init; }
        public int MaxVariableCount { get; init; }
        public double TrainingDataSamplePercentage { get; init; }
        public double CrossValidationDataSamplePercentage { get; init; }
        public double TestingDataSamplePercentage { get; init; }
        public double ValidationTestingActivationThreshold { get; init; }
        public int TopologySinceImprovementLimit { get; init; }
        public int LayerDepthLimit { get; init; }
        public int LayerWidthLimitInputLayerFactor { get; init; }
        public int TopologyComplexityLimit { get; init; }
        public int ActivationFunctionExplorationEpochs { get; init; }
        public int TopologyExplorationEpochs { get; init; }
        public int TopologyFinalisationEpochs { get; init; }
        public int SimulationsCount { get; init; }
        public double ChallengerModelBaseProbability { get; init; }

        public static ExhaustiveTrainingSettings FromEnvironment(DynamicEnvironment.DynamicEnvironment environment)
        {
            return new ExhaustiveTrainingSettings
            {
                TrialsLimit = int.Parse(environment.AppSettings("ExhaustiveTrialsLimit")),
                MinVariableCount = int.Parse(environment.AppSettings("ExhaustiveMinVariableCount")),
                MaxVariableCount = int.Parse(environment.AppSettings("ExhaustiveMaxVariableCount")),
                TrainingDataSamplePercentage =
                    double.Parse(environment.AppSettings("ExhaustiveTrainingDataSamplePercentage")),
                CrossValidationDataSamplePercentage =
                    double.Parse(environment.AppSettings("ExhaustiveCrossValidationDataSamplePercentage")),
                TestingDataSamplePercentage =
                    double.Parse(environment.AppSettings("ExhaustiveTestingDataSamplePercentage")),
                ValidationTestingActivationThreshold =
                    double.Parse(environment.AppSettings("ExhaustiveValidationTestingActivationThreshold")),
                TopologySinceImprovementLimit =
                    int.Parse(environment.AppSettings("ExhaustiveTopologySinceImprovementLimit")),
                LayerDepthLimit = int.Parse(environment.AppSettings("ExhaustiveLayerDepthLimit")),
                LayerWidthLimitInputLayerFactor =
                    int.Parse(environment.AppSettings("ExhaustiveLayerWidthLimitInputLayerFactor")),
                TopologyComplexityLimit = int.Parse(environment.AppSettings("ExhaustiveTopologyComplexityLimit")),
                ActivationFunctionExplorationEpochs =
                    int.Parse(environment.AppSettings("ExhaustiveActivationFunctionExplorationEpochs")),
                TopologyExplorationEpochs =
                    int.Parse(environment.AppSettings("ExhaustiveTopologyExplorationEpochs")),
                TopologyFinalisationEpochs =
                    int.Parse(environment.AppSettings("ExhaustiveTopologyFinalisationEpochs")),
                SimulationsCount = int.Parse(environment.AppSettings("ExhaustiveSimulationsCount")),
                ChallengerModelBaseProbability =
                    double.Parse(environment.AppSettings("ExhaustiveChallengerModelBaseProbability"))
            };
        }
    }
}