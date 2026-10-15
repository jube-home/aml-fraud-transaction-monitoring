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

using System.Globalization;
using System.Text.Json;
using System.Threading.Channels;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.Observability.Interfaces;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Observability
{
    public sealed class SnapshotOnWriteAiFunction(
        AIFunction inner,
        IAutoSnapshot autoSnapshot,
        ChannelWriter<AgentStreamItem>? channel = null,
        string? agentLabel = null) : AIFunction
    {
        public override string Name => inner.Name;
        public override string Description => inner.Description;
        public override JsonElement JsonSchema => inner.JsonSchema;
        public override JsonElement? ReturnJsonSchema => inner.ReturnJsonSchema;
        public override IReadOnlyDictionary<string, object?> AdditionalProperties => inner.AdditionalProperties;

        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var record = await autoSnapshot.TakeBeforeWriteAsync(Name, cancellationToken).ConfigureAwait(false);

            if (record is not null && channel is not null)
            {
                await channel.WriteAsync(
                        new AgentStreamItem(AgentStreamItemKind.Step,
                            $"Took restore point {record.Id.ToString(CultureInfo.InvariantCulture)} before {Name}.",
                            agentLabel), cancellationToken)
                    .ConfigureAwait(false);
            }

            return await inner.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
        }
    }
}