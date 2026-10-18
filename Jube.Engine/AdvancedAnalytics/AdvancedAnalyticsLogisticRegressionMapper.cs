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
using Accord.Statistics.Models.Regression;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Models;

namespace Jube.Engine.AdvancedAnalytics
{
    public sealed class AdvancedAnalyticsLogisticRegressionMapper :
        IAdvancedAnalyticsSnapshotMapper<LogisticRegression, AdvancedAnalyticsLogisticRegressionSnapshot>
    {
        public const string ModelKind = "LogisticRegression";
        public string Kind => ModelKind;
        public int SchemaVersion => 1;

        public AdvancedAnalyticsLogisticRegressionSnapshot ToSnapshot(LogisticRegression model)
        {
            ArgumentNullException.ThrowIfNull(model);

            return new AdvancedAnalyticsLogisticRegressionSnapshot
            {
                Weights = (double[])model.Weights.Clone(),
                Intercept = model.Intercept,
                StandardErrors = model.StandardErrors != null && model.StandardErrors.Length == model.Weights.Length + 1
                    ? (double[])model.StandardErrors.Clone()
                    : null
            };
        }

        public LogisticRegression FromSnapshot(AdvancedAnalyticsLogisticRegressionSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            snapshot.Validate(snapshot.Weights.Length);

            var model = LogisticRegression.FromWeights((double[])snapshot.Weights.Clone(), snapshot.Intercept);

            if (snapshot.StandardErrors != null)
            {
                model.StandardErrors = (double[])snapshot.StandardErrors.Clone();
            }

            return model;
        }
    }
}