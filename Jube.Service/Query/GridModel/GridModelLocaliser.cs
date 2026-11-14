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

using Jube.Dto.Query.GridModel;
using Jube.Service.Query.FormModel;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.GridModel
{
    public static class GridModelLocaliser
    {
        public static GridModelDto Localise(GridModelDto model, IStringLocalizer strings)
        {
            var area = model.Area;
            var copy = model.Copy();

            copy.Title = Pick(strings, FormModelKeys.Title(area), model.Title);
            copy.Columns =
            [
                .. model.Columns.Select(column =>
                {
                    var translated = column.Copy();

                    translated.Title = Pick(strings, FormModelKeys.Column(area, column.Field), column.Title);
                    translated.Description = column.Description.Length == 0
                        ? column.Description
                        : Pick(strings, FormModelKeys.Description(area, column.Field), column.Description);

                    return translated;
                })
            ];
            copy.Filters = [.. model.Filters.Select(filter => LocaliseFilter(area, filter, strings))];
            copy.Search = model.Search == null ? null : LocaliseFilter(area, model.Search, strings);

            return copy;
        }

        private static GridFilterDto LocaliseFilter(string area, GridFilterDto filter, IStringLocalizer strings)
        {
            var translated = filter.Copy();

            translated.Label = Pick(strings, FormModelKeys.Filter(area, filter.Param), filter.Label);
            translated.Options =
            [
                .. filter.Options.Select(option =>
                {
                    var choice = option.Copy();

                    choice.Label = Pick(strings, FormModelKeys.Option(option.Key), option.Label);

                    return choice;
                })
            ];

            return translated;
        }

        private static string Pick(IStringLocalizer strings, string key, string fallback)
        {
            var found = strings[key];

            return found.ResourceNotFound || string.IsNullOrWhiteSpace(found.Value) ? fallback : found.Value;
        }
    }
}
