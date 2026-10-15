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
using System.Linq;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.HttpAdaptationProtocol;
using Jube.HttpAdaptationProtocol.Calibration;
using Jube.HttpAdaptationProtocol.Contribution;
using Jube.HttpAdaptationProtocol.Models;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public sealed class MultipleLinearRegressionExplain : IAdvancedAnalyticsExplanation
    {
        public required double PredictedValue { get; init; }
        public required double Intercept { get; init; }
        public required IReadOnlyList<LinearRegressionContribution> Contributions { get; init; }

        public Adaptation ToAdaptation(double value, string modelName, IReadOnlyList<int> processingTypeIds = null)
        {
            var items = Contributions.Select((c, i) => new ContributionItem
            {
                Name = c.Name,
                Weight = c.Weight,
                Source = processingTypeIds != null && i < processingTypeIds.Count
                    ? AdaptationInputExtraction.ProtocolSource(processingTypeIds[i])
                    : null
            }).ToList();

            return new Adaptation
            {
                Value = value,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = modelName,
                    Family = ProtocolConstants.Family.Glm,
                    FeatureCount = Contributions.Count
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.Score,
                    Calibrated = false,
                    Method = ProtocolConstants.CalibrationMethod.None
                },
                Contribution = new ContributionSet
                {
                    Space = ProtocolConstants.ValueSpace.Score,
                    Method = ProtocolConstants.ContributionMethod.Coefficient,
                    Exact = true,
                    BaseValue = Intercept,
                    Items = items
                }
            };
        }
    }
}