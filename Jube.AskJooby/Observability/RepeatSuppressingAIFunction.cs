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

using System.Text.Json;
using Jube.AskJooby.Observability.Interfaces;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Observability
{
    public sealed class RepeatSuppressingAiFunction(AIFunction inner, IRepeatedToolLedger ledger) : AIFunction
    {
        public override string Name => inner.Name;
        public override string Description => inner.Description;
        public override JsonElement JsonSchema => inner.JsonSchema;
        public override JsonElement? ReturnJsonSchema => inner.ReturnJsonSchema;
        public override IReadOnlyDictionary<string, object?> AdditionalProperties => inner.AdditionalProperties;

        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            if (!ledger.CanRepeat(Name))
            {
                return await inner.InvokeAsync(arguments, cancellationToken);
            }

            string signature;

            try
            {
                signature = JsonSerializer.Serialize(arguments);
            }
            catch (NotSupportedException)
            {
                return await inner.InvokeAsync(arguments, cancellationToken);
            }

            if (ledger.TryRecall(Name, signature, out var previous))
            {
                return previous;
            }

            var result = await inner.InvokeAsync(arguments, cancellationToken);
            ledger.Remember(Name, signature, result);

            return result;
        }
    }
}