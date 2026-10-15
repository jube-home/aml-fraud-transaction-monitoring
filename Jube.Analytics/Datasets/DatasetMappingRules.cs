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
using Jube.Analytics.Datasets.Models;

namespace Jube.Analytics.Datasets
{
    public static class DatasetMappingRules
    {
        public const int MaxNameLength = 64;

        private static readonly Regex namePattern = new("^[A-Za-z][A-Za-z0-9_]{0,63}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool IsSafeName(string? name)
        {
            return name != null && namePattern.IsMatch(name);
        }

        public static List<DatasetMappingIssue> Validate(IReadOnlyList<DatasetColumnDefinition> profile,
            DatasetMappingInput input)
        {
            var issues = new List<DatasetMappingIssue>();

            if (input.Columns == null || input.Columns.Count == 0)
            {
                issues.Add(new DatasetMappingIssue(DatasetMappingCodes.ColumnsRequired, null));

                return issues;
            }

            if (input.Columns.Count != profile.Count)
            {
                issues.Add(new DatasetMappingIssue(DatasetMappingCodes.ColumnCountMismatch, null,
                    $"{profile.Count}"));

                return issues;
            }

            var byIndex = new Dictionary<int, DatasetMappingColumn>();

            foreach (var column in input.Columns)
            {
                if (profile.All(p => p.Index != column.Index))
                {
                    issues.Add(new DatasetMappingIssue(DatasetMappingCodes.ColumnIndexInvalid, column.Index));
                }
                else if (!byIndex.TryAdd(column.Index, column))
                {
                    issues.Add(new DatasetMappingIssue(DatasetMappingCodes.ColumnIndexDuplicate, column.Index));
                }
            }

            if (issues.Count > 0)
            {
                return issues;
            }

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var included = 0;

            foreach (var column in input.Columns.OrderBy(c => c.Index))
            {
                if (!column.Include)
                {
                    continue;
                }

                included++;
                var definition = profile.First(p => p.Index == column.Index);

                if (!definition.IsNumeric && !definition.IsCategorical)
                {
                    issues.Add(new DatasetMappingIssue(DatasetMappingCodes.VariableNotNumeric, column.Index));
                }
                else if (definition.IsConstant)
                {
                    issues.Add(new DatasetMappingIssue(DatasetMappingCodes.VariableConstant, column.Index));
                }

                if (!DatasetProcessingTypes.IsKnown(column.ProcessingTypeId))
                {
                    issues.Add(new DatasetMappingIssue(DatasetMappingCodes.ProcessingTypeInvalid, column.Index));
                }

                if (!IsSafeName(column.Name))
                {
                    issues.Add(new DatasetMappingIssue(DatasetMappingCodes.NameInvalid, column.Index));
                }
                else if (!names.Add(column.Name!))
                {
                    issues.Add(new DatasetMappingIssue(DatasetMappingCodes.NameDuplicate, column.Index, column.Name));
                }
            }

            if (included == 0)
            {
                issues.Add(new DatasetMappingIssue(DatasetMappingCodes.NoVariables, null));
            }

            return issues;
        }
    }
}