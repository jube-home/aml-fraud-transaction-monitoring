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

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelBacktest(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "EntityAnalysisModelBacktestFields", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists the fields a backtest's filter and class may use, including Tag.<Name> for the class."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelBacktestRunRule", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Backtests a rule immediately over a filtered archive against a class; returns raw counts."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelBacktestSubmit", OperationKind.Write, Idempotent: false,
                Destructive: false,
                "Submits a large backtest to the engine's backtest threads; returns the run to poll."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelBacktestStatus", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Returns a submitted backtest's status, progress and headline counts."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelBacktestList", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists a model's submitted backtests, optionally for one rule."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelBacktestResult", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Returns a finished backtest's counts and examples."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelBacktestStop", OperationKind.Write, Idempotent: true,
                Destructive: false,
                "Stops a submitted backtest."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelBacktestExplain", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Explains why one archived transaction was or was not counted, with the values read.")
        ]);
    }
}