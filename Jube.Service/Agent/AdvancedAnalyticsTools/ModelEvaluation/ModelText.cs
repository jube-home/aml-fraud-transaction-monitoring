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

using System.Globalization;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class ModelText
    {
        public static EvaluationImportanceRow[] Rank(string[] names, double[] values, double[]? deviations = null)
        {
            var order = Enumerable.Range(0, names.Length).OrderByDescending(i => values[i])
                .ThenBy(i => i).ToArray();

            var rows = new EvaluationImportanceRow[names.Length];

            for (var rank = 0; rank < order.Length; rank++)
            {
                var i = order[rank];
                rows[rank] = new EvaluationImportanceRow(names[i], values[i], deviations?[i], rank + 1);
            }

            return rows;
        }

        public static EvaluationNumber Fact(string name, double value)
        {
            return new EvaluationNumber(name, double.IsFinite(value) ? value : null);
        }

        public static string Probability(double value)
        {
            return "p(positive)=" + value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}