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

using Jube.Data.Reporting;
using Jube.Dto.Options;
using Jube.Service.Charts.Datasource.Models;

namespace Jube.Service.Charts.Datasource
{
    public static class DatasourceChartMappingResolver
    {
        public static bool Accepts(DatasourceRoleAccepts accepts, SeriesDataType type)
        {
            var numeric = type is SeriesDataType.Integer or SeriesDataType.Decimal or SeriesDataType.Flag;

            return accepts switch
            {
                DatasourceRoleAccepts.Numeric => numeric,
                DatasourceRoleAccepts.Time => type == SeriesDataType.DateTime,
                DatasourceRoleAccepts.NumericOrTime => numeric || type == SeriesDataType.DateTime,
                _ => true
            };
        }

        public static DatasourceChartMapping Resolve(DatasourceChartKind kind, DatasourceFrame frame,
            IReadOnlyDictionary<DatasourceChartRole, IReadOnlyList<string>>? overrides)
        {
            var problems = new List<DatasourceChartProblem>();
            var assigned = new Dictionary<DatasourceChartRole, IReadOnlyList<string>>();
            var claimed = new HashSet<string>(StringComparer.Ordinal);
            var byName = frame.Columns.ToDictionary(c => c.Name, StringComparer.Ordinal);
            var kindTerm = DatasourceChartText.KindTerm(kind);

            foreach (var (role, names) in overrides ?? new Dictionary<DatasourceChartRole, IReadOnlyList<string>>())
            {
                var spec = kind.Roles.FirstOrDefault(r => r.Role == role);
                var roleTerm = DatasourceChartText.RoleTerm(role);

                if (spec == null)
                {
                    problems.Add(new DatasourceChartProblem(DatasourceMessages.RoleNotUsed, [kindTerm, roleTerm]));
                    continue;
                }

                if (names.Count == 0)
                {
                    continue;
                }

                if (!spec.Many && names.Count > 1)
                {
                    problems.Add(new DatasourceChartProblem(DatasourceMessages.RoleTakesOne,
                        [roleTerm, names.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)]));

                    continue;
                }

                var valid = true;

                foreach (var name in names)
                {
                    if (!byName.TryGetValue(name, out var column))
                    {
                        problems.Add(new DatasourceChartProblem(DatasourceMessages.OverrideColumnMissing,
                            [name, roleTerm]));

                        valid = false;
                    }
                    else if (!Accepts(spec.Accepts, column.Type))
                    {
                        problems.Add(new DatasourceChartProblem(DatasourceMessages.OverrideColumnType,
                            [name, roleTerm, DatasourceChartText.NeedsTerm(spec.Accepts)]));

                        valid = false;
                    }
                }

                if (valid)
                {
                    assigned[role] = names.ToList();

                    foreach (var name in names)
                    {
                        claimed.Add(name);
                    }
                }
            }

            var explicitRoles = assigned.Keys.ToHashSet();

            foreach (var spec in kind.Roles.Where(r => !assigned.ContainsKey(r.Role)))
            {
                if (spec.Optional && !spec.AutoAssign && spec.NameHints is not { Count: > 0 })
                {
                    continue;
                }

                if (spec.DisabledByOverrideOf?.Any(explicitRoles.Contains) == true)
                {
                    continue;
                }

                var candidates = frame.Columns
                    .Where(c => !claimed.Contains(c.Name) && Accepts(spec.Accepts, c.Type))
                    .ToList();

                if (spec.NameHints is { Count: > 0 })
                {
                    var hinted = candidates.Where(c => Hinted(c.Name, spec.NameHints)).ToList();
                    candidates = spec.HintsOnly ? hinted : [.. hinted, .. candidates.Except(hinted)];
                }

                if (candidates.Count == 0)
                {
                    if (!spec.Optional)
                    {
                        problems.Add(new DatasourceChartProblem(DatasourceMessages.RoleUnsatisfied,
                            [kindTerm, DatasourceChartText.RoleTerm(spec.Role),
                                DatasourceChartText.NounTerm(spec.Accepts)]));
                    }

                    continue;
                }

                var take = spec.Many
                    ? spec.DefaultTake > 0 ? Math.Min(spec.DefaultTake, candidates.Count) : candidates.Count
                    : 1;

                var chosen = candidates.Take(take).Select(c => c.Name).ToList();
                assigned[spec.Role] = chosen;

                foreach (var name in chosen)
                {
                    claimed.Add(name);
                }
            }

            var mapping = new DatasourceChartMapping(assigned);

            if (problems.Count == 0 && kind.Check?.Invoke(mapping) is { } failure)
            {
                problems.AddRange(failure.Problems);
            }

            if (problems.Count > 0)
            {
                throw new DatasourceChartException(problems);
            }

            return mapping;
        }

        private static bool Hinted(string columnName, IReadOnlyList<string> hints)
        {
            var name = new string(columnName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

            return hints.Any(h => name == h || (h.Length >= 5 && name.Contains(h, StringComparison.Ordinal)));
        }
    }
}
