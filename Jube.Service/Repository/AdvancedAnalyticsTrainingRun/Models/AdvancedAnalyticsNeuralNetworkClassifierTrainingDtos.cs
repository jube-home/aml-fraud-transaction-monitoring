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
using Jube.Engine.AdvancedAnalytics.Training;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models
{
    public sealed class AdvancedAnalyticsNeuralNetworkClassifierTrainingDto
    {
        [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
        public int AdvancedAnalyticsInstanceId { get; set; }

        [Description("Name of the model the trained version is filed under. A model of that name in the " +
                     "instance receives the result as a new version; otherwise a new model is created.")]
        public string? ModelName { get; set; }

        [Description("Optional opaque label kept with a newly created model, for the caller to find it again.")]
        public string? CorrelationKey { get; set; }

        [Description("Where the training rows come from. Leave empty to train on the Inputs and Labels given here " +
                     "(inline). Set the kind to CachePointer with a cache pointer to train on rows held in the " +
                     "Advanced Analytics cache, whose last column is the label unless a label column is named. " +
                     "The Dataset kind is reserved and is not supported yet.")]
        public AdvancedAnalyticsTrainingInput? Input { get; set; }

        [Description("Predictor rows: one array of numbers per case, all the same length as the variable names. " +
                     "NaN marks a missing value. Used when the input is inline.")]
        public double[][]? Inputs { get; set; }

        [Description("The binary outcome for each row (0 or 1), in the same order as the rows. Both classes must " +
                     "be present. Used when the input is inline.")]
        public int[]? Labels { get; set; }

        [Description("Name of each variable, unique and in column order.")]
        public string[]? VariableNames { get; set; }

        [Description("Where each variable comes from in the live pipeline, one number per variable name in the " +
                     "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                     "calculation, 7 or 99 other. The live pipeline uses it to find each value when it scores.")]
        public int[]? ProcessingTypeIds { get; set; }

        [Description("Number of units in the single hidden layer, from 1 up to 128. Default 8.")]
        public int HiddenUnits { get; set; } = 8;

        [Description("Number of full passes the gradient descent fit makes over the training split, from 1 up to " +
                     "5000. Default 200.")]
        public int Epochs { get; set; } = 200;

        [Description("Step size the gradient descent update takes at every epoch. Default 0.1.")]
        public double LearningRate { get; set; } = 0.1;

        [Description("Whether variables are z-scored with training statistics before fitting. Default true.")]
        public bool Standardise { get; set; } = true;

        [Description("Whether the network's raw output is Platt-calibrated to a probability on the validation " +
                     "split. Default true.")]
        public bool Calibrate { get; set; } = true;

        [Description("Share of the rows held back for a validation split used to fit the calibrator, from 0 up " +
                     "to but not including 1.")]
        public double ValidationFraction { get; set; } = 0.2;

        [Description("Share of the rows held back purely to measure held-out quality, from 0 up to but not " +
                     "including 1. Together with the validation share it may not exceed 0.9.")]
        public double TestFraction { get; set; } = 0.2;

        [Description("Seed that makes the row split and the initial random weights reproducible.")]
        public int Seed { get; set; } = 1;
    }
}