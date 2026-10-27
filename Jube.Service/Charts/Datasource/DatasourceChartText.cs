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
using System.Reflection;
using System.ComponentModel;
using Jube.Data.Repository.Exceptions;
using Jube.Dto.Options;
using Microsoft.Extensions.Localization;
using DataMessages = Jube.Data.Reporting.DatasourceMessages;

namespace Jube.Service.Charts.Datasource
{
    public sealed class DatasourceChartText(IStringLocalizer? strings = null)
    {
        public static DatasourceChartText English { get; } = new();

        public static string RoleDescriptionEnglish(DatasourceChartRole role)
        {
            return typeof(DatasourceChartRole).GetField(role.ToString())?
                .GetCustomAttribute<DescriptionAttribute>()?.Description ?? role.ToString();
        }

        public static MessageTerm KindTerm(DatasourceChartKind kind)
        {
            return new MessageTerm($"Kind.{kind.Kind}.Name", kind.Name);
        }

        public static MessageTerm RoleTerm(DatasourceChartRole role)
        {
            return new MessageTerm($"Role.{role}.Name", role.ToString());
        }

        public static MessageTerm NeedsTerm(DatasourceRoleAccepts accepts)
        {
            return accepts switch
            {
                DatasourceRoleAccepts.Numeric => DataMessages.NeedsNumeric,
                DatasourceRoleAccepts.Time => DataMessages.NeedsTime,
                DatasourceRoleAccepts.NumericOrTime => DataMessages.NeedsNumericOrTime,
                _ => DataMessages.NeedsAny
            };
        }

        public static MessageTerm NounTerm(DatasourceRoleAccepts accepts)
        {
            return accepts switch
            {
                DatasourceRoleAccepts.Numeric => DataMessages.NounNumeric,
                DatasourceRoleAccepts.Time => DataMessages.NounTime,
                DatasourceRoleAccepts.NumericOrTime => DataMessages.NounNumericOrTime,
                _ => DataMessages.NounAny
            };
        }

        public string Pick(string key, string english)
        {
            if (strings == null)
            {
                return english;
            }

            var found = strings[key];

            return found.ResourceNotFound || string.IsNullOrWhiteSpace(found.Value) ? english : found.Value;
        }

        public string Term(MessageTerm term)
        {
            return Pick(term.Key, term.English);
        }

        public string KindName(DatasourceChartKind kind)
        {
            return Pick($"Kind.{kind.Kind}.Name", kind.Name);
        }

        public string KindDescription(DatasourceChartKind kind)
        {
            return Pick($"Kind.{kind.Kind}.Description", kind.Description);
        }

        public string RoleName(DatasourceChartRole role)
        {
            return Pick($"Role.{role}.Name", role.ToString());
        }

        public string RoleDescription(DatasourceChartRole role)
        {
            return Pick($"Role.{role}.Description", RoleDescriptionEnglish(role));
        }

        public string Message(string code, IReadOnlyList<object> arguments)
        {
            var template = Pick($"Message.{code}", DataMessages.English[code]);

            return string.Format(CultureInfo.CurrentUICulture, template,
                [.. arguments.Select(a => a is MessageTerm term ? Term(term) : Convert.ToString(a, CultureInfo.InvariantCulture) ?? string.Empty)]);
        }

        public string Message(string code, params object[] arguments)
        {
            return Message(code, (IReadOnlyList<object>)arguments);
        }

        public string Describe(Exception exception)
        {
            return exception switch
            {
                DatasourceChartException { Problems.Count: > 0 } chart =>
                    string.Join(" ", chart.Problems.Select(p => Message(p.Code, p.Arguments))),
                SqlValidationFailedException { Code: not null } sql => Message(sql.Code, sql.Arguments),
                ParameterValueInvalidException { Code: not null } parameter =>
                    Message(parameter.Code, parameter.Arguments),
                _ => exception.Message
            };
        }
    }
}
