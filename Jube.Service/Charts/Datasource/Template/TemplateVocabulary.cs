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


using Jube.Service.Charts.Datasource.Template.Models;

namespace Jube.Service.Charts.Datasource.Template
{
    public static class TemplateVocabulary
    {
        public static readonly IReadOnlySet<string> Tags = new HashSet<string>(StringComparer.Ordinal)
        {
            "div", "span", "p", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li", "strong", "em", "b", "i",
            "small", "br", "hr", "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "section",
            "article", "header", "footer", "dl", "dt", "dd", "time", "code", "pre", "abbr", "mark", "sub", "sup",
            "template"
        };

        public static readonly IReadOnlySet<string> VoidTags = new HashSet<string>(StringComparer.Ordinal)
        {
            "br", "hr", "input"
        };

        public static readonly IReadOnlySet<string> FormTags = new HashSet<string>(StringComparer.Ordinal)
        {
            "form", "fieldset", "legend", "label", "input", "select", "option", "textarea"
        };

        public static readonly IReadOnlySet<string> FormRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Form", "Case", "Payload", "Abstraction", "Dictionary", "TtlCounter", "Sanction", "HttpAdaptation",
            "ExhaustiveAdaptation", "AdvancedAnalyticsAdaptation", "Activation"
        };

        public static readonly IReadOnlySet<string> FormSqlRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Form", "rows", "columns", "rowCount", "truncated"
        };

        public static readonly IReadOnlySet<string> TextRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Form", "rows", "columns", "rowCount", "truncated"
        };

        public static readonly IReadOnlySet<string> InputTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "text", "number", "date", "datetime-local", "email", "tel", "checkbox", "radio"
        };

        private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> FormAttributes =
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                ["input"] = new HashSet<string>(StringComparer.Ordinal) { "name", "type", "placeholder", "value" },
                ["select"] = new HashSet<string>(StringComparer.Ordinal) { "name" },
                ["option"] = new HashSet<string>(StringComparer.Ordinal) { "value" },
                ["textarea"] = new HashSet<string>(StringComparer.Ordinal) { "name", "placeholder", "rows" },
                ["label"] = new HashSet<string>(StringComparer.Ordinal) { "for" }
            };

        public static readonly IReadOnlySet<string> Attributes = new HashSet<string>(StringComparer.Ordinal)
        {
            "class", "title", "role", "scope", "colspan", "rowspan", "datetime", "lang", "dir"
        };

        public static readonly IReadOnlySet<string> Roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "rows", "columns", "rowCount", "truncated"
        };

        public static bool AttributeAllowed(string name)
        {
            return Attributes.Contains(name) || IsPrefixed(name, "aria-") || IsPrefixed(name, "data-");
        }

        public static bool TagAllowed(TemplateDialect dialect, string tag)
        {
            return Tags.Contains(tag) || dialect != TemplateDialect.Datasource && FormTags.Contains(tag);
        }

        public static IReadOnlySet<string> RootsFor(TemplateDialect dialect)
        {
            return dialect switch
            {
                TemplateDialect.Form => FormRoots,
                TemplateDialect.FormSql => FormSqlRoots,
                _ => Roots
            };
        }

        public static bool AttributeAllowed(TemplateDialect dialect, string tag, string name)
        {
            return AttributeAllowed(name) || dialect != TemplateDialect.Datasource &&
                FormAttributes.TryGetValue(tag, out var allowed) && allowed.Contains(name);
        }

        private static bool IsPrefixed(string name, string prefix)
        {
            return name.Length > prefix.Length && name.StartsWith(prefix, StringComparison.Ordinal)
                                               && name.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
        }
    }
}
