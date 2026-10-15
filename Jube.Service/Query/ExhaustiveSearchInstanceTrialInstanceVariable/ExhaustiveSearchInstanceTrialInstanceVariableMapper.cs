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

using Jube.Analytics.Categorical;
using Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery;
using Jube.Dto.Query.ExhaustiveSearchInstanceTrialInstanceVariable;

namespace Jube.Service.Query.ExhaustiveSearchInstanceTrialInstanceVariable
{
    internal static class ExhaustiveSearchInstanceTrialInstanceVariableMapper
    {
        public static ExhaustiveSearchInstanceTrialInstanceVariableDto ToDto(
            GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto source)
        {
            return new ExhaustiveSearchInstanceTrialInstanceVariableDto
            {
                Id = source.Id,
                Name = source.Name,
                Mean = FiniteNumber.Of(source.Mean),
                Maximum = FiniteNumber.Of(source.Maximum),
                Minimum = FiniteNumber.Of(source.Minimum),
                StandardDeviation = FiniteNumber.Of(source.StandardDeviation),
                NormalisationTypeId = source.NormalisationTypeId,
                EmptyRange = source.EmptyRange,
                VariableSequence = source.VariableSequence,
                ProcessingTypeId = source.ProcessingTypeId,
                CategoricalSourceName = source.CategoricalSourceName,
                CategoricalKind = source.CategoricalKind,
                CategoricalLevel = source.CategoricalLevel,
                CategoricalReferenceLabel = ReferenceLabel(source.CategoricalVocabularyJson)
            };
        }

        private static string? ReferenceLabel(string? vocabularyJson)
        {
            if (string.IsNullOrWhiteSpace(vocabularyJson))
            {
                return null;
            }

            try
            {
                return CategoricalFieldEncoder.FromJson(vocabularyJson).Reference.Label;
            }
            catch (Exception ex) when (ex is FormatException or NotSupportedException)
            {
                return null;
            }
        }

        public static List<ExhaustiveSearchInstanceTrialInstanceVariableDto> ToDto(
            IEnumerable<GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto>
                source)
        {
            return source.Select(ToDto).ToList();
        }
    }
}