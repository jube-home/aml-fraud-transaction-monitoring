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
using Jube.Service.AskJoobyConversation;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddAskJoobyConversation(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "AskJoobyConversationList", OperationKind.Read, true, false,
                    "Lists AskJooby conversations for the caller's tenant, most recently active first.",
                    AskJoobyConversationService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "AskJoobyConversationCreate", OperationKind.Write, false, false,
                    "Starts a new, empty, untitled AskJooby conversation in the caller's tenant.",
                    AskJoobyConversationService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "AskJoobyConversationGetMessages", OperationKind.Read, true, false,
                    "Returns the full transcript of one conversation, oldest first.",
                    AskJoobyConversationService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "AskJoobyConversationSendMessage", OperationKind.Write, false, false,
                    "Sends a message to Jooby and streams the reply, persisting both sides of the exchange.",
                    AskJoobyConversationService.RequiredPermissions)
            ]);
        }
    }
}