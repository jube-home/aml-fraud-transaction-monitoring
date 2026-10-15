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

using OpenAI.Chat;

namespace Jube.AskJooby.Reasoning
{
    public static class ThinkingOptions
    {
        private static readonly string[] efforts = ["low", "high", "xhigh", "max"];

        public static ChatCompletionOptions DisabledOptions()
        {
            var raw = new ChatCompletionOptions();
#pragma warning disable SCME0001
            raw.Patch.Set("$.thinking.type"u8, "disabled");
#pragma warning restore SCME0001

            return raw;
        }

        public static ChatCompletionOptions RawOptions(int? effort = null)
        {
            var resolvedEffort = efforts[effort is >= 0 and < 4 ? effort.Value : 1];

            var raw = new ChatCompletionOptions();
#pragma warning disable SCME0001
            raw.Patch.Set("$.reasoning_effort"u8, resolvedEffort);
            raw.Patch.Set("$.thinking.type"u8, "enabled");
#pragma warning restore SCME0001

            return raw;
        }
    }
}