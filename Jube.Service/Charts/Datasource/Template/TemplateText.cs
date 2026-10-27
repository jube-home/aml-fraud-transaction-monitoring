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
using Jube.Data.Reporting;
using Jube.Service.Charts.Datasource.Template.Models;

namespace Jube.Service.Charts.Datasource.Template
{
    public static class TemplateText
    {
        public const int MaximumLength = 20000;
        private const int MaximumTokens = 500;
        private const int MaximumOutput = 200000;

        public static void Validate(string? text, TemplateDialect dialect = TemplateDialect.FormSql)
        {
            Parse(text, dialect);
        }

        public static string Render(string? text, IReadOnlyDictionary<string, object?> roots,
            Func<string, string> encode, TemplateDialect dialect = TemplateDialect.FormSql)
        {
            var segments = Parse(text, dialect);
            var scope = roots.Aggregate(TemplateScope.Empty, (current, root) => current.With(root.Key, root.Value));
            var output = new System.Text.StringBuilder();

            foreach (var segment in segments)
            {
                output.Append(segment.Expression == null
                    ? segment.Text
                    : encode(TemplateValues.Display(TemplateEvaluator.Evaluate(segment.Expression, scope))));

                if (output.Length > MaximumOutput)
                {
                    throw new DatasourceChartException(DatasourceMessages.TemplateTooLarge, MaximumOutput);
                }
            }

            return output.ToString();
        }

        public static string Plain(string value)
        {
            return value;
        }

        public static string Html(string value)
        {
            return WebUtility.HtmlEncode(value);
        }

        public static string Url(string value)
        {
            return Uri.EscapeDataString(value);
        }

        private static List<TemplateAttributePart> Parse(string? text, TemplateDialect dialect)
        {
            var parts = new List<TemplateAttributePart>();

            if (string.IsNullOrEmpty(text))
            {
                return parts;
            }

            if (text.Length > MaximumLength)
            {
                throw new DatasourceChartException(DatasourceMessages.TemplateTooLong, MaximumLength);
            }

            var names = TemplateVocabulary.RootsFor(dialect);
            var cursor = 0;
            var tokens = 0;

            while (cursor < text.Length)
            {
                var open = text.IndexOf("[@", cursor, StringComparison.Ordinal);

                if (open < 0)
                {
                    parts.Add(new TemplateAttributePart(text[cursor..], null));

                    break;
                }

                if (open > cursor)
                {
                    parts.Add(new TemplateAttributePart(text[cursor..open], null));
                }

                var (line, column) = Where(text, open);
                var close = text.IndexOf("@]", open + 2, StringComparison.Ordinal);

                if (close < 0)
                {
                    throw new DatasourceChartException(DatasourceMessages.TemplateInterpolationOpen, line, column);
                }

                if (++tokens > MaximumTokens)
                {
                    throw new DatasourceChartException(DatasourceMessages.TemplateTooManySteps, MaximumTokens);
                }

                parts.Add(new TemplateAttributePart(null,
                    TemplateExpressionParser.Parse(text[(open + 2)..close], line, column, names)));
                cursor = close + 2;
            }

            return parts;
        }

        private static (int Line, int Column) Where(string text, int at)
        {
            var line = 1;
            var lineStart = 0;

            for (var i = 0; i < at; i++)
            {
                if (text[i] != '\n')
                {
                    continue;
                }

                line++;
                lineStart = i + 1;
            }

            return (line, at - lineStart + 1);
        }
    }
}
