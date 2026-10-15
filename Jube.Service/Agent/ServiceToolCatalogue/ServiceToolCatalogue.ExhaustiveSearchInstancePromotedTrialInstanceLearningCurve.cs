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

using Jube.Service.Agent.Models;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddExhaustiveSearchInstancePromotedTrialInstanceLearningCurve(
            List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveGet", OperationKind.Read, true,
                    false,
                    "Returns the score-over-time learning curve of an exhaustive search instance's promoted trial instances.",
                    ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveService.RequiredPermissions)
            ]);
        }
    }
}