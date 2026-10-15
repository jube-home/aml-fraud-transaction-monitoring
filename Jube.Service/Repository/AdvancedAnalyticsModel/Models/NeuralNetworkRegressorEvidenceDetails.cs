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

using System.ComponentModel;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsModel.Models
{
    public sealed class NeuralNetworkRegressorEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Number of units in each hidden layer, in order from the input side.")]
        public int[] HiddenLayers { get; set; } = [];

        [Description("Number of full passes over the training split the fit ran for.")]
        public int EpochsRun { get; set; }

        [Description("Learning rate the gradient descent update used.")]
        public double LearningRate { get; set; }

        [Description("Mean squared error, on the rescaled 0.05 to 0.95 target, on the training split at a few " +
                     "checkpoint epochs; a falling trace is the fit converging, a flat or rising trace is not.")]
        public NeuralNetworkRegressorLossCheckpoint[] TrainingLossTrace { get; set; } = [];

        [Description("Mean squared error, on the rescaled target, on the training split after the last epoch.")]
        public double FinalTrainingLoss { get; set; }

        [Description("Whether every weight stayed finite throughout training.")]
        public bool Converged { get; set; }

        [Description("The span (maximum minus minimum) of the outcome in the training split that the network's " +
                     "bounded output is rescaled against; a span at or below zero means the training outcome was " +
                     "constant and the network was fitted to reproduce that constant only.")]
        public double TargetSpan { get; set; }

        [Description("Metrics on the training split.")]
        public NeuralNetworkRegressorSplitMetrics? TrainMetrics { get; set; }

        [Description("Metrics on the validation split, or null when it has no rows.")]
        public NeuralNetworkRegressorSplitMetrics? ValidationMetrics { get; set; }

        [Description("Metrics on the held-out test split, or null when it has no rows.")]
        public NeuralNetworkRegressorSplitMetrics? TestMetrics { get; set; }
    }
}