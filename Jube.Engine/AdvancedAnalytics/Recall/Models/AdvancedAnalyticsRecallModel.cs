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
using System.Collections.Generic;
using Jube.Engine.AdvancedAnalytics.Interfaces;

namespace Jube.Engine.AdvancedAnalytics.Recall.Models
{
    public sealed class AdvancedAnalyticsRecallModel
    {
        public AdvancedAnalyticsRecallModel(string kind, IReadOnlyList<string> variables,
            Func<double[], double[]> predict, Func<double[], IAdvancedAnalyticsExplanation> explain = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(kind);
            ArgumentNullException.ThrowIfNull(variables);
            ArgumentNullException.ThrowIfNull(predict);

            Kind = kind;
            Variables = variables;
            Predict = predict;
            Explain = explain;
        }

        public string Kind { get; }
        public IReadOnlyList<string> Variables { get; }
        public Func<double[], double[]> Predict { get; }

        public Func<double[], IAdvancedAnalyticsExplanation> Explain { get; }
    }
}