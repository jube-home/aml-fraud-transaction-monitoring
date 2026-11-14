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
    public static class DatasourceTemplate
    {
        public static void Validate(string? text)
        {
            TemplateParser.Parse(text);
        }

        public static FormSchema CompileForm(string? text, TemplateDialect dialect = TemplateDialect.Form)
        {
            return FormCompiler.Compile(TemplateParser.Parse(text, dialect));
        }

        public static FormSubmissionResult ValidateSubmission(string? text, IReadOnlyDictionary<string, object?> roots,
            IReadOnlyDictionary<string, string?> submitted, TemplateDialect dialect = TemplateDialect.Form)
        {
            var schema = CompileForm(text, dialect);
            var render = RenderForm(text, roots, submitted, false, dialect);

            return FormSubmissionValidator.Validate(schema, render.VisibleFields, submitted);
        }

        public static FormRender RenderForm(string? text, IReadOnlyDictionary<string, object?> roots,
            IReadOnlyDictionary<string, string?> values, bool readOnly, TemplateDialect dialect = TemplateDialect.Form)
        {
            var nodes = TemplateParser.Parse(text, dialect);

            FormCompiler.Compile(nodes);

            return TemplateRenderer.RenderForm(nodes, roots, values, readOnly);
        }

        public static string Render(string? text, DatasourceFrame frame, bool truncated)
        {
            IReadOnlyList<TemplateNode> nodes = TemplateParser.Parse(text);

            return TemplateRenderer.Render(nodes, frame, truncated);
        }
    }
}
