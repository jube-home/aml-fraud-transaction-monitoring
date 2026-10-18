/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.AskJooby.Agents;
using Jube.AskJooby.Agents.Models;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    internal static class AdvancedAnalyticsGroupIcons
    {
        internal static void Register(IEnumerable<AdvancedAnalyticsToolDescriptor> descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                if (!string.IsNullOrWhiteSpace(descriptor.ToolName))
                {
                    AgentToolIcons.Register(descriptor.ToolName, For(descriptor.GroupName));
                }
            }
        }

        internal static AgentToolIcon For(string groupName)
        {
            return groupName switch
            {
                "AdaBoostClassifierTrainingRuns" => AgentToolIcon.Zap,
                "AdvancedAnalyticsTrainingRuns" => AgentToolIcon.Sparkles,
                "BaggingClassifierTrainingRuns" => AgentToolIcon.Layers,
                "BalancedKMeansClustererTrainingRuns" => AgentToolIcon.Scale,
                "BinarySplitClustererTrainingRuns" => AgentToolIcon.Split,
                "CategoricalAndSurvival" => AgentToolIcon.Hourglass,
                "DataPreparationAndResampling" => AgentToolIcon.Shuffle,
                "DecisionTreeClassifierTrainingRuns" => AgentToolIcon.GitBranch,
                "DecisionTreeRegressorTrainingRuns" => AgentToolIcon.Sprout,
                "DescriptiveStatistics" => AgentToolIcon.Sigma,
                "DistributionsAndHistograms" => AgentToolIcon.BarChart3,
                "GaussianMixtureClustererTrainingRuns" => AgentToolIcon.Blend,
                "GaussianMixtureDensityTrainingRuns" => AgentToolIcon.Waves,
                "GradientBoostingClassifierTrainingRuns" => AgentToolIcon.TrendingUp,
                "GradientBoostingRegressorTrainingRuns" => AgentToolIcon.ChartSpline,
                "HypothesisTests" => AgentToolIcon.TestTube,
                "IsolationForestAnomalyTrainingRuns" => AgentToolIcon.ShieldAlert,
                "KernelRidgeRegressorTrainingRuns" => AgentToolIcon.Waypoints,
                "KernelSupportVectorClassifierTrainingRuns" => AgentToolIcon.Slice,
                "KMeansClustererTrainingRuns" => AgentToolIcon.Group,
                "KMedoidsClustererTrainingRuns" => AgentToolIcon.Crosshair,
                "KModesClustererTrainingRuns" => AgentToolIcon.Boxes,
                "KNearestNeighboursClassifierTrainingRuns" => AgentToolIcon.Network,
                "KNearestNeighboursRegressorTrainingRuns" => AgentToolIcon.ScatterChart,
                "LinearRegressionTrainingRuns" => AgentToolIcon.Ruler,
                "LinearSupportVectorClassifierTrainingRuns" => AgentToolIcon.Rows3,
                "LogisticRegression" => AgentToolIcon.Percent,
                "LogisticRegressionTrainingRuns" => AgentToolIcon.ToggleRight,
                "MahalanobisAnomalyTrainingRuns" => AgentToolIcon.Radar,
                "MeanShiftClustererTrainingRuns" => AgentToolIcon.Orbit,
                "ModelEvaluationTools" => AgentToolIcon.ClipboardCheck,
                "ModelSelection" => AgentToolIcon.ClipboardCheck,
                "MiniBatchKMeansClustererTrainingRuns" => AgentToolIcon.Package,
                "MultivariateAndClustering" => AgentToolIcon.Combine,
                "NaiveBayesClassifierTrainingRuns" => AgentToolIcon.Brain,
                "NeuralNetworkClassifierTrainingRuns" => AgentToolIcon.Cpu,
                "NeuralNetworkRegressorTrainingRuns" => AgentToolIcon.BrainCircuit,
                "NumericsAndOptimisation" => AgentToolIcon.Calculator,
                "OneClassSupportVectorAnomalyTrainingRuns" => AgentToolIcon.ScanEye,
                "PrincipalComponentTrainingRuns" => AgentToolIcon.Rotate3D,
                "RandomForestClassifierTrainingRuns" => AgentToolIcon.TreeDeciduous,
                "RandomForestRegressorTrainingRuns" => AgentToolIcon.Trees,
                "RelationshipsAndRegression" => AgentToolIcon.Link,
                "RScript" => AgentToolIcon.FileCode,
                "TimeSeriesAndSignal" => AgentToolIcon.AudioWaveform,
                "TreeAugmentedNaiveBayesClassifierTrainingRuns" => AgentToolIcon.GitFork,
                _ => AgentToolIcons.Default
            };
        }
    }
}