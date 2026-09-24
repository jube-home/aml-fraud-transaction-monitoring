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
using Jube.Data.QueryBuilder;
using Jube.Dto.Filter;
using Jube.Dto.Validation;

namespace Jube.Service.Agent
{
    public static class DtoFilter
    {
        public const int MaximumTake = 200;
        public const int MaximumGroups = 200;

        public static List<FilterFieldDto> Fields<T>()
        {
            return BuilderFilter.Fields(typeof(T)).Select(f => new FilterFieldDto
            {
                Name = f.Id,
                DataType = f.Type.ToString(),
                Operators = BuilderProfile.Operators[f.Type].ToList(),
                Description = f.Description
            }).ToList();
        }

        public static FilterResultDto<T> Filter<T>(IEnumerable<T> rows, string? builderJson, int take, int? afterId,
            Func<T, int> id)
        {
            ArgumentNullException.ThrowIfNull(rows);
            ArgumentNullException.ThrowIfNull(id);

            if (!TryPredicate<T>(builderJson, out var predicate, out var errors))
            {
                return new FilterResultDto<T> { Errors = errors };
            }

            var clampedTake = Math.Clamp(take, 1, MaximumTake);
            var matched = rows
                .Where(r => !afterId.HasValue || id(r) > afterId.Value)
                .OrderBy(id)
                .Where(predicate)
                .Take(clampedTake + 1)
                .ToList();

            return new FilterResultDto<T>
            {
                Valid = true,
                More = matched.Count > clampedTake,
                Items = matched.Take(clampedTake).ToList()
            };
        }

        public static FilterCountResultDto Count<T>(IEnumerable<T> rows, string? builderJson, string? groupBy)
        {
            ArgumentNullException.ThrowIfNull(rows);

            if (!TryPredicate<T>(builderJson, out var predicate, out var errors))
            {
                return new FilterCountResultDto { Errors = errors };
            }

            FilterField? group = null;
            if (!string.IsNullOrWhiteSpace(groupBy))
            {
                group = BuilderFilter.Fields(typeof(T)).FirstOrDefault(f => f.Id == groupBy);
                if (group == null)
                {
                    return new FilterCountResultDto
                    {
                        Errors =
                        [
                            new ValidationErrorDto
                            {
                                PropertyName = "groupBy", ErrorCode = "FieldUnknown",
                                Message = $"'{groupBy}' is not a field that can be used here; list the fields to see " +
                                          "which can."
                            }
                        ]
                    };
                }
            }

            var matched = rows.Where(predicate).ToList();
            var result = new FilterCountResultDto { Valid = true, Count = matched.Count };
            if (group == null)
            {
                return result;
            }

            var groups = matched
                .GroupBy(r => Format(group.Property.GetValue(r)))
                .Select(g => new FilterCountGroupDto { Value = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.Value, StringComparer.Ordinal)
                .ToList();

            result.GroupsTruncated = groups.Count > MaximumGroups;
            result.Groups = groups.Take(MaximumGroups).ToList();
            return result;
        }

        private static bool TryPredicate<T>(string? builderJson, out Func<T, bool> predicate,
            out List<ValidationErrorDto> errors)
        {
            errors = [];
            if (string.IsNullOrWhiteSpace(builderJson))
            {
                predicate = _ => true;
                return true;
            }

            var parsed = BuilderProfile.Parse(builderJson, BuilderFilter.Catalogue(typeof(T)));
            if (!parsed.Valid)
            {
                predicate = _ => false;
                errors = parsed.Errors.Select(e => new ValidationErrorDto
                    { PropertyName = e.Path, ErrorCode = e.Code, Message = e.Message }).ToList();
                return false;
            }

            predicate = BuilderFilter.Compile<T>(parsed.Group);
            return true;
        }

        private static string? Format(object? value)
        {
            return value switch
            {
                null => null,
                string text => text,
                bool flag => flag ? "True" : "False",
                DateTime date => date.ToString("o", CultureInfo.InvariantCulture),
                DateTimeOffset offset => offset.ToString("o", CultureInfo.InvariantCulture),
                Guid guid => guid.ToString("D"),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }
    }
}