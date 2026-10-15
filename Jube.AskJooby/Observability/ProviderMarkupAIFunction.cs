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
using Jube.AskJooby.Reasoning;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Observability
{
    public sealed class ProviderMarkupAiFunction(AIFunction inner) : AIFunction
    {
        public override string Name => inner.Name;
        public override string Description => inner.Description;
        public override JsonElement JsonSchema => inner.JsonSchema;
        public override JsonElement? ReturnJsonSchema => inner.ReturnJsonSchema;
        public override IReadOnlyDictionary<string, object?> AdditionalProperties => inner.AdditionalProperties;
        public override JsonSerializerOptions JsonSerializerOptions => inner.JsonSerializerOptions;

        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var leaked = arguments
                .Where(argument => argument.Value is string text && ProviderMarkup.Contains(text))
                .ToList();

            if (leaked.Count == 0)
            {
                return await inner.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
            }

            string? intended = null;

            foreach (var argument in leaked)
            {
                var text = (string)argument.Value!;
                intended ??= ProviderMarkup.IntendedToolName(text);
                arguments[argument.Key] = ProviderMarkup.Strip(text);
            }

            var result = await inner.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);

            return Annotate(result, Notice(intended));
        }

        private string Notice(string? intended)
        {
            var which = intended is null
                ? "A tool call you made"
                : $"Your call to {intended}";

            return $"IMPORTANT -- {which} did not run. It arrived as text inside the arguments of this "
                   + $"{Name} call, so the provider never sent it as a tool call at all. The text has been "
                   + "cut off, so this tool received only what you meant to send it, and nothing you asked "
                   + "for in that other call has happened. Issue it again as a real tool call before going "
                   + "any further, and do not treat it as done.";
        }

        private static object Annotate(object? result, string notice)
        {
            return result switch
            {
                ToolInvocationFailure failure => new ToolInvocationFailure(
                    failure.ToolName, failure.Message + "\n\n" + notice),
                JsonElement { ValueKind: JsonValueKind.String } element => Join(element.GetString(), notice),
                JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => notice,
                string text => Join(text, notice),
                null => notice,
                _ => result
            };
        }

        private static string Join(string? text, string notice)
        {
            return string.IsNullOrEmpty(text) ? notice : text + "\n\n" + notice;
        }
    }
}