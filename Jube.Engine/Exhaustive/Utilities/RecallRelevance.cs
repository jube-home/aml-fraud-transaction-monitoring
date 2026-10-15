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

using System.Collections.Generic;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.Exhaustive.Calibration;
using Jube.HttpAdaptationProtocol;
using Jube.HttpAdaptationProtocol.Calibration;
using Jube.HttpAdaptationProtocol.Contribution;
using Jube.HttpAdaptationProtocol.Models;

namespace Jube.Engine.Exhaustive.Utilities
{
    public sealed class RecallRelevance
    {
        public IReadOnlyList<VariableRelevance> Variables { get; init; }
        public double BaseValue { get; init; }
        public IReadOnlyList<IReadOnlyList<double>> NodeRelevance { get; init; }
        public IReadOnlyList<IReadOnlyList<IReadOnlyList<double>>> EdgeRelevance { get; init; }

        public Adaptation ToAdaptation(double value, string modelName, int hiddenLayers, int processingElements,
            ExhaustiveCalibration calibration, IReadOnlyList<int> processingTypeIds = null)
        {
            var items = new List<ContributionItem>(Variables.Count);

            for (var i = 0; i < Variables.Count; i++)
            {
                var variable = Variables[i];

                items.Add(new ContributionItem
                {
                    Name = variable.Name,
                    Weight = variable.LrpRelevance,
                    Source = processingTypeIds != null && i < processingTypeIds.Count
                        ? AdaptationInputExtraction.ProtocolSource(processingTypeIds[i])
                        : null
                });
            }

            return new Adaptation
            {
                Value = value,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = modelName,
                    Family = ProtocolConstants.Family.NeuralNetwork,
                    FeatureCount = Variables.Count,
                    HiddenLayers = hiddenLayers,
                    ProcessingElements = processingElements
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = calibration.IsCalibrated
                        ? ProtocolConstants.ValueSpace.Probability
                        : ProtocolConstants.ValueSpace.Score,
                    Calibrated = calibration.IsCalibrated,
                    Method = calibration.IsCalibrated
                        ? ProtocolConstants.CalibrationMethod.Platt
                        : ProtocolConstants.CalibrationMethod.None,
                    Intercept = NullableIfFinite(calibration.Intercept),
                    Slope = NullableIfFinite(calibration.Slope)
                },
                Contribution = new ContributionSet
                {
                    Space = ProtocolConstants.ValueSpace.Score,
                    Method = ProtocolConstants.ContributionMethod.LayerwiseRelevance,
                    Exact = true,
                    BaseValue = BaseValue,
                    Items = items
                }
            };
        }

        private static double? NullableIfFinite(double value)
        {
            return double.IsFinite(value) ? value : null;
        }
    }
}