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

using Jube.Data.Poco;

namespace Jube.Service.AskJoobyConversation.Models
{
    public static class ConversationWindow
    {
        public static List<AskJoobyMessage> Select(IReadOnlyList<AskJoobyMessage> priorMessages)
        {
            var lastCompact = priorMessages.LastOrDefault(message => message.IsCompact == true);

            if (lastCompact is null)
            {
                return priorMessages.ToList();
            }

            var anchor = lastCompact.CompactedToAskJoobyMessageId ?? lastCompact.Id;

            var window = new List<AskJoobyMessage> { lastCompact };

            window.AddRange(priorMessages
                .Where(message => message.Id > anchor
                                  && message.Id != lastCompact.Id
                                  && message.IsCompact != true)
                .OrderBy(message => message.Id));

            return window;
        }
    }
}