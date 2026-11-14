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
using System.Net;
using System.Text;
using Jube.Data.Reporting;
using Jube.Service.Charts.Datasource.Template.Models;

namespace Jube.Service.Charts.Datasource.Template
{
    public sealed class TemplateRenderer
    {
        private readonly StringBuilder output = new();
        private FormRenderState? form;
        private int steps;

        private TemplateRenderer()
        {
        }

        public static string Render(IReadOnlyList<TemplateNode> nodes, DatasourceFrame frame, bool truncated)
        {
            var renderer = new TemplateRenderer();
            var scope = TemplateScope.Empty
                .With("rows", Rows(frame))
                .With("columns", Columns(frame))
                .With("rowCount", frame.Rows.Count)
                .With("truncated", truncated);

            renderer.Nodes(nodes, scope);

            return renderer.output.ToString();
        }

        public static FormRender RenderForm(IReadOnlyList<TemplateNode> nodes,
            IReadOnlyDictionary<string, object?> roots, IReadOnlyDictionary<string, string?> values, bool readOnly)
        {
            var state = new FormRenderState(new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase),
                readOnly);
            var renderer = new TemplateRenderer { form = state };
            var scope = roots.Aggregate(TemplateScope.Empty, (current, root) => current.With(root.Key, root.Value))
                .With("Form", new Dictionary<string, object?>(
                    values.ToDictionary(v => v.Key, v => (object?)v.Value), StringComparer.OrdinalIgnoreCase));

            renderer.Nodes(nodes, scope);

            return new FormRender(renderer.output.ToString(), state.Visible);
        }

        private static List<object?> Rows(DatasourceFrame frame)
        {
            var rows = new List<object?>(frame.Rows.Count);

            foreach (var row in frame.Rows)
            {
                var map = new Dictionary<string, object?>(frame.Columns.Count, StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < frame.Columns.Count; i++)
                {
                    map[frame.Columns[i].Name] = DatasourceCardValue.Normalise(row[i]);
                }

                rows.Add(map);
            }

            return rows;
        }

        private static List<object?> Columns(DatasourceFrame frame)
        {
            return frame.Columns.Select(c => (object?)new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["name"] = c.Name,
                ["ordinal"] = c.Ordinal,
                ["type"] = c.DataTypeName
            }).ToList();
        }

        private void Step()
        {
            if (++steps > TemplateLimits.MaximumSteps)
            {
                throw new DatasourceChartException(DatasourceMessages.TemplateTooManySteps,
                    TemplateLimits.MaximumSteps);
            }
        }

        private void Write(string text)
        {
            if (output.Length + text.Length > TemplateLimits.MaximumOutputCharacters)
            {
                throw new DatasourceChartException(DatasourceMessages.TemplateTooLarge,
                    TemplateLimits.MaximumOutputCharacters);
            }

            output.Append(text);
        }

        private void Nodes(IReadOnlyList<TemplateNode> nodes, TemplateScope scope)
        {
            var satisfied = false;

            foreach (var node in nodes)
            {
                Step();

                switch (node)
                {
                    case TemplateTextNode text:
                        Write(WebUtility.HtmlEncode(text.Text));

                        break;
                    case TemplateInterpolationNode interpolation:
                        Write(WebUtility.HtmlEncode(
                            TemplateValues.Display(TemplateEvaluator.Evaluate(interpolation.Expression, scope))));

                        break;
                    case TemplateElementNode element:
                        if (element.Branch is TemplateBranch.None or TemplateBranch.If)
                        {
                            satisfied = false;
                        }

                        if (!Passes(element, scope, ref satisfied))
                        {
                            break;
                        }

                        Loop(element, scope);

                        break;
                }
            }
        }

        private static bool Passes(TemplateElementNode element, TemplateScope scope, ref bool satisfied)
        {
            switch (element.Branch)
            {
                case TemplateBranch.None:
                    return true;
                case TemplateBranch.Else:
                    var chosen = !satisfied;

                    satisfied = true;

                    return chosen;
                default:
                    if (satisfied)
                    {
                        return false;
                    }

                    satisfied = TemplateValues.Truthy(TemplateEvaluator.Evaluate(element.Condition!, scope));

                    return satisfied;
            }
        }

        private void Loop(TemplateElementNode element, TemplateScope scope)
        {
            if (element.Loop == null)
            {
                Element(element, scope);

                return;
            }

            if (TemplateEvaluator.Evaluate(element.Loop.Source, scope) is not IList items)
            {
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                Step();

                var inner = scope.With(element.Loop.ItemName, items[i]);

                if (element.Loop.IndexName != null)
                {
                    inner = inner.With(element.Loop.IndexName, i);
                }

                Element(element, inner);
            }
        }

        private void Element(TemplateElementNode element, TemplateScope scope)
        {
            if (element.Tag == "template")
            {
                Nodes(element.Children, scope);

                return;
            }

            var attributes = new List<(string Name, string Value)>();

            foreach (var attribute in element.Attributes)
            {
                var value = attribute.Parts != null
                    ? string.Concat(attribute.Parts.Select(p => p.Text ?? TemplateValues.Display(
                        TemplateEvaluator.Evaluate(p.Expression!, scope))))
                    : attribute.Bound == null
                        ? attribute.Static
                        : AttributeValue(TemplateEvaluator.Evaluate(attribute.Bound, scope));

                if (value != null)
                {
                    attributes.Add((attribute.Name, value));
                }
            }

            string? textareaValue = null;

            if (form != null)
            {
                textareaValue = Field(element, attributes);
            }

            var builder = new StringBuilder("<").Append(element.Tag);

            foreach (var (name, value) in attributes)
            {
                builder.Append(' ').Append(name);

                if (value.Length > 0 || name is not ("checked" or "selected" or "disabled"))
                {
                    builder.Append("=\"").Append(WebUtility.HtmlEncode(value)).Append('"');
                }
            }

            Write(builder.Append('>').ToString());

            if (TemplateVocabulary.VoidTags.Contains(element.Tag))
            {
                return;
            }

            if (textareaValue != null)
            {
                Write(WebUtility.HtmlEncode(textareaValue));
            }
            else if (element.Tag == "select" && form != null)
            {
                var previous = form.SelectName;

                form.SelectName = attributes.FirstOrDefault(a => a.Name == "name").Value;
                Nodes(element.Children, scope);
                form.SelectName = previous;
            }
            else
            {
                Nodes(element.Children, scope);
            }

            Write("</" + element.Tag + ">");
        }

        private string? Field(TemplateElementNode element, List<(string Name, string Value)> attributes)
        {
            var state = form!;
            var name = attributes.FirstOrDefault(a => a.Name == "name").Value;

            switch (element.Tag)
            {
                case "input":
                    var type = attributes.FirstOrDefault(a => a.Name == "type").Value ?? "text";

                    if (!state.Visible.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        state.Visible.Add(name);
                    }

                    state.Values.TryGetValue(name, out var current);

                    if (type == "checkbox")
                    {
                        if (string.Equals(current, "true", StringComparison.OrdinalIgnoreCase))
                        {
                            attributes.Add(("checked", string.Empty));
                        }
                    }
                    else if (type == "radio")
                    {
                        if (current != null && current == attributes.FirstOrDefault(a => a.Name == "value").Value)
                        {
                            attributes.Add(("checked", string.Empty));
                        }
                    }
                    else if (current != null)
                    {
                        attributes.RemoveAll(a => a.Name == "value");
                        attributes.Add(("value", current));
                    }

                    break;
                case "select":
                    state.Visible.Add(name);

                    break;
                case "textarea":
                    state.Visible.Add(name);
                    state.Values.TryGetValue(name, out var text);

                    if (state.ReadOnly)
                    {
                        attributes.Add(("disabled", string.Empty));
                    }

                    return text ?? string.Empty;
                case "option":
                    if (state.SelectName != null && state.Values.TryGetValue(state.SelectName, out var chosen) &&
                        chosen == attributes.FirstOrDefault(a => a.Name == "value").Value)
                    {
                        attributes.Add(("selected", string.Empty));
                    }

                    return null;
                default:
                    return null;
            }

            if (state.ReadOnly)
            {
                attributes.Add(("disabled", string.Empty));
            }

            return null;
        }

        private static string? AttributeValue(object? value)
        {
            return value is null or false ? null : TemplateValues.Display(value);
        }
    }
}
