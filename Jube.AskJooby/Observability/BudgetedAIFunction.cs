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
    public sealed class BudgetedAiFunction(
        AIFunction inner,
        ITokenBudget budget,
        ITurnActivityLedger? activity = null) : AIFunction
    {
        public override string Name => inner.Name;
        public override string Description => inner.Description;
        public override JsonElement JsonSchema => inner.JsonSchema;
        public override JsonElement? ReturnJsonSchema => inner.ReturnJsonSchema;
        public override IReadOnlyDictionary<string, object?> AdditionalProperties => inner.AdditionalProperties;

        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            if (budget.IsExhausted)
            {
                return
                    "You have done a lot of digging on this turn, so stop here and take stock rather "
                    + "than press on. Do not call another tool, and do not try this one again. Tell the "
                    + "caller what you have established so far, name the one thing you were about to do "
                    + "next, and ask whether you are on the right lines -- so that a yes is all it takes "
                    + "to carry on. Say plainly what is still outstanding.";
            }

            activity?.RecordToolCall();

            return await inner.InvokeAsync(arguments, cancellationToken);
        }
    }
}