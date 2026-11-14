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
using Jube.AskJooby.Security.Interfaces;
using Jube.AskJooby.Security.Models;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Security
{
    public sealed class ApprovalGatedAiFunction(AIFunction inner, IToolApprovalGate gate) : AIFunction
    {
        public override string Name => inner.Name;
        public override string Description => inner.Description;
        public override JsonElement JsonSchema => inner.JsonSchema;
        public override JsonElement? ReturnJsonSchema => inner.ReturnJsonSchema;
        public override IReadOnlyDictionary<string, object?> AdditionalProperties => inner.AdditionalProperties;

        protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var snapshot = arguments.ToDictionary(pair => pair.Key, pair => pair.Value);

            return gate.Review(Name, snapshot) switch
            {
                ToolApprovalVerdict.Proceed => inner.InvokeAsync(arguments, cancellationToken),
                ToolApprovalVerdict.Rejected => ValueTask.FromResult<object?>(
                    "Not done: the user declined this change. Do not try it again unless they ask for it " +
                    "afresh, and do not look for another way to make the same change."),
                _ => ValueTask.FromResult<object?>(
                    "Not done yet: this change needs the user's approval and nothing has been changed. The user " +
                    "has been shown exactly what you asked for with an Approve and a Decline button. Tell them " +
                    "in one sentence what you intend to change and why, then stop. Only repeat this identical " +
                    "call after the user tells you they approved it.")
            };
        }
    }
}
