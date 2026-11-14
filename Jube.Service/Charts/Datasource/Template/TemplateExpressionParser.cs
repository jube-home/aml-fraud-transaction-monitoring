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


using System.Globalization;
using System.Text;
using Jube.Data.Reporting;
using Jube.Service.Charts.Datasource.Template.Models;

namespace Jube.Service.Charts.Datasource.Template
{
    public sealed class TemplateExpressionParser
    {
        private readonly int column;
        private readonly IReadOnlySet<string> names;
        private readonly int line;
        private readonly string source;
        private readonly List<(string Kind, string Text, int Position)> tokens;

        private int cursor;
        private int depth;

        private TemplateExpressionParser(string source, int line, int column, IReadOnlySet<string> names)
        {
            this.source = source;
            this.line = line;
            this.column = column;
            this.names = names;
            tokens = Tokenise();
        }

        public static TemplateExpression Parse(string source, int line, int column, IReadOnlySet<string> names)
        {
            var parser = new TemplateExpressionParser(source, line, column, names);
            var expression = parser.ParsePipe();

            if (parser.Peek.Kind != "end")
            {
                throw parser.Invalid(parser.Peek);
            }

            return expression;
        }

        private (string Kind, string Text, int Position) Peek => tokens[cursor];

        private DatasourceChartException Invalid((string Kind, string Text, int Position) near)
        {
            var text = near.Kind == "end" ? "end" : near.Text;

            return new DatasourceChartException(DatasourceMessages.TemplateExpressionInvalid, line, column,
                source.Trim(), text);
        }

        private bool Accept(string text)
        {
            if (Peek.Kind != "punct" || Peek.Text != text)
            {
                return false;
            }

            cursor++;

            return true;
        }

        private void Expect(string text)
        {
            if (!Accept(text))
            {
                throw Invalid(Peek);
            }
        }

        private T Nested<T>(Func<T> parse)
        {
            if (++depth > TemplateLimits.MaximumExpressionDepth)
            {
                throw new DatasourceChartException(DatasourceMessages.TemplateTooDeep, line, column,
                    TemplateLimits.MaximumExpressionDepth);
            }

            try
            {
                return parse();
            }
            finally
            {
                depth--;
            }
        }

        private TemplateExpression ParsePipe()
        {
            var expression = ParseConditional();

            while (Accept("|"))
            {
                if (Peek.Kind != "name")
                {
                    throw Invalid(Peek);
                }

                var name = tokens[cursor++].Text;

                if (!TemplateFilters.Exists(name))
                {
                    throw new DatasourceChartException(DatasourceMessages.TemplateFilterUnknown, line, column, name);
                }

                var arguments = new List<TemplateExpression>();

                if (Accept("("))
                {
                    if (!Accept(")"))
                    {
                        do
                        {
                            arguments.Add(ParseConditional());
                        } while (Accept(","));

                        Expect(")");
                    }
                }

                if (!TemplateFilters.AcceptsArguments(name, arguments.Count))
                {
                    throw new DatasourceChartException(DatasourceMessages.TemplateFilterArguments, line, column, name);
                }

                expression = new TemplateFilterCall(expression, name, arguments);
            }

            return expression;
        }

        private TemplateExpression ParseConditional()
        {
            return Nested(() =>
            {
                var test = ParseBinary(0);

                if (!Accept("?"))
                {
                    return test;
                }

                var whenTrue = ParseConditional();

                Expect(":");

                return new TemplateConditional(test, whenTrue, ParseConditional());
            });
        }

        private static readonly string[][] Levels =
        [
            ["||"],
            ["&&"],
            ["==", "!=", "===", "!=="],
            ["<", "<=", ">", ">="],
            ["+", "-"],
            ["*", "/", "%"]
        ];

        private TemplateExpression ParseBinary(int level)
        {
            if (level == Levels.Length)
            {
                return ParseUnary();
            }

            var left = ParseBinary(level + 1);

            while (Peek.Kind == "punct" && Levels[level].Contains(Peek.Text))
            {
                var op = tokens[cursor++].Text;
                var normalised = op switch { "===" => "==", "!==" => "!=", _ => op };

                left = new TemplateBinary(normalised, left, ParseBinary(level + 1));
            }

            return left;
        }

        private TemplateExpression ParseUnary()
        {
            return Nested(() =>
            {
                if (Accept("!"))
                {
                    return new TemplateUnary("!", ParseUnary());
                }

                return Accept("-") ? new TemplateUnary("-", ParseUnary()) : ParsePostfix();
            });
        }

        private TemplateExpression ParsePostfix()
        {
            var target = ParsePrimary();

            while (true)
            {
                if (Accept("."))
                {
                    if (Peek.Kind != "name")
                    {
                        throw Invalid(Peek);
                    }

                    target = new TemplateMember(target, new TemplateLiteral(tokens[cursor++].Text));
                }
                else if (Accept("["))
                {
                    var key = ParsePipe();

                    Expect("]");
                    target = new TemplateMember(target, key);
                }
                else
                {
                    return target;
                }
            }
        }

        private TemplateExpression ParsePrimary()
        {
            var token = Peek;

            switch (token.Kind)
            {
                case "number":
                    cursor++;

                    return new TemplateLiteral(decimal.Parse(token.Text, CultureInfo.InvariantCulture));
                case "string":
                    cursor++;

                    return new TemplateLiteral(token.Text);
                case "name":
                    cursor++;

                    switch (token.Text)
                    {
                        case "true":
                            return new TemplateLiteral(true);
                        case "false":
                            return new TemplateLiteral(false);
                        case "null":
                            return new TemplateLiteral(null);
                    }

                    if (!names.Contains(token.Text))
                    {
                        throw new DatasourceChartException(DatasourceMessages.TemplateNameUnknown, line, column,
                            token.Text);
                    }

                    return new TemplateName(token.Text);
                case "punct" when token.Text == "(":
                    cursor++;

                    var inner = ParsePipe();

                    Expect(")");

                    return inner;
                default:
                    throw Invalid(token);
            }
        }

        private List<(string Kind, string Text, int Position)> Tokenise()
        {
            var found = new List<(string, string, int)>();
            var i = 0;

            while (i < source.Length)
            {
                var c = source[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;

                    continue;
                }

                var start = i;

                if (char.IsDigit(c) || (c == '.' && i + 1 < source.Length && char.IsDigit(source[i + 1])))
                {
                    while (i < source.Length && (char.IsDigit(source[i]) || source[i] == '.'))
                    {
                        i++;
                    }

                    var text = source[start..i];

                    if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                    {
                        throw new DatasourceChartException(DatasourceMessages.TemplateExpressionInvalid, line, column,
                            source.Trim(), text);
                    }

                    found.Add(("number", text, start));
                }
                else if (c is '\'' or '"')
                {
                    var builder = new StringBuilder();

                    i++;

                    while (i < source.Length && source[i] != c)
                    {
                        if (source[i] == '\\' && i + 1 < source.Length)
                        {
                            i++;
                        }

                        builder.Append(source[i]);
                        i++;
                    }

                    if (i >= source.Length)
                    {
                        throw new DatasourceChartException(DatasourceMessages.TemplateExpressionInvalid, line, column,
                            source.Trim(), source[start..]);
                    }

                    i++;
                    found.Add(("string", builder.ToString(), start));
                }
                else if (char.IsLetter(c) || c is '_' or '$')
                {
                    while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] is '_' or '$'))
                    {
                        i++;
                    }

                    found.Add(("name", source[start..i], start));
                }
                else
                {
                    var three = i + 3 <= source.Length ? source.Substring(i, 3) : string.Empty;
                    var two = i + 2 <= source.Length ? source.Substring(i, 2) : string.Empty;
                    string symbol;

                    if (three is "===" or "!==")
                    {
                        symbol = three;
                    }
                    else if (two is "==" or "!=" or "<=" or ">=" or "&&" or "||")
                    {
                        symbol = two;
                    }
                    else if ("()[].,?:!<>+-*/%|".Contains(c))
                    {
                        symbol = c.ToString();
                    }
                    else
                    {
                        throw new DatasourceChartException(DatasourceMessages.TemplateExpressionInvalid, line, column,
                            source.Trim(), c.ToString());
                    }

                    i += symbol.Length;
                    found.Add(("punct", symbol, start));
                }
            }

            found.Add(("end", string.Empty, source.Length));

            return found;
        }
    }
}
