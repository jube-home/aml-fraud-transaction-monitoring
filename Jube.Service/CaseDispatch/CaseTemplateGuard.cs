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

using FluentValidation.Results;
using Jube.Service.Charts.Datasource;
using Jube.Service.Charts.Datasource.Template;
using Jube.Service.Charts.Datasource.Template.Models;
using Microsoft.Extensions.Localization;

namespace Jube.Service.CaseDispatch
{
    public sealed class CaseTemplateGuard(IStringLocalizer chartStrings)
    {
        private const string Property = "Html";

        private readonly DatasourceChartText chartText = new(chartStrings);

        public void Append(ValidationResult results, string? html, string? sql)
        {
            if (string.IsNullOrWhiteSpace(html) || results.Errors.Any(e => e.PropertyName == Property))
            {
                return;
            }

            try
            {
                DatasourceTemplate.CompileForm(html,
                    string.IsNullOrWhiteSpace(sql) ? TemplateDialect.Form : TemplateDialect.FormSql);
            }
            catch (DatasourceChartException ex)
            {
                results.Errors.Add(new ValidationFailure(Property, chartText.Describe(ex))
                {
                    ErrorCode = "TemplateInvalid"
                });
            }
        }
    }
}
