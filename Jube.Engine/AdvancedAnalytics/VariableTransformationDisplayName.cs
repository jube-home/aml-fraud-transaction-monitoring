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
using Jube.Engine.AdvancedAnalytics.Models;

namespace Jube.Engine.AdvancedAnalytics
{
    public static class VariableTransformationDisplayName
    {
        private static readonly Dictionary<VariableTransformationKind, string> abbreviations = new()
        {
            [VariableTransformationKind.Log] = "Log",
            [VariableTransformationKind.Log10] = "Log10",
            [VariableTransformationKind.Log2] = "Log2",
            [VariableTransformationKind.Log1P] = "Log1P",
            [VariableTransformationKind.SignedLog1P] = "SignedLog1P",
            [VariableTransformationKind.Sqrt] = "Sqrt",
            [VariableTransformationKind.SignedSqrt] = "SignedSqrt",
            [VariableTransformationKind.CubeRoot] = "CubeRoot",
            [VariableTransformationKind.Reciprocal] = "Reciprocal",
            [VariableTransformationKind.Square] = "Square",
            [VariableTransformationKind.AbsoluteValue] = "Abs",
            [VariableTransformationKind.BoxCox] = "BoxCox",
            [VariableTransformationKind.YeoJohnson] = "YeoJohnson",
            [VariableTransformationKind.ZScoreStandard] = "ZScore",
            [VariableTransformationKind.MinMax] = "MinMax",
            [VariableTransformationKind.RobustMad] = "RobustMad",
            [VariableTransformationKind.RobustIqr] = "RobustIqr",
            [VariableTransformationKind.MaxAbs] = "MaxAbs",
            [VariableTransformationKind.QuantileUniform] = "QuantileUniform",
            [VariableTransformationKind.QuantileNormal] = "QuantileNormal",
            [VariableTransformationKind.Winsorise] = "Winsor",
            [VariableTransformationKind.DiscretiseEqualWidth] = "BinEW",
            [VariableTransformationKind.DiscretiseEqualFrequency] = "BinEF",
            [VariableTransformationKind.DiscretiseKMeans] = "BinKMeans",
            [VariableTransformationKind.DiscretiseSupervised] = "BinSupervised",
            [VariableTransformationKind.DiscretiseCustomEdges] = "BinCustom",
            [VariableTransformationKind.LabelOrdinal] = "Ordinal",
            [VariableTransformationKind.TargetMean] = "TargetMean",
            [VariableTransformationKind.WeightOfEvidence] = "WoE",
            [VariableTransformationKind.Frequency] = "Freq"
        };

        public static string DisplayName(string name, VariableTransformationKind kind)
        {
            switch (kind)
            {
                case VariableTransformationKind.None:
                case VariableTransformationKind.OneHotIndicator:
                    return name;
                case VariableTransformationKind.CyclicalSin:
                    return name + "Sin";
                case VariableTransformationKind.CyclicalCos:
                    return name + "Cos";
                default:
                    return abbreviations.TryGetValue(kind, out var abbreviation) ? name + abbreviation : name;
            }
        }
    }
}