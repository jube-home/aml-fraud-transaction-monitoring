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
using Jube.Service.Query.CaseSuppressionCandidates;
using Jube.Service.Repository.CaseSuppression;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseSuppression(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseSuppressionListByCase", OperationKind.Read, true, false,
                    "Lists the suppression requests made from one case, newest first, with their targets, evidence " +
                    "transactions, decisions, the counts when requested and what each is now: pending, approved, " +
                    "rejected, retracted, expired or removed.",
                    CaseSuppressionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSuppressionRequest", OperationKind.Write, false, false,
                    "Requests that an override key and value be suppressed or forced on models and activation " +
                    "rules, from a case. Nothing changes until different people with the approval permission " +
                    "approve it. The value must be one the case journal carries, proved by archive ids from " +
                    "CaseSuppressionCandidateTransactions; a reason and a future expiry are required. Flagged as " +
                    "written by AskJooby.",
                    CaseSuppressionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSuppressionCandidateValues", OperationKind.Read, true, false,
                    "Lists the distinct values of one override key across the journal of a case, with how many " +
                    "transactions on the case carry each and, where indexed, how widely each is carried.",
                    CaseSuppressionCandidatesService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSuppressionCandidateTransactions", OperationKind.Read, true, false,
                    "Lists the transactions in the journal of a case that carry one value of an override key, with " +
                    "the archive ids to send back as evidence.",
                    CaseSuppressionCandidatesService.RequiredPermissions)
            ]);
        }
    }
}
