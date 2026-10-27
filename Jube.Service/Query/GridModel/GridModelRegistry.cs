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

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Jube.Dto.Forms;
using Jube.Dto.Options;
using Jube.Dto.Query.FormModel;
using Jube.Dto.Query.GridModel;
using Jube.Service.Query.FormModel;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.GridModel
{
    public sealed class GridModelRegistry
    {
        private readonly ConcurrentDictionary<(string Area, string Culture), GridModelDto> localised = new();
        private readonly Dictionary<string, GridModelDto> models;

        private GridModelRegistry(Dictionary<string, GridModelDto> models, List<string> failures)
        {
            this.models = models;
            Failures = failures;
        }

        public IReadOnlyCollection<string> Areas => models.Keys;

        public IReadOnlyList<string> Failures { get; }

        public static GridModelRegistry Build(Assembly assembly)
        {
            var built = new Dictionary<string, GridModelDto>(StringComparer.OrdinalIgnoreCase);
            var failures = new List<string>();

            foreach (var type in assembly.GetTypes())
            {
                var endpoint = type.GetCustomAttribute<GridEndpointAttribute>();

                if (endpoint != null)
                {
                    built[endpoint.Area] = Describe(type, endpoint, failures);
                }
            }

            return new GridModelRegistry(built, failures);
        }

        public bool TryGet(string area, out GridModelDto? model)
        {
            return models.TryGetValue(area, out model);
        }

        public bool TryGetLocalised(string area, string culture, IStringLocalizer strings, out GridModelDto? model)
        {
            if (!models.TryGetValue(area, out var english))
            {
                model = null;

                return false;
            }

            model = localised.GetOrAdd((english.Area, culture), _ => GridModelLocaliser.Localise(english, strings));

            return true;
        }

        private static GridModelDto Describe(Type type, GridEndpointAttribute endpoint, List<string> failures)
        {
            var area = endpoint.Area;
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.GetIndexParameters().Length == 0)
                .ToList();

            var columns = new List<GridColumnDto>();
            var filters = new List<GridFilterDto>();
            string? timeField = null;
            var window = 60;

            for (var index = 0; index < properties.Count; index++)
            {
                var property = properties[index];
                var list = property.GetCustomAttribute<ListColumnAttribute>();
                var clientType = FormModelRegistry.ClientType(property.PropertyType);
                var field = FormModelRegistry.CamelCase(property.Name);

                columns.Add(new GridColumnDto
                {
                    Field = field,
                    Title = list?.Title ?? FormModelRegistry.FieldLabel(property.Name),
                    Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
                    Type = clientType,
                    Format = FormatName(list?.Format ?? GridFormat.Default, clientType),
                    Unit = list?.Unit,
                    Order = list is { Order: > 0 } ? list.Order : (index + 1) * 10,
                    Sortable = list?.Sortable ?? true,
                    Hidden = list?.Hidden ?? false
                });

                var filter = property.GetCustomAttribute<GridFilterAttribute>();

                if (filter != null)
                {
                    var options = FormModelRegistry.Choices(property, failures);

                    filters.Add(new GridFilterDto
                    {
                        Param = filter.Param ?? field,
                        Label = filter.Label ?? FormModelRegistry.FieldLabel(property.Name),
                        Kind = options.Count > 0 ? "select" : "text",
                        Options = options
                    });
                }

                var range = property.GetCustomAttribute<GridTimeRangeAttribute>();

                if (range != null)
                {
                    if (clientType != "datetime")
                    {
                        failures.Add($"{area}: {property.Name} carries a time range but is not a date");
                    }

                    timeField = field;
                    window = range.DefaultWindowMinutes;
                }
            }

            foreach (var declared in type.GetCustomAttributes<GridFilterAttribute>())
            {
                if (string.IsNullOrEmpty(declared.Param))
                {
                    failures.Add($"{area}: a filter declared on the type has no Param");

                    continue;
                }

                var choices = new List<FormOptionDto>();

                foreach (var choice in declared.Choices)
                {
                    var split = choice.IndexOf('=');

                    if (split <= 0 || split == choice.Length - 1 || !long.TryParse(choice[..split], out var value))
                    {
                        failures.Add($"{area}: filter {declared.Param} choice '{choice}' is not of the form number=Label");

                        continue;
                    }

                    choices.Add(new FormOptionDto
                    {
                        Key = $"{area}.{declared.Param}.{value}",
                        Value = value,
                        Label = choice[(split + 1)..]
                    });
                }

                filters.Add(new GridFilterDto
                {
                    Param = declared.Param,
                    Label = declared.Label ?? FormModelRegistry.FieldLabel(declared.Param),
                    Kind = choices.Count > 0 ? "select" : declared.Lookup != null ? "lookup" : "text",
                    Options = choices,
                    Lookup = declared.Lookup
                });
            }

            foreach (var ratio in type.GetCustomAttributes<GridRatioAttribute>())
            {
                var numerator = FormModelRegistry.CamelCase(ratio.Numerator);
                var denominator = FormModelRegistry.CamelCase(ratio.Denominator);

                if (columns.All(column => column.Field != numerator) ||
                    columns.All(column => column.Field != denominator))
                {
                    failures.Add($"{area}: ratio {ratio.Name} names {ratio.Numerator} or {ratio.Denominator}, which is not a column");

                    continue;
                }

                columns.Add(new GridColumnDto
                {
                    Field = ratio.Name,
                    Title = ratio.Title ?? FormModelRegistry.FieldLabel(ratio.Name),
                    Type = "number",
                    Format = FormatName(ratio.Format, "number"),
                    Unit = ratio.Unit,
                    Order = ratio.Order > 0 ? ratio.Order : (properties.Count + 1) * 10,
                    Numerator = numerator,
                    Denominator = denominator
                });
            }

            var search = type.GetCustomAttribute<GridSearchAttribute>();
            var sort = endpoint.DefaultSort;

            if (sort.Length > 0 && columns.All(column => column.Field != sort))
            {
                failures.Add($"{area}: the default sort {sort} is not a column");
            }

            if (endpoint.Snapshot && timeField != null)
            {
                failures.Add($"{area}: a snapshot grid has no time window");
            }

            return new GridModelDto
            {
                Area = area,
                Endpoint = endpoint.Endpoint ?? $"/api/{area}",
                Title = endpoint.Title ?? FormModelRegistry.FieldLabel(area),
                Snapshot = endpoint.Snapshot,
                SortField = sort,
                SortDirection = endpoint.DefaultSortDirection,
                Take = endpoint.Take,
                TimeField = timeField,
                WindowMinutes = window,
                Search = search == null
                    ? null
                    : new GridFilterDto { Param = search.Param, Label = search.Label ?? "Search", Kind = "text" },
                Filters = filters,
                Columns = [.. columns.OrderBy(column => column.Order).ThenBy(column => column.Field, StringComparer.Ordinal)]
            };
        }

        private static string FormatName(GridFormat format, string clientType)
        {
            if (format != GridFormat.Default)
            {
                return char.ToLowerInvariant(format.ToString()[0]) + format.ToString()[1..];
            }

            return clientType switch
            {
                "datetime" => "dateTime",
                "integer" => "integer",
                "number" => "decimal",
                "guid" => "code",
                _ => "default"
            };
        }
    }
}
