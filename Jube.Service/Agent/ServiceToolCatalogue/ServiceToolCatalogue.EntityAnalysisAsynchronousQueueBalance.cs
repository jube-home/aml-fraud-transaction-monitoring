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
        static partial void AddEntityAnalysisAsynchronousQueueBalance(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "EntityAnalysisAsynchronousQueueBalanceList", OperationKind.Read, true, false,
                    "Lists platform-wide asynchronous processing queue balance rows, most recent first, " +
                    "capped at 100000, with optional date-range filters. Each row is a roughly one-minute " +
                    "interval's queue depth for asynchronous CaseCreation, Tagging and Notification " +
                    "dispatch. Not scoped to any tenant. Also accepts samplePercentage (0-100) to draw an " +
                    "unbiased random subset of matching rows instead of the most recent ones."));
        }
    }
}