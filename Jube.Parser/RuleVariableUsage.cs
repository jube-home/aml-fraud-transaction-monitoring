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
using System.Globalization;
using System.Linq;
using Jube.Parser.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Jube.Parser
{
    public static class RuleVariableUsage
    {
        public const string UseCode = "VariableUse";

        public static List<ErrorSpan> Check(string ruleText, IReadOnlyList<RuleVariable> variables,
            ISet<string> names)
        {
            if (names.Count == 0)
            {
                return [];
            }

            var wrapped = EngineRuleWrapper.AbstractionRule(ruleText, true, variables);
            var end = wrapped.CharOffset + ruleText.Length;
            var root = VisualBasicSyntaxTree.ParseText(wrapped.Text).GetRoot();

            var method = root.DescendantNodes().OfType<MethodBlockSyntax>()
                .LastOrDefault(block => block.SpanStart < wrapped.CharOffset);

            return
            [
                .. root.DescendantTokens(descendIntoTrivia: true)
                    .Where(token => token.SpanStart >= wrapped.CharOffset && token.SpanStart < end &&
                                    token.IsKind(SyntaxKind.IdentifierToken) && names.Contains(token.ValueText))
                    .Where(token => method == null || !method.Span.Contains(token.Span) || !IsValueUse(token))
                    .Select(token => Error(token, wrapped.LineOffset))
            ];
        }

        private static ErrorSpan Error(SyntaxToken token, int lineOffset)
        {
            var position = token.GetLocation().GetLineSpan();
            var line = position.StartLinePosition.Line - lineOffset;

            return new ErrorSpan
            {
                Code = UseCode,
                Line = line,
                Column = position.StartLinePosition.Character,
                EndColumn = position.StartLinePosition.Character + token.Span.Length,
                Message =
                    $"Line {(line + 1).ToString(CultureInfo.InvariantCulture)}: the variable '{token.ValueText}' can only be read, assigned or have a method called on it. It cannot follow a dot or stand where a type or a name is expected."
            };
        }

        private static bool IsValueUse(SyntaxToken token)
        {
            if (token.Parent is not IdentifierNameSyntax name)
            {
                return false;
            }

            var previous = token.GetPreviousToken();

            if (previous.IsKind(SyntaxKind.DotToken) || previous.IsKind(SyntaxKind.ExclamationToken))
            {
                return false;
            }

            return name.Parent switch
            {
                MemberAccessExpressionSyntax access => access.Expression == name &&
                                                       access.IsKind(SyntaxKind.SimpleMemberAccessExpression),
                SimpleArgumentSyntax argument => argument.Expression == name && argument.NameColonEquals == null,
                BinaryExpressionSyntax or UnaryExpressionSyntax or ParenthesizedExpressionSyntax => true,
                AssignmentStatementSyntax => true,
                IfStatementSyntax or ElseIfStatementSyntax or SingleLineIfStatementSyntax => true,
                ReturnStatementSyntax or SelectStatementSyntax => true,
                SimpleCaseClauseSyntax or RangeCaseClauseSyntax or RelationalCaseClauseSyntax => true,
                TernaryConditionalExpressionSyntax or BinaryConditionalExpressionSyntax => true,
                _ => false
            };
        }
    }
}
