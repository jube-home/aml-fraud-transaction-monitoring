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


using System.Net;
using System.Text.RegularExpressions;
using Jube.Data.Reporting;
using Jube.Service.Charts.Datasource.Template.Models;

namespace Jube.Service.Charts.Datasource.Template
{
    public sealed class TemplateParser
    {
        private readonly TemplateDialect dialect;
        private readonly string source;
        private readonly Stack<Frame> stack = new();

        private int position;

        private TemplateParser(string source, TemplateDialect dialect)
        {
            this.source = source;
            this.dialect = dialect;
        }

        public static IReadOnlyList<TemplateNode> Parse(string? text,
            TemplateDialect dialect = TemplateDialect.Datasource)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new DatasourceChartException(DatasourceMessages.TemplateEmpty);
            }

            if (text.Length > TemplateLimits.MaximumTextLength)
            {
                throw new DatasourceChartException(DatasourceMessages.TemplateTooLong,
                    TemplateLimits.MaximumTextLength);
            }

            return new TemplateParser(text, dialect).Run();
        }

        private static readonly Regex LoopPattern = new(
            @"^\s*(?:\(\s*(?<item>[A-Za-z_]\w*)\s*,\s*(?<index>[A-Za-z_]\w*)\s*\)|(?<item2>[A-Za-z_]\w*))\s+in\s+(?<source>.+?)\s*$",
            RegexOptions.Singleline | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        private (int Line, int Column) Where(int at)
        {
            var line = 1;
            var lineStart = 0;

            for (var i = 0; i < at && i < source.Length; i++)
            {
                if (source[i] != '\n')
                {
                    continue;
                }

                line++;
                lineStart = i + 1;
            }

            return (line, at - lineStart + 1);
        }

        private IReadOnlyList<TemplateNode> Run()
        {
            var root = new Frame(null, [], TemplateVocabulary.RootsFor(dialect), 0);

            stack.Push(root);

            while (position < source.Length)
            {
                if (Match("<!--"))
                {
                    var end = source.IndexOf("-->", position + 4, StringComparison.Ordinal);

                    position = end < 0 ? source.Length : end + 3;
                }
                else if (Match("</"))
                {
                    CloseTag();
                }
                else if (source[position] == '<' && position + 1 < source.Length && char.IsLetter(source[position + 1]))
                {
                    OpenTag();
                }
                else if (Match("[@"))
                {
                    position += 2;
                    Interpolate();
                }
                else
                {
                    Text();
                }
            }

            if (stack.Count > 1)
            {
                var open = stack.Peek();
                var (line, column) = Where(open.Start);

                throw new DatasourceChartException(DatasourceMessages.TemplateUnclosedTag, line, column,
                    open.Tag ?? string.Empty);
            }

            return root.Children;
        }

        private bool Match(string text)
        {
            return string.CompareOrdinal(source, position, text, 0, text.Length) == 0;
        }

        private void Text()
        {
            var start = position;

            while (position < source.Length && !Match("[@") && !Match("<!--") && !Match("</") &&
                   !(source[position] == '<' && position + 1 < source.Length && char.IsLetter(source[position + 1])))
            {
                position++;
            }

            if (position == start)
            {
                position++;
            }

            var raw = source[start..position];

            if (raw.Trim().Length == 0 && raw.Contains('\n'))
            {
                return;
            }

            var (line, column) = Where(start);

            Append(new TemplateTextNode(WebUtility.HtmlDecode(Regex.Replace(raw, @"\s+", " ")), line, column));
        }

        private void Interpolate()
        {
            var start = position - 2;
            var end = source.IndexOf("@]", position, StringComparison.Ordinal);
            var (line, column) = Where(start);

            if (end < 0)
            {
                throw new DatasourceChartException(DatasourceMessages.TemplateInterpolationOpen, line, column);
            }

            var expression = TemplateExpressionParser.Parse(source[position..end], line, column, stack.Peek().Names);

            position = end + 2;
            Append(new TemplateInterpolationNode(expression, line, column));
        }

        private void Append(TemplateNode node)
        {
            var frame = stack.Peek();

            if (node is TemplateElementNode { Branch: TemplateBranch.ElseIf or TemplateBranch.Else } element)
            {
                var previous = frame.Children.LastOrDefault(n => n is not TemplateTextNode { Text: " " });

                if (previous is not TemplateElementNode { Branch: TemplateBranch.If or TemplateBranch.ElseIf })
                {
                    throw new DatasourceChartException(DatasourceMessages.TemplateDirectiveInvalid, element.Line,
                        element.Column, element.Branch == TemplateBranch.Else ? "v-else" : "v-else-if");
                }
            }

            frame.Children.Add(node);
        }

        private void CloseTag()
        {
            var start = position;

            position += 2;

            var end = source.IndexOf('>', position);
            var (line, column) = Where(start);

            if (end < 0)
            {
                throw new DatasourceChartException(DatasourceMessages.TemplateUnexpectedClose, line, column,
                    source[position..].Trim());
            }

            var tag = source[position..end].Trim().ToLowerInvariant();

            position = end + 1;

            if (stack.Count == 1 || stack.Peek().Tag != tag)
            {
                throw new DatasourceChartException(DatasourceMessages.TemplateUnexpectedClose, line, column, tag);
            }

            Finish(stack.Pop());
        }

        private void Finish(Frame frame)
        {
            var (line, column) = Where(frame.Start);

            Append(new TemplateElementNode(frame.Tag!, frame.Attributes, frame.Children, frame.Branch,
                frame.Condition, frame.Loop, line, column));
        }

        private void OpenTag()
        {
            var start = position;
            var (line, column) = Where(start);

            position++;

            var nameStart = position;

            while (position < source.Length && (char.IsLetterOrDigit(source[position]) || source[position] == '-'))
            {
                position++;
            }

            var tag = source[nameStart..position].ToLowerInvariant();

            if (!TemplateVocabulary.TagAllowed(dialect, tag))
            {
                throw new DatasourceChartException(DatasourceMessages.TemplateTagNotAllowed, line, column, tag);
            }

            var raw = new List<(string Name, string? Value, int At)>();
            var selfClosing = false;

            while (true)
            {
                while (position < source.Length && char.IsWhiteSpace(source[position]))
                {
                    position++;
                }

                if (position >= source.Length)
                {
                    throw new DatasourceChartException(DatasourceMessages.TemplateUnclosedTag, line, column, tag);
                }

                if (source[position] == '>')
                {
                    position++;

                    break;
                }

                if (Match("/>"))
                {
                    position += 2;
                    selfClosing = true;

                    break;
                }

                raw.Add(ReadAttribute());
            }

            var parent = stack.Peek();
            var frame = Build(parent, tag, raw, start, line, column);

            if (TemplateVocabulary.VoidTags.Contains(tag) || selfClosing)
            {
                Finish(frame);

                return;
            }

            if (stack.Count > TemplateLimits.MaximumElementDepth)
            {
                throw new DatasourceChartException(DatasourceMessages.TemplateTooDeep, line, column,
                    TemplateLimits.MaximumElementDepth);
            }

            stack.Push(frame);
        }

        private (string Name, string? Value, int At) ReadAttribute()
        {
            var at = position;

            while (position < source.Length && !char.IsWhiteSpace(source[position]) &&
                   source[position] is not ('=' or '>' or '/'))
            {
                position++;
            }

            var name = source[at..position];

            if (name.Length == 0)
            {
                position++;

                return (source[at].ToString(), null, at);
            }

            if (position >= source.Length || source[position] != '=')
            {
                return (name, null, at);
            }

            position++;

            if (position < source.Length && source[position] is '"' or '\'')
            {
                var quote = source[position];
                var end = source.IndexOf(quote, position + 1);

                if (end < 0)
                {
                    var (line, column) = Where(at);

                    throw new DatasourceChartException(DatasourceMessages.TemplateUnclosedTag, line, column, name);
                }

                var value = source[(position + 1)..end];

                position = end + 1;

                return (name, value, at);
            }

            var valueStart = position;

            while (position < source.Length && !char.IsWhiteSpace(source[position]) && source[position] != '>')
            {
                position++;
            }

            return (name, source[valueStart..position], at);
        }

        private Frame Build(Frame parent, string tag, List<(string Name, string? Value, int At)> raw, int start,
            int line, int column)
        {
            TemplateLoop? loop = null;
            TemplateExpression? condition = null;
            var branch = TemplateBranch.None;
            var names = parent.Names;
            var attributes = new List<TemplateAttribute>();
            var directives = raw.Count(r => r.Name is "v-for" or "v-if" or "v-else-if" or "v-else");

            foreach (var (name, value, _) in raw)
            {
                switch (name)
                {
                    case "v-for":
                        var match = LoopPattern.Match(value ?? string.Empty);

                        if (!match.Success)
                        {
                            throw new DatasourceChartException(DatasourceMessages.TemplateLoopInvalid, line, column,
                                value ?? string.Empty);
                        }

                        var item = match.Groups["item"].Success ? match.Groups["item"].Value : match.Groups["item2"].Value;
                        var index = match.Groups["index"].Success ? match.Groups["index"].Value : null;
                        var sourceExpression = TemplateExpressionParser.Parse(match.Groups["source"].Value, line,
                            column, parent.Names);

                        loop = new TemplateLoop(item, index, sourceExpression);

                        var scoped = new HashSet<string>(parent.Names, StringComparer.OrdinalIgnoreCase) { item };

                        if (index != null)
                        {
                            scoped.Add(index);
                        }

                        names = scoped;

                        break;
                    case "v-if":
                    case "v-else-if":
                        branch = name == "v-if" ? TemplateBranch.If : TemplateBranch.ElseIf;
                        condition = TemplateExpressionParser.Parse(value ?? string.Empty, line, column, parent.Names);

                        break;
                    case "v-else":
                        branch = TemplateBranch.Else;

                        break;
                }
            }

            if (directives > 1)
            {
                throw new DatasourceChartException(DatasourceMessages.TemplateDirectiveInvalid, line, column,
                    "v-if");
            }

            foreach (var (name, value, _) in raw)
            {
                if (name.StartsWith("v-", StringComparison.Ordinal))
                {
                    if (name is not ("v-for" or "v-if" or "v-else-if" or "v-else"))
                    {
                        throw new DatasourceChartException(DatasourceMessages.TemplateDirectiveInvalid, line, column,
                            name);
                    }

                    continue;
                }

                if (tag == "template")
                {
                    throw new DatasourceChartException(DatasourceMessages.TemplateAttributeNotAllowed, line, column,
                        name, tag);
                }

                if (!TemplateVocabulary.AttributeAllowed(dialect, tag, name))
                {
                    throw new DatasourceChartException(DatasourceMessages.TemplateAttributeNotAllowed, line, column,
                        name, tag);
                }

                attributes.Add(Attribute(name, value ?? string.Empty, line, column, names));
            }

            return new Frame(tag, attributes, names, start) { Branch = branch, Condition = condition, Loop = loop };
        }

        private static TemplateAttribute Attribute(string name, string value, int line, int column,
            IReadOnlySet<string> names)
        {
            if (!value.Contains("[@", StringComparison.Ordinal))
            {
                return new TemplateAttribute(name, WebUtility.HtmlDecode(value), null);
            }

            var parts = new List<TemplateAttributePart>();
            var cursor = 0;

            while (cursor < value.Length)
            {
                var open = value.IndexOf("[@", cursor, StringComparison.Ordinal);

                if (open < 0)
                {
                    parts.Add(new TemplateAttributePart(WebUtility.HtmlDecode(value[cursor..]), null));

                    break;
                }

                if (open > cursor)
                {
                    parts.Add(new TemplateAttributePart(WebUtility.HtmlDecode(value[cursor..open]), null));
                }

                var close = value.IndexOf("@]", open + 2, StringComparison.Ordinal);

                if (close < 0)
                {
                    throw new DatasourceChartException(DatasourceMessages.TemplateInterpolationOpen, line, column);
                }

                parts.Add(new TemplateAttributePart(null,
                    TemplateExpressionParser.Parse(WebUtility.HtmlDecode(value[(open + 2)..close]), line, column,
                        names)));
                cursor = close + 2;
            }

            return parts is [{ Expression: not null } only]
                ? new TemplateAttribute(name, null, only.Expression)
                : new TemplateAttribute(name, null, null, parts);
        }

        private sealed class Frame(string? tag, List<TemplateAttribute> attributes, IReadOnlySet<string> names,
            int start)
        {
            public string? Tag { get; } = tag;
            public List<TemplateAttribute> Attributes { get; } = attributes;
            public IReadOnlySet<string> Names { get; } = names;
            public int Start { get; } = start;
            public List<TemplateNode> Children { get; } = [];
            public TemplateBranch Branch { get; init; }
            public TemplateExpression? Condition { get; init; }
            public TemplateLoop? Loop { get; init; }
        }
    }
}
