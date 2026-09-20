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
        static partial void AddEntityAnalysisModelProcessingCounter(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "EntityAnalysisModelProcessingCounterList", OperationKind.Read, true, false,
                    "Lists per-Model processing counter rows, most recent first, capped at 100000, with " +
                    "optional date-range and Model-name substring filters. Each row is a roughly " +
                    "one-minute interval's counts for one Entity Analysis Model -- synchronous invocations, " +
                    "Gateway Rule matches, response-elevation triggers, activation-watcher triggers, " +
                    "response times, and pending archive WAL entries. Landlord only: every other caller is refused with a 403. Also accepts samplePercentage (0-100) to draw an " +
                    "unbiased random subset of matching rows instead of the most recent ones."));
        }
    }
}