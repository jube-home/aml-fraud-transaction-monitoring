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

namespace Jube.Parser
{
    using System;
    using System.Collections.Generic;

    public sealed record RuleReference(string Namespace, string Name, int Line)
    {
        public const string Payload = "Payload";
        public const string TtlCounter = "TTLCounter";
        public const string Abstraction = "Abstraction";
        public const string Dictionary = "Dictionary";
        public const string Sanction = "Sanction";
        public const string AbstractionCalculation = "AbstractionCalculation";
        public const string ExhaustiveAdaptation = "ExhaustiveAdaptation";
        public const string HttpAdaptation = "HTTPAdaptation";
        public const string List = "List";
        public const string Activation = "Activation";

        private static readonly Dictionary<string, string> namespaces = new(StringComparer.OrdinalIgnoreCase)
        {
            [Payload] = Payload,
            [TtlCounter] = TtlCounter,
            [Abstraction] = Abstraction,
            [Dictionary] = Dictionary,
            [Sanction] = Sanction,
            [AbstractionCalculation] = AbstractionCalculation,
            [ExhaustiveAdaptation] = ExhaustiveAdaptation,
            [HttpAdaptation] = HttpAdaptation,
            [List] = List,
            [Activation] = Activation
        };

        public string CompletionName => $"{Namespace}.{Name}";

        internal static void TryAdd(List<RuleReference> references, string prefix, string name, int line)
        {
            if (!namespaces.TryGetValue(prefix, out var canonical) || string.IsNullOrEmpty(name))
            {
                return;
            }

            references.Add(new RuleReference(canonical, name, line));
        }
    }
}