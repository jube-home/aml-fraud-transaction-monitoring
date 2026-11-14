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

using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

namespace Jube.RegulatoryReport
{
    internal sealed class TemplateRenderer(ReportProfile profile, JsonNode root, List<ReportIssue> issues)
    {
        public const string Namespace = "urn:jube:regulatory-report:template:1";
        public const int MaximumDepth = 64;
        public const int MaximumRepeats = 100_000;

        private static readonly XNamespace jr = Namespace;
        private int produced;

        public static XDocument Parse(string template)
        {
            if (template.Length > ReportProfile.MaximumTemplateCharacters)
            {
                throw new InvalidDataException("The template is too large.");
            }

            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };

            using var reader = XmlReader.Create(new StringReader(template), settings);

            return XDocument.Load(reader, LoadOptions.SetLineInfo);
        }

        public XElement? Render(XElement template)
        {
            return RenderElement(template, root, "$", 0).FirstOrDefault();
        }

        private IEnumerable<XElement> RenderElement(XElement template, JsonNode scope, string scopePath, int depth)
        {
            if (depth > MaximumDepth)
            {
                Add("TemplateTooDeep", $"The template nests deeper than {MaximumDepth} levels.", scopePath, template);

                yield break;
            }

            var each = Directive(template, "each");

            if (each is null)
            {
                var one = RenderOne(template, scope, scopePath, depth);

                if (one is not null)
                {
                    yield return one;
                }

                yield break;
            }

            var node = JsonPath.Resolve(root, scope, each);

            if (node is null)
            {
                if (IsTrue(template, "required"))
                {
                    Add("MissingRequired", $"'{each}' is required but is missing.", Combine(scopePath, each), template);
                }

                yield break;
            }

            if (node is not JsonArray array)
            {
                Add("NotAList", $"'{each}' is not a list.", Combine(scopePath, each), template);

                yield break;
            }

            if (array.Count == 0 && IsTrue(template, "required"))
            {
                Add("MissingRequired", $"'{each}' is required but is empty.", Combine(scopePath, each), template);
            }

            if (array.Count > MaximumRepeats)
            {
                Add("TooManyItems", $"'{each}' has more than {MaximumRepeats} items.", Combine(scopePath, each),
                    template);

                yield break;
            }

            for (var i = 0; i < array.Count; i++)
            {
                if (array[i] is null)
                {
                    continue;
                }

                var one = RenderOne(template, array[i]!, $"{Combine(scopePath, each)}[{i}]", depth);

                if (one is not null)
                {
                    yield return one;
                }
            }
        }

        private XElement? RenderOne(XElement template, JsonNode scope, string scopePath, int depth)
        {
            var condition = Directive(template, "if");

            if (condition is not null && !JsonPath.Truthy(JsonPath.Resolve(root, scope, condition)))
            {
                return null;
            }

            if (++produced > MaximumRepeats * 10)
            {
                Add("OutputTooLarge", "The report has too many elements.", scopePath, template);

                return null;
            }

            var element = new XElement(template.Name);

            foreach (var attribute in template.Attributes().Where(a => a.Name.Namespace != jr
                                                                       && !a.IsNamespaceDeclaration))
            {
                element.SetAttributeValue(attribute.Name, attribute.Value);
            }

            foreach (var declaration in template.Attributes().Where(a => a.IsNamespaceDeclaration
                                                                         && a.Value != Namespace))
            {
                element.SetAttributeValue(declaration.Name, declaration.Value);
            }

            var attributes = Directive(template, "attributes");

            if (attributes is not null)
            {
                foreach (var pair in attributes.Split(';', StringSplitOptions.RemoveEmptyEntries
                                                           | StringSplitOptions.TrimEntries))
                {
                    var equals = pair.IndexOf('=');

                    if (equals <= 0)
                    {
                        Add("BadAttributes", $"'{pair}' is not name=expression.", scopePath, template);

                        continue;
                    }

                    var value = Evaluate(pair[(equals + 1)..], scope, scopePath, template, out _);

                    if (!string.IsNullOrEmpty(value))
                    {
                        element.SetAttributeValue(XName.Get(pair[..equals].Trim()), value);
                    }
                }
            }

            var expression = Directive(template, "value");

            if (expression is not null)
            {
                var value = Evaluate(expression, scope, scopePath, template, out var failed);

                if (string.IsNullOrEmpty(value) && !failed)
                {
                    value = Directive(template, "default");
                }

                if (string.IsNullOrEmpty(value))
                {
                    if (IsTrue(template, "required") && !failed)
                    {
                        Add("MissingRequired", $"'{expression}' is required but is missing or empty.",
                            Combine(scopePath, expression.Split('|')[0].Trim()), template);
                    }

                    if (!IsTrue(template, "keep-empty"))
                    {
                        return null;
                    }
                }
                else
                {
                    element.Add(value);
                }
            }
            else
            {
                foreach (var child in template.Nodes())
                {
                    switch (child)
                    {
                        case XElement childElement:
                            foreach (var rendered in RenderElement(childElement, scope, scopePath, depth + 1))
                            {
                                element.Add(rendered);
                            }

                            break;
                        case XText text when !string.IsNullOrWhiteSpace(text.Value):
                            element.Add(text.Value);

                            break;
                    }
                }
            }

            if (IsTrue(template, "prune") && !element.HasAttributes && !element.Nodes().Any())
            {
                return null;
            }

            return element;
        }

        private string? Evaluate(string expression, JsonNode scope, string scopePath, XElement template,
            out bool failed)
        {
            failed = false;
            var parts = expression.Split('|', StringSplitOptions.TrimEntries);
            var path = parts[0];
            var node = JsonPath.Resolve(root, scope, path);
            var location = Combine(scopePath, path);

            if (node is JsonArray or JsonObject)
            {
                Add("NotAValue", $"'{path}' is a list or object, not a single value.", location, template);
                failed = true;

                return null;
            }

            var (formatted, issue) = ValueFormatter.Apply(JsonPath.Scalar(node), parts.Skip(1), profile, location);

            if (issue is not null)
            {
                issues.Add(issue with { Line = Line(template), Position = Position(template) });
                failed = true;

                return null;
            }

            return formatted;
        }

        private static string Combine(string scopePath, string path)
        {
            var trimmed = path.Trim();

            if (trimmed.StartsWith('$'))
            {
                return trimmed;
            }

            return trimmed is "" or "." ? scopePath : $"{scopePath}.{trimmed}";
        }

        private static string? Directive(XElement element, string name)
        {
            var value = element.Attribute(jr + name)?.Value;

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static bool IsTrue(XElement element, string name)
        {
            return string.Equals(Directive(element, name), "true", StringComparison.OrdinalIgnoreCase);
        }

        private static int? Line(XElement element)
        {
            return ((IXmlLineInfo)element).HasLineInfo() ? ((IXmlLineInfo)element).LineNumber : null;
        }

        private static int? Position(XElement element)
        {
            return ((IXmlLineInfo)element).HasLineInfo() ? ((IXmlLineInfo)element).LinePosition : null;
        }

        private void Add(string code, string message, string path, XElement template)
        {
            issues.Add(new ReportIssue(ReportIssueStage.Mapping, code, message, path, Line(template),
                Position(template)));
        }
    }
}
