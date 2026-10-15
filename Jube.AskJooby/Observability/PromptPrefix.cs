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

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Observability
{
    public static class PromptPrefix
    {
        public static string? Hash(IEnumerable<ChatMessage>? messages)
        {
            var materialized = messages?.ToList();

            var first = materialized?.FirstOrDefault(message => message.Role == ChatRole.System)
                        ?? materialized?.FirstOrDefault();

            var text = first?.Text;

            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..32]
                .ToLowerInvariant();
        }
    }
}