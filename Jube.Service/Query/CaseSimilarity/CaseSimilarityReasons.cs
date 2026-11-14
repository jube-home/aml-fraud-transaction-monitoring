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

using Jube.CaseRisk.Similarity;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseSimilarity;

namespace Jube.Service.Query.CaseSimilarity
{
    public static class CaseSimilarityReasons
    {
        public static string NameOf(SimilarityComponent component)
        {
            var value = component.Value;

            return component.Kind switch
            {
                CaseSimilarityKind.Rule or CaseSimilarityKind.Tag => value,
                CaseSimilarityKind.Model or CaseSimilarityKind.Measure => Before(value, value.LastIndexOf(':')),
                CaseSimilarityKind.Feature => After(Before(value, value.LastIndexOf('=')), ':'),
                _ => Before(value, value.LastIndexOf('='))
            };
        }

        public static List<CaseSimilarReasonDto> From(SimilarityResult result, Func<SimilarityComponent, double> weight,
            int take)
        {
            return
            [
                .. result.Kinds
                    .SelectMany(kind => kind.Shared)
                    .OrderByDescending(weight)
                    .ThenBy(component => component.Kind)
                    .ThenBy(component => component.Value, StringComparer.Ordinal)
                    .Select(component => (component.Kind, Name: NameOf(component)))
                    .Distinct()
                    .Take(take)
                    .Select(reason => new CaseSimilarReasonDto { KindId = (byte)reason.Kind, Name = reason.Name })
            ];
        }

        private static string Before(string value, int index) => index > 0 ? value[..index] : value;

        private static string After(string value, char separator) => value[(value.IndexOf(separator) + 1)..];
    }
}
