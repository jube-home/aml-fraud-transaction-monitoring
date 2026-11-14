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


using System.Text.RegularExpressions;
using Jube.Data.Reporting;
using Jube.Service.Charts.Datasource.Template.Models;

namespace Jube.Service.Charts.Datasource.Template
{
    public sealed class FormCompiler
    {
        private static readonly Regex NamePattern = new("^[A-Za-z][A-Za-z0-9_]{0,63}$", RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        private readonly Dictionary<string, FormField> fields = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> radios = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<(string Name, int Line, int Column)> labels = [];
        private readonly List<string> order = [];

        private FormCompiler()
        {
        }

        public static FormSchema Compile(IReadOnlyList<TemplateNode> nodes)
        {
            var compiler = new FormCompiler();

            compiler.Walk(nodes, false);

            foreach (var (name, line, column) in compiler.labels.Where(l => !compiler.fields.ContainsKey(l.Name)))
            {
                throw new DatasourceChartException(DatasourceMessages.FormLabelTargetUnknown, line, column, name);
            }

            return new FormSchema(compiler.order.Select(n =>
            {
                var field = compiler.fields[n];

                return compiler.radios.TryGetValue(n, out var options) ? field with { Options = options } : field;
            }).ToList());
        }

        private void Walk(IReadOnlyList<TemplateNode> nodes, bool inLoop)
        {
            foreach (var element in nodes.OfType<TemplateElementNode>())
            {
                var looped = inLoop || element.Loop != null;

                switch (element.Tag)
                {
                    case "input":
                        Input(element, looped);

                        break;
                    case "textarea":
                        RefuseLoop(element, looped);
                        Add(element, Name(element), FormFieldKind.TextArea, []);

                        break;
                    case "select":
                        Select(element, looped);

                        break;
                    case "label":
                        Label(element);
                        Walk(element.Children, looped);

                        break;
                    default:
                        Walk(element.Children, looped);

                        break;
                }
            }
        }

        private static string? Static(TemplateElementNode element, string attribute)
        {
            var found = element.Attributes.FirstOrDefault(a => a.Name == attribute);

            return found is { Bound: null, Parts: null } ? found.Static : null;
        }

        private static void RefuseLoop(TemplateElementNode element, bool inLoop)
        {
            if (inLoop)
            {
                throw new DatasourceChartException(DatasourceMessages.FormFieldInLoop, element.Line, element.Column,
                    element.Tag);
            }
        }

        private static string Name(TemplateElementNode element)
        {
            var name = Static(element, "name");

            if (string.IsNullOrEmpty(name))
            {
                throw new DatasourceChartException(DatasourceMessages.FormFieldNameMissing, element.Line,
                    element.Column, element.Tag);
            }

            if (!NamePattern.IsMatch(name))
            {
                throw new DatasourceChartException(DatasourceMessages.FormFieldNameInvalid, element.Line,
                    element.Column, name);
            }

            return name;
        }

        private void Add(TemplateElementNode element, string name, FormFieldKind kind, IReadOnlyList<string> options)
        {
            if (fields.ContainsKey(name))
            {
                throw new DatasourceChartException(DatasourceMessages.FormFieldNameDuplicate, element.Line,
                    element.Column, name);
            }

            fields[name] = new FormField(name, kind, options, element.Line, element.Column);
            order.Add(name);
        }

        private void Input(TemplateElementNode element, bool inLoop)
        {
            RefuseLoop(element, inLoop);

            var name = Name(element);
            var type = Static(element, "type") ?? "text";

            if (!TemplateVocabulary.InputTypes.Contains(type))
            {
                throw new DatasourceChartException(DatasourceMessages.FormInputTypeInvalid, element.Line,
                    element.Column, type);
            }

            if (type == "radio")
            {
                var value = Static(element, "value");

                if (string.IsNullOrEmpty(value))
                {
                    throw new DatasourceChartException(DatasourceMessages.FormOptionsFixed, element.Line,
                        element.Column, name);
                }

                if (fields.TryGetValue(name, out var existing) && existing.Kind != FormFieldKind.Radio)
                {
                    throw new DatasourceChartException(DatasourceMessages.FormFieldNameDuplicate, element.Line,
                        element.Column, name);
                }

                if (existing == null)
                {
                    Add(element, name, FormFieldKind.Radio, []);
                    radios[name] = [];
                }

                radios[name].Add(value);

                return;
            }

            Add(element, name, type switch
            {
                "number" => FormFieldKind.Number,
                "date" => FormFieldKind.Date,
                "datetime-local" => FormFieldKind.DateTime,
                "email" => FormFieldKind.Email,
                "tel" => FormFieldKind.Telephone,
                "checkbox" => FormFieldKind.Checkbox,
                _ => FormFieldKind.Text
            }, []);
        }

        private void Select(TemplateElementNode element, bool inLoop)
        {
            RefuseLoop(element, inLoop);

            var name = Name(element);
            var options = new List<string>();

            Options(element, name, options);

            if (options.Count == 0)
            {
                throw new DatasourceChartException(DatasourceMessages.FormOptionsFixed, element.Line, element.Column,
                    name);
            }

            Add(element, name, FormFieldKind.Select, options);
        }

        private static void Options(TemplateElementNode parent, string name, List<string> options)
        {
            foreach (var child in parent.Children.OfType<TemplateElementNode>())
            {
                if (child.Loop != null)
                {
                    throw new DatasourceChartException(DatasourceMessages.FormOptionsFixed, child.Line, child.Column,
                        name);
                }

                if (child.Tag == "option")
                {
                    var value = Static(child, "value");

                    if (value == null)
                    {
                        throw new DatasourceChartException(DatasourceMessages.FormOptionsFixed, child.Line,
                            child.Column, name);
                    }

                    options.Add(value);
                }
                else
                {
                    Options(child, name, options);
                }
            }
        }

        private void Label(TemplateElementNode element)
        {
            var target = Static(element, "for");

            if (target != null)
            {
                labels.Add((target, element.Line, element.Column));
            }
        }
    }
}
