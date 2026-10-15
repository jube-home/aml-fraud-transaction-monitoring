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

using System.ComponentModel;
using Jube.Service.Agent.RuleExtensionTools.Models;

namespace Jube.Service.Agent.RuleExtensionTools
{
    public sealed class RuleExtensionTools
    {
        [Description("Searches the rule language for extension methods by name or by what they do, returning "
                     + "Visual Basic signatures ready to paste into a rule fragment. Search before writing any "
                     + "fragment that uses a method not already in front of you: the surface is over a thousand "
                     + "methods and a guessed argument list compiles into nothing.")]
        public IReadOnlyList<RuleExtensionSignature> RuleExtensionMethodSearch(
            [Description("Words to look for in the method name, such as \"fuzzy\", \"iban\", \"round\" or "
                         + "\"business hours\". Each word must appear somewhere in the name, in any order and "
                         + "ignoring case. Leave empty to list everything the instance type offers.")]
            string query = "",
            [Description("Restrict to methods callable on this Visual Basic type, such as String, Double, "
                         + "Integer, DateTime or Boolean. This is the type of whatever sits to the left of the "
                         + "dot at that point in the pipeline. Leave empty for all types.")]
            string instanceType = "",
            [Description("Maximum signatures to return.")]
            int maximumResults = 60)
        {
            var words = query.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries
                                                     | StringSplitOptions.TrimEntries);

            return RuleExtensionCatalogue.All
                .Where(signature => string.IsNullOrWhiteSpace(instanceType)
                                    || string.Equals(signature.Instance.TrimEnd('?'), instanceType.Trim(),
                                        StringComparison.OrdinalIgnoreCase))
                .Where(signature => words.All(word =>
                    signature.Name.Contains(word, StringComparison.OrdinalIgnoreCase)))
                .Take(Math.Clamp(maximumResults, 1, 400))
                .ToList();
        }

        [Description("Returns every overload of one named rule extension method, with its Visual Basic "
                     + "signature, the type it is called on and the type it returns. Use it to confirm an exact "
                     + "argument list before writing the call.")]
        public IReadOnlyList<RuleExtensionSignature> RuleExtensionMethodGet(
            [Description("The exact method name, such as LevenshteinDistance or IsJustBelowThreshold.")]
            string name)
        {
            return RuleExtensionCatalogue.All
                .Where(signature => string.Equals(signature.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        [Description("Checks a chained rule fragment against the real method surface before it goes anywhere "
                     + "near a rule, resolving each link and tracking the type through the chain. Validations "
                     + "are gold: this one is free, it is exact, and it names the link that broke and the type "
                     + "the expression held at that point, which a whole-rule validation cannot do.")]
        public RuleExtensionValidationResult RuleExtensionMethodValidate(
            [Description("The fragment to check, such as "
                         + "Payload.Narrative.RemoveDiacritics().ContainsAnyIgnoreCase(\"crypto\",\"wallet\"). "
                         + "A leading path that is not a method, such as Payload.Narrative or "
                         + "Abstraction.AverageAmount, is taken as the thing being operated on.")]
            string expression,
            [Description("The Visual Basic type the chain starts from, which is the type of the payload field "
                         + "or abstraction on the left. String unless the value is numeric, a date or a flag.")]
            string receiverType = "String")
        {
            return RuleExtensionChainValidator.Validate(expression, receiverType);
        }
    }
}