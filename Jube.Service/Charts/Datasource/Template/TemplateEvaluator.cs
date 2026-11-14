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


using System.Collections;
using Jube.Service.Charts.Datasource.Template.Models;

namespace Jube.Service.Charts.Datasource.Template
{
    public static class TemplateEvaluator
    {
        public static object? Evaluate(TemplateExpression expression, TemplateScope scope)
        {
            switch (expression)
            {
                case TemplateLiteral literal:
                    return literal.Value;
                case TemplateName name:
                    return scope.Lookup(name.Name);
                case TemplateMember member:
                    return Member(Evaluate(member.Target, scope), Evaluate(member.Key, scope));
                case TemplateUnary unary:
                    return Unary(unary, scope);
                case TemplateConditional conditional:
                    return TemplateValues.Truthy(Evaluate(conditional.Test, scope))
                        ? Evaluate(conditional.WhenTrue, scope)
                        : Evaluate(conditional.WhenFalse, scope);
                case TemplateBinary binary:
                    return Binary(binary, scope);
                case TemplateFilterCall call:
                    return TemplateFilters.Apply(call.Name, Evaluate(call.Input, scope),
                        call.Arguments.Select(a => Evaluate(a, scope)).ToList());
                default:
                    return null;
            }
        }

        private static object? Member(object? target, object? key)
        {
            switch (target)
            {
                case IDictionary<string, object?> map:
                    return key is string text ? Lookup(map, text) : null;
                case string value when IsLength(key):
                    return value.Length;
                case IList list:
                    if (IsLength(key))
                    {
                        return list.Count;
                    }

                    return TemplateValues.AsNumber(key) is { } number && number == decimal.Truncate(number) &&
                           number >= 0 && number < list.Count
                        ? list[(int)number]
                        : null;
                default:
                    return null;
            }
        }

        private static bool IsLength(object? key)
        {
            return key is string text && string.Equals(text, "length", StringComparison.OrdinalIgnoreCase);
        }

        private static object? Lookup(IDictionary<string, object?> map, string text)
        {
            if (map.TryGetValue(text, out var found))
            {
                return found;
            }

            foreach (var pair in map)
            {
                if (string.Equals(pair.Key, text, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }

            return null;
        }

        private static object? Unary(TemplateUnary unary, TemplateScope scope)
        {
            var value = Evaluate(unary.Operand, scope);

            return unary.Operator == "!"
                ? !TemplateValues.Truthy(value)
                : TemplateValues.AsNumber(value) is { } number
                    ? -number
                    : null;
        }

        private static object? Binary(TemplateBinary binary, TemplateScope scope)
        {
            if (binary.Operator == "&&")
            {
                var left = Evaluate(binary.Left, scope);

                return TemplateValues.Truthy(left) ? Evaluate(binary.Right, scope) : left;
            }

            if (binary.Operator == "||")
            {
                var left = Evaluate(binary.Left, scope);

                return TemplateValues.Truthy(left) ? left : Evaluate(binary.Right, scope);
            }

            var a = Evaluate(binary.Left, scope);
            var b = Evaluate(binary.Right, scope);

            switch (binary.Operator)
            {
                case "==":
                    return TemplateValues.Equal(a, b);
                case "!=":
                    return !TemplateValues.Equal(a, b);
                case "<":
                    return TemplateValues.Compare(a, b) is < 0;
                case "<=":
                    return TemplateValues.Compare(a, b) is <= 0;
                case ">":
                    return TemplateValues.Compare(a, b) is > 0;
                case ">=":
                    return TemplateValues.Compare(a, b) is >= 0;
                case "+":
                    return TemplateValues.AsNumber(a) is { } x && TemplateValues.AsNumber(b) is { } y
                        ? Arithmetic("+", x, y)
                        : TemplateValues.Display(a) + TemplateValues.Display(b);
                default:
                    return TemplateValues.AsNumber(a) is { } p && TemplateValues.AsNumber(b) is { } q
                        ? Arithmetic(binary.Operator, p, q)
                        : null;
            }
        }

        private static object? Arithmetic(string op, decimal x, decimal y)
        {
            try
            {
                return op switch
                {
                    "+" => x + y,
                    "-" => x - y,
                    "*" => x * y,
                    "/" => y == 0 ? null : x / y,
                    "%" => y == 0 ? null : x % y,
                    _ => null
                };
            }
            catch (OverflowException)
            {
                return null;
            }
        }
    }
}
