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

using System.Text;

namespace Jube.Service.AskJoobyConversation.Models
{
    public sealed class ThinkingPassBuffer
    {
        private const string Unlabelled = "";
        private readonly Dictionary<string, StringBuilder> passes = new(StringComparer.Ordinal);

        public void Append(string? agentLabel, string text)
        {
            var key = agentLabel ?? Unlabelled;

            if (!passes.TryGetValue(key, out var builder))
            {
                builder = new StringBuilder();
                passes[key] = builder;
            }

            builder.Append(text);
        }

        public IEnumerable<string> CompletedPasses()
        {
            foreach (var pass in passes)
            {
                if (pass.Value.Length == 0)
                {
                    continue;
                }

                yield return pass.Key.Length == 0
                    ? pass.Value.ToString()
                    : $"[{pass.Key}] {pass.Value}";
            }
        }
    }
}