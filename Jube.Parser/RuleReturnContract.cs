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
    using System.Linq;
    using Microsoft.CodeAnalysis.VisualBasic;
    using Microsoft.CodeAnalysis.VisualBasic.Syntax;
    using Models;

    public static class RuleReturnContract
    {
        private const string Header = "Public Class RuleContract\r\nPublic Sub Contract()\r\n";
        private const string Footer = "\r\nEnd Sub\r\nEnd Class\r\n";
        private const int HeaderLines = 2;
        private const string ResultName = "Matched";

        public static ErrorSpan Check(string ruleText)
        {
            var root = VisualBasicSyntaxTree.ParseText(Header + ruleText + Footer).GetRoot();
            var method = root.DescendantNodes().OfType<MethodBlockSyntax>().FirstOrDefault();

            if (method == null)
            {
                return null;
            }

            var returns = method.DescendantNodes().OfType<ReturnStatementSyntax>().Any();

            var assignsResult = method.DescendantNodes().OfType<AssignmentStatementSyntax>()
                .Any(a => a.Left is IdentifierNameSyntax name &&
                          string.Equals(name.Identifier.ValueText, ResultName, StringComparison.OrdinalIgnoreCase));

            if (returns || assignsResult)
            {
                return null;
            }

            var discarded = method.DescendantNodes().OfType<ExpressionStatementSyntax>().FirstOrDefault();

            if (discarded == null)
            {
                return new ErrorSpan
                {
                    Message = "The rule never sets Matched and never returns, so the wrapper answers every input " +
                              "with the default. Assign Matched, as in Matched = Payload.Amount > 100, or return " +
                              "early with Return True or Return False."
                };
            }

            var line = discarded.GetLocation().GetLineSpan().StartLinePosition.Line - HeaderLines;
            var expression = discarded.Expression.ToString().Trim();

            return new ErrorSpan
            {
                Line = line,
                Start = discarded.SpanStart - Header.Length,
                Length = discarded.Span.Length,
                Message = $"Line {line + 1}: {expression} is evaluated and its result is discarded, and the rule " +
                          $"never sets Matched or returns, so the wrapper answers every input with the default. " +
                          $"Assign the result, as in Matched = {expression}, or return early with Return True or " +
                          $"Return False."
            };
        }
    }
}