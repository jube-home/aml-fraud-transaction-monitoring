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
using Jube.Engine.AdvancedAnalytics.Training.Models;

namespace Jube.Engine.AdvancedAnalytics.Training
{
    public sealed class AdvancedAnalyticsTrainingInput
    {
        public AdvancedAnalyticsTrainingInputKind Kind { get; set; } = AdvancedAnalyticsTrainingInputKind.Inline;
        public double[][] Inputs { get; set; }
        public int[] Labels { get; set; }
        public Guid? Pointer { get; set; }
        public int LabelColumn { get; set; } = -1;
        public string Reference { get; set; }

        public static AdvancedAnalyticsTrainingInput Inline(double[][] inputs, int[] labels)
        {
            return new AdvancedAnalyticsTrainingInput
            {
                Kind = AdvancedAnalyticsTrainingInputKind.Inline,
                Inputs = inputs,
                Labels = labels
            };
        }

        public static AdvancedAnalyticsTrainingInput CachePointer(Guid pointer, int labelColumn = -1)
        {
            return new AdvancedAnalyticsTrainingInput
            {
                Kind = AdvancedAnalyticsTrainingInputKind.CachePointer,
                Pointer = pointer,
                LabelColumn = labelColumn
            };
        }

        public static AdvancedAnalyticsTrainingInput Dataset(string reference)
        {
            return new AdvancedAnalyticsTrainingInput
            {
                Kind = AdvancedAnalyticsTrainingInputKind.Dataset,
                Reference = reference
            };
        }

        public AdvancedAnalyticsTrainingInput WithoutData()
        {
            return new AdvancedAnalyticsTrainingInput
            {
                Kind = Kind,
                Pointer = Pointer,
                LabelColumn = LabelColumn,
                Reference = Reference
            };
        }
    }
}