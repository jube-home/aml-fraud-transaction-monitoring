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
using System.Linq;
using Jube.Parser.Models;

namespace Jube.Parser
{
    public static class RuleVariableEmitter
    {
        public const string RecordMethod = "MatchRecord";
        public const string ScanName = "Scan";

        private const string EntryPrefix = "Public Shared Function Match(";

        public static bool HasScan(IReadOnlyList<RuleVariable> variables)
        {
            return variables != null && variables.Any(v => v.Kind == RuleVariableKind.Scan);
        }

        public static IEnumerable<string> Fields(IReadOnlyList<RuleVariable> variables)
        {
            return (variables ?? []).Where(v => v.Kind == RuleVariableKind.Scan).Select(v =>
                v.Initialiser == null
                    ? $"Private {v.Name} As {v.Type}"
                    : $"Private {v.Name} As {v.Type} = {v.Initialiser}");
        }

        public static IEnumerable<string> Locals(IReadOnlyList<RuleVariable> variables)
        {
            return (variables ?? []).Where(v => v.Kind == RuleVariableKind.Record)
                .Select(v => $"Dim {v.Name} As {v.Type}");
        }

        public static IEnumerable<string> Entry(string className, string signature)
        {
            return
            [
                signature,
                $"Dim {ScanName} As {className} = Nothing",
                $"If Cursor IsNot Nothing Then {ScanName} = TryCast(Cursor.State, {className})",
                $"If {ScanName} Is Nothing Then",
                $"{ScanName} = New {className}()",
                $"If Cursor IsNot Nothing Then Cursor.State = {ScanName}",
                "End If",
                $"Return {ScanName}.{RecordMethod}(Data,List,KVP,Svirfd,Cursor,Log)",
                "End Function",
                $"Private Function {RecordMethod}({signature[EntryPrefix.Length..]}"
            ];
        }

        public static string Signature(IReadOnlyList<RuleVariable> variables)
        {
            return variables == null || variables.Count == 0
                ? string.Empty
                : "\n" + string.Join("\n", Fields(variables).Concat(Locals(variables)));
        }
    }
}
