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

using Jube.Dto.EntityAnalysisModelActivationRuleSuppression;

namespace Jube.Service.EntityAnalysisModelActivationRuleSuppression
{
    using ActivationRuleSuppressionPoco = Data.Poco.EntityAnalysisModelActivationRuleSuppression;

    internal static class EntityAnalysisModelActivationRuleSuppressionMapper
    {
        public static EntityAnalysisModelActivationRuleSuppressionDto? ToDto(ActivationRuleSuppressionPoco? suppression)
        {
            return suppression is null
                ? null
                : new EntityAnalysisModelActivationRuleSuppressionDto
                {
                    Id = suppression.Id,
                    EntityAnalysisModelGuid = suppression.EntityAnalysisModelGuid,
                    SuppressionKey = suppression.SuppressionKey,
                    SuppressionKeyValue = suppression.SuppressionKeyValue,
                    EntityAnalysisModelActivationRuleName = suppression.EntityAnalysisModelActivationRuleName,
                    CreatedUser = suppression.CreatedUser,
                    CreatedDate = ToOffset(suppression.CreatedDate),
                    Version = suppression.Version.GetValueOrDefault(),
                    DeletedUser = suppression.DeletedUser,
                    DeletedDate = ToOffset(suppression.DeletedDate),
                    DeleteExpiryDate = ToOffset(suppression.DeleteExpiryDate)
                };
        }

        public static List<EntityAnalysisModelActivationRuleSuppressionDto> ToDto(
            IEnumerable<ActivationRuleSuppressionPoco>? source)
        {
            return (source ?? Enumerable.Empty<ActivationRuleSuppressionPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static ActivationRuleSuppressionPoco ToPoco(EntityAnalysisModelActivationRuleSuppressionDto dto)
        {
            return new ActivationRuleSuppressionPoco
            {
                Id = dto.Id,
                EntityAnalysisModelGuid = dto.EntityAnalysisModelGuid,
                SuppressionKey = dto.SuppressionKey,
                SuppressionKeyValue = dto.SuppressionKeyValue,
                EntityAnalysisModelActivationRuleName = dto.EntityAnalysisModelActivationRuleName
            };
        }

        private static DateTimeOffset? ToOffset(DateTime? value)
        {
            return value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
        }
    }
}