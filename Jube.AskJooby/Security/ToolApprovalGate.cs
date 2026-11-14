/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.AskJooby.Security.Interfaces;
using Jube.AskJooby.Security.Models;

namespace Jube.AskJooby.Security
{
    public sealed class ToolApprovalGate(
        ToolApprovalLedger ledger,
        string conversationKey,
        string userName,
        int tenantRegistryId) : IToolApprovalGate
    {
        public ToolApprovalVerdict Review(string toolName, IReadOnlyDictionary<string, object?> arguments)
        {
            var argumentsJson = ToolApprovalLedger.Canonicalise(arguments);

            if (ledger.TryConsume(conversationKey, userName, tenantRegistryId, toolName, argumentsJson,
                    ToolApprovalState.Approved))
            {
                return ToolApprovalVerdict.Proceed;
            }

            if (ledger.TryConsume(conversationKey, userName, tenantRegistryId, toolName, argumentsJson,
                    ToolApprovalState.Rejected))
            {
                return ToolApprovalVerdict.Rejected;
            }

            ledger.Request(conversationKey, userName, tenantRegistryId, toolName, argumentsJson);

            return ToolApprovalVerdict.Pending;
        }
    }
}
