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
using System.Linq;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Regression;

namespace Jube.Engine.AdvancedAnalytics.Training
{
    internal static class AdvancedAnalyticsVariableRowsJson
    {
        internal static string Build(
            string[] variablesInOrder,
            string[] names,
            int[] types,
            VariableTransformationKind[] transformationIds,
            string[] transformationParametersJson)
        {
            var rows = variablesInOrder.Select(name =>
            {
                var index = Array.IndexOf(names, name);

                if (index < 0)
                {
                    throw new InvalidOperationException("The snapshot names a variable that was not supplied.");
                }

                return new AdvancedAnalyticsVariableRow
                {
                    Name = name,
                    ProcessingTypeId = types[index],
                    TransformationId = transformationIds != null && index < transformationIds.Length
                        ? transformationIds[index]
                        : VariableTransformationKind.None,
                    TransformationParametersJson =
                        transformationParametersJson != null && index < transformationParametersJson.Length
                            ? transformationParametersJson[index]
                            : null
                };
            }).ToArray();

            return AdvancedAnalyticsEvidenceJson.Serialise(rows);
        }
    }
}