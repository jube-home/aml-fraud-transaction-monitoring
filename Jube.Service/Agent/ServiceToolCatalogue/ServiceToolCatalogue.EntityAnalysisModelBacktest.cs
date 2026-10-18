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
using Jube.Service.Query.EntityAnalysisModelBacktest;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelBacktest(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelBacktestFields", OperationKind.Read, true,
                    false,
                    "Lists the fields a backtest's filter and class may use, including Tag.<Name> for the class.",
                    EntityAnalysisModelBacktestService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelBacktestRunRule", OperationKind.Read, true,
                    false,
                    "Backtests a rule immediately over a filtered archive against a class; returns raw counts.",
                    EntityAnalysisModelBacktestService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelBacktestSubmit", OperationKind.Write, false,
                    false,
                    "Submits a large backtest to the engine's backtest threads; returns the run to poll.",
                    EntityAnalysisModelBacktestService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelBacktestStatus", OperationKind.Read, true,
                    false,
                    "Returns a submitted backtest's status, progress and headline counts.",
                    EntityAnalysisModelBacktestService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelBacktestList", OperationKind.Read, true,
                    false,
                    "Lists a model's submitted backtests, optionally for one rule.",
                    EntityAnalysisModelBacktestService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelBacktestResult", OperationKind.Read, true,
                    false,
                    "Returns a finished backtest's counts and examples.",
                    EntityAnalysisModelBacktestService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelBacktestStop", OperationKind.Write, true,
                    false,
                    "Stops a submitted backtest.",
                    EntityAnalysisModelBacktestService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelBacktestExplain", OperationKind.Read, true,
                    false,
                    "Explains why one archived transaction was or was not counted, with the values read.",
                    EntityAnalysisModelBacktestService.RequiredPermissions)
            ]);
        }
    }
}