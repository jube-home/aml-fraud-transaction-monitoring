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
using Jube.AskJooby.Agents.Models;

namespace Jube.AskJooby.Agents
{
    public sealed class ReplyDraft
    {
        private const int SubstantialDraftCharacters = 400;
        private const int DiscardedToKeptRatio = 3;
        private readonly StringBuilder builder = new();
        private string setAside = string.Empty;
        public int Length => ToString().Length;

        public void Append(string? text)
        {
            builder.Append(text);
        }

        public void Append(char value)
        {
            builder.Append(value);
        }

        public void Observe(AgentStreamItem item)
        {
            if (item.AgentLabel is not null || item.AnswerPreserving)
            {
                return;
            }

            if (item.Kind is not (AgentStreamItemKind.Step or AgentStreamItemKind.Delegation))
            {
                return;
            }

            var discarded = builder.ToString().Trim();

            if (discarded.Length > setAside.Length)
            {
                setAside = discarded;
            }

            builder.Clear();
        }

        public override string ToString()
        {
            var written = builder.ToString().Trim();

            if (setAside.Length == 0)
            {
                return written;
            }

            if (written.Length == 0)
            {
                return setAside;
            }

            return setAside.Length >= SubstantialDraftCharacters
                   && setAside.Length >= written.Length * DiscardedToKeptRatio
                ? setAside + "\n\n" + written
                : written;
        }
    }
}