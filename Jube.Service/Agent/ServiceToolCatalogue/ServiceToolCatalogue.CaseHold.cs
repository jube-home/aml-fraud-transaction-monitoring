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
using Jube.Service.Query.CaseHold;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseHold(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseHoldList", OperationKind.Read, true, false,
                    "Lists the payments held, declined, released or refused on the transactions of a case, with " +
                    "the response elevation range that stamped each, the deadline of the payment hold diary, who " +
                    "decided and why, the messages sent and the release hook that would release it.",
                    CaseHoldService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseHoldRelease", OperationKind.Write, false, false,
                    "Releases a held payment by calling the case workflow release hook that handles it; recorded " +
                    "as Released only when the hook answers exactly 200. Needs a reason and the release permission. " +
                    "Moves money in the payment system.",
                    CaseHoldService.RequiredDecisionPermissions),
                new ServiceToolDescriptor(
                    "CaseHoldRefuse", OperationKind.Write, false, false,
                    "Refuses a held payment, recording who and why, and calling the refuse URL of the release hook " +
                    "when it has one. Needs a reason and the release permission.",
                    CaseHoldService.RequiredDecisionPermissions)
            ]);
        }
    }
}
