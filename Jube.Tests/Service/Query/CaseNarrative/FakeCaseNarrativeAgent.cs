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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jube.Service.Query.CaseNarrative.Interfaces;
using Jube.Service.Query.CaseNarrative.Models;

namespace Jube.Test.Service.Query.CaseNarrative
{
    public sealed class FakeCaseNarrativeAgent : ICaseNarrativeAgent
    {
        public List<(string User, string Prompt, string PageContext)> Calls { get; } = [];
        public string Reply { get; set; } = "Plain narrative.";
        public bool Silent { get; set; }
        public int? ConversationId { get; set; } = 77;

        public Task<CaseNarrativeAnswer?> AskAsync(string userName, string prompt, string pageContext,
            CancellationToken token = default)
        {
            lock (Calls)
            {
                Calls.Add((userName, prompt, pageContext));
            }

            return Task.FromResult<CaseNarrativeAnswer?>(Silent ? null : new CaseNarrativeAnswer(Reply, ConversationId));
        }
    }
}
