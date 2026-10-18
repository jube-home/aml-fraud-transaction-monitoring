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

using System.Text.Json;
using Jube.Data.Poco;
using Jube.Dto.Query.ExhaustiveSearchInstanceTrialInstanceListQuery;

namespace Jube.Service.Query.ExhaustiveSearchInstanceTrialInstanceListQuery
{
    internal static class ExhaustiveSearchInstanceTrialInstanceListQueryMapper
    {
        public static ExhaustiveSearchInstanceTrialInstanceListQueryDto ToDto(
            ExhaustiveSearchInstanceTrialInstance source, bool promoted)
        {
            return new ExhaustiveSearchInstanceTrialInstanceListQueryDto
            {
                Id = source.Id,
                CreatedDate = source.CreatedDate.GetValueOrDefault(),
                Kind = source.KindId?.ToString(),
                Score = Finite(source.Score),
                Promoted = promoted
            };
        }

        private static double? Finite(double? value)
        {
            return value.HasValue && double.IsFinite(value.Value) ? value : null;
        }

        public static ExhaustiveSearchInstanceTrialInstanceDetailDto? ToDetailDto(
            ExhaustiveSearchInstanceTrialInstance? source)
        {
            if (source is null)
            {
                return null;
            }

            JsonElement? evidence = null;

            if (!string.IsNullOrWhiteSpace(source.MetricsJson))
            {
                try
                {
                    using var document = JsonDocument.Parse(source.MetricsJson);
                    evidence = document.RootElement.Clone();
                }
                catch (JsonException)
                {
                    evidence = null;
                }
            }

            return new ExhaustiveSearchInstanceTrialInstanceDetailDto
            {
                Id = source.Id,
                Kind = source.KindId?.ToString(),
                Score = Finite(source.Score),
                HasDiagram = !string.IsNullOrWhiteSpace(source.Json),
                Evidence = evidence
            };
        }
    }
}