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
using Jube.Engine.AdvancedAnalytics.Classification.Models;
using Jube.Engine.AdvancedAnalytics.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    public sealed class NeuralNetworkEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Number of units in the single hidden layer.")]
        public int HiddenUnits { get; set; }

        [Description("Number of full passes over the training split the fit ran for.")]
        public int Epochs { get; set; }

        [Description("Learning rate the gradient descent update used.")]
        public double LearningRate { get; set; }

        [Description("Binary cross-entropy loss on the training split after the last epoch.")]
        public double FinalTrainingLoss { get; set; }

        [Description("Whether every weight stayed finite throughout training.")]
        public bool Converged { get; set; }

        [Description("Metrics on the training split.")]
        public NeuralNetworkSplitMetrics? TrainMetrics { get; set; }

        [Description("Metrics on the validation split, or null when it has no rows.")]
        public NeuralNetworkSplitMetrics? ValidationMetrics { get; set; }

        [Description("Metrics on the held-out test split, or null when it has no rows.")]
        public NeuralNetworkSplitMetrics? TestMetrics { get; set; }

        [Description("Whether and how the raw network output was Platt-calibrated on the validation split.")]
        public NeuralNetworkCalibrationInfo Calibration { get; set; } = null!;
    }
}