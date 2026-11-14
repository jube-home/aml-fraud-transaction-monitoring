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

using Jube.Dto.Query.FormModel;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.FormModel
{
    public static class FormModelLocaliser
    {
        public static FormModelDto Localise(FormModelDto model, IStringLocalizer strings)
        {
            var area = model.Area;
            var copy = model.Copy();

            copy.Groups =
            [
                .. model.Groups.Select(group =>
                {
                    var translated = group.Copy();

                    translated.Label = Pick(strings, FormModelKeys.Group(area, group.Path), group.Label);

                    return translated;
                })
            ];

            copy.Fields =
            [
                .. model.Fields.Select(field =>
                {
                    var translated = field.Copy();

                    translated.Label = Pick(strings, FormModelKeys.Field(area, field.Name), field.Label);
                    translated.Description = string.IsNullOrWhiteSpace(field.Description)
                        ? field.Description
                        : Pick(strings, FormModelKeys.Description(area, field.Name), field.Description);
                    translated.Help = field.Help == null
                        ? null
                        : Pick(strings, FormModelKeys.Help(area, field.Name), field.Help);
                    translated.Placeholder = field.Placeholder == null
                        ? null
                        : Pick(strings, FormModelKeys.Placeholder(area, field.Name), field.Placeholder);
                    translated.Options =
                    [
                        .. field.Options.Select(option =>
                        {
                            var choice = option.Copy();

                            choice.Label = Pick(strings, FormModelKeys.Option(option.Key), option.Label);

                            return choice;
                        })
                    ];

                    return translated;
                })
            ];

            copy.ListColumns =
            [
                .. model.ListColumns.Select(column =>
                {
                    var translated = column.Copy();

                    translated.Title = Pick(strings, FormModelKeys.Column(area, column.Field), column.Title);

                    return translated;
                })
            ];

            copy.Children =
            [
                .. model.Children.Select(child =>
                {
                    var translated = child.Copy();

                    translated.Label = Pick(strings, FormModelKeys.Child(area, child.Area), child.Label);
                    translated.Hub = string.IsNullOrEmpty(child.Hub)
                        ? child.Hub
                        : Pick(strings, FormModelKeys.Hub(area, child.Hub), child.Hub);

                    return translated;
                })
            ];

            return copy;
        }

        private static string Pick(IStringLocalizer strings, string key, string fallback)
        {
            var found = strings[key];

            return found.ResourceNotFound || string.IsNullOrWhiteSpace(found.Value) ? fallback : found.Value;
        }
    }
}
