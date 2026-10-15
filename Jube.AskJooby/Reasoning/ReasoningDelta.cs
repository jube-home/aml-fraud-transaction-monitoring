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

using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace Jube.AskJooby.Reasoning
{
    using StreamingChatCompletionUpdate = StreamingChatCompletionUpdate;

    public static class ReasoningDelta
    {
        public static bool TryRead(ChatResponseUpdate? update, out string reasoning)
        {
            reasoning = string.Empty;

            if (update?.RawRepresentation is not StreamingChatCompletionUpdate raw)
            {
                return false;
            }

#pragma warning disable SCME0001

            if (!raw.Patch.TryGetValue("$.choices[0].delta.reasoning_content"u8, out string? value)
                || string.IsNullOrEmpty(value))
            {
                return false;
            }
#pragma warning restore SCME0001

            reasoning = value;

            return true;
        }
    }
}