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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    public static class ModelEvaluationLimits
    {
        public const int MaximumRows = 20000;
        public const int MaximumFeatures = 200;
        public const int MaximumTrees = 500;
        public const int MaximumTreeDepth = 24;
        public const int MaximumEpochs = 1000;
        public const int MaximumHiddenLayers = 3;
        public const int MaximumHiddenUnits = 64;
        public const int MaximumFolds = 10;
        public const int MaximumGridSize = 64;
        public const int MaximumSearchRows = 10000;
        public const int MaximumKernelRows = 4000;
        public const int MaximumNeighbourRows = 20000;
        public const int MaximumSweepValues = 16;
        public const int MaximumTotalFits = 1500;
        public const int MaximumClasses = 20;
        public const int MaximumBins = 32;
        public const int MaximumClusters = 32;
        public const int MaximumPartialDependencePoints = 50;
        public const int MaximumImportanceRows = 2000;
        public const int MaximumPermutationRepeats = 10;
        public const int MaximumComparedFamilies = 12;
        public const int MaximumReturnedRows = 50;
        public const int MinimumRows = 20;
        public const int MinimumTestRows = 100;
        public const int MinimumTestClassRows = 10;
        public const double SevereImbalanceRatio = 100d;
        public const double OverfitGap = 0.1;
        public const double SuspiciousAuc = 0.999;
    }
}