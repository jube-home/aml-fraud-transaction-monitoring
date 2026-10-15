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
using Accord.Neuro;
using Jube.Engine.Exhaustive.Algorithms.Models;

namespace Jube.Engine.Exhaustive.Activation
{
    internal static class ActivationAnnotations
    {
        internal static List<ActivationNetworkAnnotation> MapTrialVariableToActivationNetworkAnnotations(
            Dictionary<int, TrialVariable> trialVariables)
        {
            return
            [
                .. trialVariables.Select(trialVariable => new ActivationNetworkAnnotation
                {
                    Name = trialVariable.Value.Name,
                    ExhaustiveSearchInstanceVariableId = trialVariable.Value.ExhaustiveSearchInstanceVariableId,
                    Mean = trialVariable.Value.Mean,
                    Sd = trialVariable.Value.Sd,
                    Max = trialVariable.Value.Max,
                    Min = trialVariable.Value.Min,
                    Mode = trialVariable.Value.Mode,
                    ExhaustiveSearchInstanceTrialInstanceVariableId =
                        trialVariable.Value.ExhaustiveSearchInstanceTrialInstanceVariableId,
                    NormalisationTypeId = trialVariable.Value.NormalisationTypeId,
                    ProcessingTypeId = trialVariable.Value.ProcessingTypeId
                })
            ];
        }
    }
}